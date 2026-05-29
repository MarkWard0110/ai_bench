using System.CommandLine;
using BAIsic.LlmApi.Ollama;

const string CurrentBenchmarkDirectory = "_current";
const string BenchmarkArchiveDirectory = "benchmarks";

var rootCommand = new RootCommand("Ollama Model Benchmark Tool");
var samplesOption = new Option<int>(
    aliases: new[] { "--samples", "-s" },
    description: "Number of times to run each prompt per model.",
    getDefaultValue: () => 1
);
rootCommand.AddOption(samplesOption);

var contextConfigOption = new Option<string?>(
    aliases: new[] { "--context-config", "-c" },
    description: "CSV file mapping model_name to max_context_size."
);
rootCommand.AddOption(contextConfigOption);

var ollamaHostOption = new Option<string>(
    aliases: new[] { "--ollama-host", "-o" },
    description: "Ollama API host URL.",
    getDefaultValue: () => "http://quorra.homelan.binaryward.com:11434"
);
rootCommand.AddOption(ollamaHostOption);

var cpuOnlyOption = new Option<bool>(
    aliases: new[] { "--cpu", "-p" },
    description: "Run benchmark using CPU only."
);
rootCommand.AddOption(cpuOnlyOption);

var newOverrideOption = new Option<bool>(
    aliases: new[] { "--override" },
    description: "Delete _current benchmark data without prompting."
);

var newCommand = new Command("new", "Reset _current benchmark data.");
newCommand.AddOption(newOverrideOption);
newCommand.SetHandler((bool isOverride) =>
{
    StartNewBenchmark(isOverride);
}, newOverrideOption);
rootCommand.AddCommand(newCommand);

var archiveLabelOption = new Option<string?>(
    aliases: new[] { "--label", "-l" },
    description: "Optional label appended to archive folder name."
);

var archiveCommand = new Command("archive", "Archive _current benchmark data to benchmarks/<timestamp>[-label].");
archiveCommand.AddOption(archiveLabelOption);
archiveCommand.SetHandler((string? label) =>
{
    ArchiveCurrentBenchmarkData(label);
}, archiveLabelOption);
rootCommand.AddCommand(archiveCommand);


rootCommand.SetHandler(async (int sampleCount, string? contextConfigFile, string ollamaHost, bool cpuOnly) =>
{
    Console.WriteLine($"ai_bench! Running with sampleCount={sampleCount}, ollamaHost={ollamaHost}, cpuOnly={cpuOnly}");
    var dataDirectory = DataDirectory();
    var ollamaBenchmark = new OllamaBenchmark(ollamaHost, dataDirectory);
    var models = await ollamaBenchmark.GetModels();

    var modelIgnoreList = new string[]{
        "nomic-embed-text:137m-v1.5-fp16", // does not support chat - embedding model
        "mxbai-embed-large:335m-v1-fp16", // embedding model
        "snowflake-arctic-embed:335m-l-fp16",
        "zw66/llama3-chat-8.0bpw:latest",   
        "unclemusclez/jina-embeddings-v2-base-code:f16",
        "jina/jina-embeddings-v2-base-en:latest",
        "bge-large:335m-en-v1.5-fp16",
        "all-minilm:33m-l12-v2-fp16",
    };

    models = models.Where(x => !modelIgnoreList.Contains(x)).ToArray();

    // Parse context config file if provided
    Dictionary<string, int> modelContextSizes = new();
    bool filterModelsByContextFile = false;
    if (!string.IsNullOrEmpty(contextConfigFile) && File.Exists(contextConfigFile))
    {
        filterModelsByContextFile = true;
        using var reader = new StreamReader(contextConfigFile);
        using var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture);
        csv.Read(); // header
        csv.ReadHeader();
        while (csv.Read())
        {
            var model = csv.GetField("model_name");
            var ctxStr = csv.GetField("max_context_size");
            if (!string.IsNullOrEmpty(model) && int.TryParse(ctxStr, out var ctx))
                modelContextSizes[model] = ctx;
        }
    }

    if (filterModelsByContextFile)
    {
        // Only keep models that are present in the context config file
        models = models.Where(x => modelContextSizes.ContainsKey(x)).ToArray();
    }

    var prompts = new List<string> {
        "What is the capital of France?",
    };

    // Randomly sort models
    var random = new Random();
    models = models.OrderBy(x => random.Next()).ToArray();

    // Pass context mapping to benchmark
    var results = await ollamaBenchmark.RunAsync(models, prompts, sampleCount, modelContextSizes, cpuOnly);

    // Save final report to CSV by reading all samples from duration-results.csv
    string reportFile = Path.Combine(dataDirectory, "benchmark_report.csv");
    string durationFile = Path.Combine(dataDirectory, "duration-results.csv");
    var allSamples = new Dictionary<(string model, string ctx), Dictionary<string, List<(double ms, double tps)>>>();
    if (File.Exists(durationFile))
    {
        using (var reader = new StreamReader(durationFile))
        using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
        {
            csv.Read(); // header
            csv.ReadHeader();
            while (csv.Read())
            {
                var model = csv.GetField("Model") ?? string.Empty;
                var ctxSize = csv.GetField("ContextSize") ?? string.Empty;
                var prompt = csv.GetField("Prompt") ?? string.Empty;
                var durationStr = csv.GetField("Duration");
                var tpsStr = csv.GetField("TokensPerSecond");
                if (!TimeSpan.TryParse(durationStr, out var duration))
                    continue;
                if (duration == TimeSpan.MinValue)
                    continue;
                if (!double.TryParse(tpsStr, out var tps))
                    tps = double.NaN;
                var key = (model, ctxSize);
                if (!allSamples.ContainsKey(key))
                    allSamples[key] = new Dictionary<string, List<(double, double)>>();
                if (!allSamples[key].ContainsKey(prompt))
                    allSamples[key][prompt] = new List<(double, double)>();
                allSamples[key][prompt].Add((duration.TotalMilliseconds, tps));
            }
        }
    }
    using (var writer = new StreamWriter(reportFile, append: false))
    {
        writer.WriteLine("Model,ContextSize,SampleCount,MinTokensPerSec,MaxTokensPerSec,MeanTokensPerSec,MedianTokensPerSec,StdDevTokensPerSec,MinMs,MaxMs,MeanMs,MedianMs,StdDevMs,Prompt");
        foreach (var key in allSamples.Keys.OrderBy(x => x.model).ThenBy(x => x.ctx))
        {
            var model = key.model;
            var ctxSize = key.ctx;
            foreach (var prompt in allSamples[key].Keys.OrderBy(x => x))
            {
                var samples = allSamples[key][prompt];
                var msList = samples.Select(x => x.ms).Where(x => !double.IsNaN(x)).ToArray();
                var tpsList = samples.Select(x => x.tps).Where(x => !double.IsNaN(x)).ToArray();
                if (msList.Length == 0)
                {
                    writer.WriteLine($"{model},{ctxSize},0,,,,,,,,,,,,{prompt}");
                    continue;
                }
                var minMs = msList.Min();
                var maxMs = msList.Max();
                var meanMs = msList.Average();
                var medianMs = GetMedian(msList);
                var stdDevMs = Math.Sqrt(msList.Select(x => Math.Pow(x - meanMs, 2)).Average());
                var minTps = tpsList.Length > 0 ? tpsList.Min() : double.NaN;
                var maxTps = tpsList.Length > 0 ? tpsList.Max() : double.NaN;
                var meanTps = tpsList.Length > 0 ? tpsList.Average() : double.NaN;
                var medianTps = tpsList.Length > 0 ? GetMedian(tpsList) : double.NaN;
                var stdDevTps = tpsList.Length > 0 ? Math.Sqrt(tpsList.Select(x => Math.Pow(x - meanTps, 2)).Average()) : double.NaN;
                writer.WriteLine($"{model},{ctxSize},{msList.Length},{minTps},{maxTps},{meanTps},{medianTps},{stdDevTps},{minMs},{maxMs},{meanMs},{medianMs},{stdDevMs},{prompt}");
            }
        }
    }
    Console.WriteLine($"Report saved to {reportFile}");
}, samplesOption, contextConfigOption, ollamaHostOption, cpuOnlyOption);

return await rootCommand.InvokeAsync(args);

// Helper function to calculate median
double GetMedian(double[] values)
{
    Array.Sort(values);
    int n = values.Length;
    double median = (n % 2 == 0) ? (values[n / 2 - 1] + values[n / 2]) / 2 : values[n / 2];
    return median;
}

string DataDirectory()
{
    if (!Directory.Exists(CurrentBenchmarkDirectory))
    {
        Directory.CreateDirectory(CurrentBenchmarkDirectory);
    }

    return CurrentBenchmarkDirectory;
}

void StartNewBenchmark(bool isOverride)
{
    var currentDirectory = DataDirectory();
    if (!HasBenchmarkData(currentDirectory))
    {
        Console.WriteLine("No _current benchmark data found.");
        return;
    }

    if (!isOverride)
    {
        Console.Write("Found _current benchmark data. Delete it? (yes/no): ");
        var input = Console.ReadLine()?.Trim();
        var confirmed = string.Equals(input, "yes", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(input, "y", StringComparison.OrdinalIgnoreCase);
        if (!confirmed)
        {
            Console.WriteLine("Keeping _current benchmark data.");
            return;
        }
    }

    Directory.Delete(currentDirectory, recursive: true);
    Directory.CreateDirectory(currentDirectory);
    Console.WriteLine("_current benchmark data reset.");
}

void ArchiveCurrentBenchmarkData(string? label)
{
    var currentDirectory = DataDirectory();
    if (!HasBenchmarkData(currentDirectory))
    {
        Console.WriteLine("No _current benchmark data found to archive.");
        return;
    }

    Directory.CreateDirectory(BenchmarkArchiveDirectory);

    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    var normalizedLabel = NormalizeArchiveLabel(label);
    var folderName = string.IsNullOrWhiteSpace(normalizedLabel)
        ? timestamp
        : $"{timestamp}-{normalizedLabel}";

    var destinationDirectory = Path.Combine(BenchmarkArchiveDirectory, folderName);
    var dedupeSuffix = 1;
    while (Directory.Exists(destinationDirectory))
    {
        destinationDirectory = Path.Combine(BenchmarkArchiveDirectory, $"{folderName}-{dedupeSuffix}");
        dedupeSuffix++;
    }

    Directory.Move(currentDirectory, destinationDirectory);
    Directory.CreateDirectory(currentDirectory);

    Console.WriteLine($"Archived _current benchmark data to {destinationDirectory}");
}

bool HasBenchmarkData(string directoryPath)
{
    return Directory.Exists(directoryPath) && Directory.EnumerateFileSystemEntries(directoryPath).Any();
}

string NormalizeArchiveLabel(string? label)
{
    if (string.IsNullOrWhiteSpace(label))
    {
        return string.Empty;
    }

    var invalidChars = Path.GetInvalidFileNameChars();
    var normalized = new string(label
        .Trim()
        .Select(c => invalidChars.Contains(c) ? '_' : c)
        .ToArray())
        .Replace(' ', '-');

    return normalized;
}
