using AgentBenchmark;
using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

Console.WriteLine("Agent Benchmark!");
const int TimeoutMinutes = 30;
var httpClient = new HttpClient()
{
    BaseAddress = new Uri("http://quorra.homelan.binaryward.com:11434"),
    Timeout = TimeSpan.FromMinutes(TimeoutMinutes)
};
var requestOptions = new RequestOptions()
{
    NumCtx = 2048,
    Temperature = 0.1f,
    TopP = 0.1f,
};

int maxRoundCount = 3;
Dictionary<string, Dictionary<string, int>> allBenchmarkData = [];

var checkModel = "llama3:70b-instruct-q6_K";
//string model = "llama3:8b-instruct-fp16", checkModel = "llama3:70b-instruct-q6_K";
//var model = "llama3:70b-instruct-q8_0";
//var checkModel = "llama3:70b-instruct-q6_K";
//var checkModel = model;
string[] models = [
    "llama3:8b-instruct-q2_K",
    "llama3:8b-instruct-q4_0",
    "llama3:8b-instruct-q6_K",
    "llama3:8b-instruct-q8_0",
    "llama3:8b-instruct-fp16",

    "llama3.1:8b-instruct-q2_K",
    "llama3.1:8b-instruct-q4_0",
    "llama3.1:8b-instruct-q6_K",
    "llama3.1:8b-instruct-q8_0",
    "llama3.1:8b-instruct-fp16",

    //"phi3:3.8b-mini-4k-instruct-fp16",
    //"phi3:14b-medium-4k-instruct-fp16",
    //"phi3.5:3.8b-mini-instruct-fp16",

    //"gemma2:2b-instruct-fp16",
    //"gemma2:9b-instruct-q6_K",
    //"gemma2:9b-instruct-q8_0",
    //"gemma2:9b-instruct-fp16",

    //"glm4:9b-chat-fp16",

    //"hermes3:8b-llama3.1-fp16",

    //"mistral:7b-instruct-v0.3-fp16",

    //"llama3:70b-instruct-q2_K",
    //"llama3:70b-instruct-q4_0",
    //"llama3:70b-instruct-q6_K",
    //"llama3:70b-instruct-q8_0",

    //"llama3.1:70b-instruct-q6_K",
    //"llama3.1:70b-instruct-q8_0",

    //"qwen2:0.5b-instruct-fp16",
    //"qwen2:1.5b-instruct-fp16",
    //"qwen2:7b-instruct-fp16",
    //"qwen2-math:1.5b-instruct-fp16",
    //"qwen2-math:7b-instruct-fp16",

    //"reflection:70b-q6_K",
];

Console.WriteLine($"Check Model: {checkModel}");
string[] teams = ["A", "B", "C"];
var teamCount = 3;
foreach (var model in models)
{
    Dictionary<string, Dictionary<string, int>> benchmarkData = [];
    Console.WriteLine($"Model: {model}");
    for (int t = 1; t <= maxRoundCount; t++)
    {
        Console.WriteLine($"Round {t}");
        Dictionary<string, int> secretValues = [];
        foreach (var prefix in teams)
        {
            for (int i = 0; i < teamCount; i++)
            {
                string nodeId = $"{prefix}{i}";
                int secretValue = new Random().Next(1, 6);  // Generate a random secret value
                secretValues[nodeId] = secretValue;
            }
        }
        // Tally
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.AutoGenTally_AutoGenSelector_AutoGenAgent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.AutoGenTally_BAIsicV1Selector_AutoGenV2Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.TallyV1_BAIsicV1Selector_ChocolateTeamV1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.TallyV2_BAIsicV1Selector_ChocolateTeamV2Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.TallyV2_BAIsicV1Selector_ChocolateTeamV3Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);

        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.TallyV1_BAIsicV1Selector_ChocolateTeamV1_1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.TallyV2_BAIsicV1Selector_ChocolateTeamV2_1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamTallyBenchmark.TallyV2_BAIsicV1Selector_ChocolateTeamV3_1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);

        // Report
        await foreach(var reportResult in ChocolateTeamReportBenchmark.All_Benchmarks(secretValues, model, checkModel, httpClient, requestOptions))
        {
            UpdateBenchmarkData(benchmarkData, reportResult, model);
        }

        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamReportBenchmark.ReportV1_BAIsicV1Selector_ChocolateTeamV1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamReportBenchmark.ReportV1_BAIsicV1Selector_ChocolateTeamV1_1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamReportBenchmark.ReportV1_BAIsicV1Selector_ChocolateTeamV2Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamReportBenchmark.ReportV1_BAIsicV1Selector_ChocolateTeamV2_1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamReportBenchmark.ReportV1_BAIsicV1Selector_ChocolateTeamV3Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);
        //UpdateBenchmarkData(benchmarkData, await ChocolateTeamReportBenchmark.ReportV1_BAIsicV1Selector_ChocolateTeamV3_1Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions).ConfigureAwait(false), model);


        Console.WriteLine(WriteStatus(benchmarkData, maxRoundCount));
        Console.WriteLine($"Round {t} done");
        Console.WriteLine("");
    }

    allBenchmarkData = allBenchmarkData.Concat(benchmarkData).ToDictionary(x => x.Key, x => x.Value);
}

Console.WriteLine("Agent Benchmark complete!");
var benchmarkResults = WriteStatus(allBenchmarkData, maxRoundCount);
Console.WriteLine(benchmarkResults);
SaveResults(benchmarkResults);
SaveBenchmark(allBenchmarkData);

static void UpdateBenchmarkData(Dictionary<string, Dictionary<string, int>> benchmarkData, (string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult) result, string model)
{
    var benchmarkName = $"{model}/{result.BenchmarkName}";
    if (benchmarkData.TryGetValue(benchmarkName, out Dictionary<string, int>? testCounts))
    {
        if (testCounts.ContainsKey(result.BenchmarkResult))
        {
            testCounts[result.BenchmarkResult]++;
        }
        else
        {
            testCounts[result.BenchmarkResult] = 1;
        }
    }
    else
    {
        benchmarkData[benchmarkName] = new Dictionary<string, int>
        {
            [result.BenchmarkResult] = 1
        };
    }

    WriteBenchmarkConversation(benchmarkName, result.BenchmarkResult, result.BenchmarkConversationResult);
}

static string DataDirectory()
{
    // Create a directory to store the benchmark data
    string dataDirectory = "data";
    if (!Directory.Exists(dataDirectory))
    {
        Directory.CreateDirectory(dataDirectory);
    }
    return dataDirectory;
}

    static void WriteBenchmarkConversation(string benchmarkName, string benchmarkResult, List<ConversationResult> benchmarkConversationResult)
{
    var benchmarkFileName = $"{DataDirectory()}/{benchmarkName.Replace("/", "_").Replace(":", "_")}.json";
    bool fileExists = File.Exists(benchmarkFileName);
    var benchmarkConversationData = new BenchmarkConversationData(benchmarkName, benchmarkResult, benchmarkConversationResult.Select(x => new BenchmarkConversationResult(x.TurnCount, x.Conversation.Select(y => new BenchmarkConversationHistory(y.Agent.Name, y.Messages)).ToImmutableList())).ToImmutableList());

    using (StreamWriter writer = new StreamWriter(benchmarkFileName, append: fileExists))
    {
        string jsonData = JsonSerializer.Serialize(benchmarkConversationData, options: new JsonSerializerOptions { WriteIndented = false });
        writer.WriteLine(jsonData);
    }
}

static string WriteStatus(Dictionary<string, Dictionary<string, int>> testResults, int roundCount)
{
    StringBuilder stringBuilder = new StringBuilder();
    stringBuilder.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    stringBuilder.AppendLine($"Round Count: {roundCount}");
    foreach (var item in testResults)
    {
        stringBuilder.AppendLine($"Benchmark: {item.Key}");
        foreach (var result in item.Value)
        {
            stringBuilder.AppendLine($"  Count: {result.Value} ({result.Key})");
        }
    }

    return stringBuilder.ToString();
}

static void SaveBenchmark(Dictionary<string, Dictionary<string, int>> testResults)
{
    string filePath = $"{DataDirectory()}/agentbenchmark.json";
    bool fileExists = File.Exists(filePath);

    Dictionary<string, Dictionary<string, int>>? fileData=null;

    if (fileExists)
    {
        var fileJson = File.ReadAllText(filePath);
        fileData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(fileJson);
    }

    fileData ??= new Dictionary<string, Dictionary<string, int>>();

    // Merge the existing data with the new data
    foreach (var item in testResults)
    {
        if (fileData.TryGetValue(item.Key, out Dictionary<string, int>? existingItem))
        {
            foreach (var result in item.Value)
            {
                if (existingItem.ContainsKey(result.Key))
                {
                    existingItem[result.Key] += result.Value;
                }
                else
                {
                    existingItem[result.Key] = result.Value;
                }
            }
        }
        else
        {
            fileData[item.Key] = item.Value;
        }
    }

    using (StreamWriter writer = new StreamWriter(filePath, append: false))
    {
        string jsonData = JsonSerializer.Serialize(fileData, options: new JsonSerializerOptions { WriteIndented = true });
        writer.WriteLine(jsonData);
    }

    SaveBenchmarkCsv(fileData);
}

static void SaveBenchmarkCsv(Dictionary<string, Dictionary<string, int>> testResults)
{
    // save testResults to csv
    string filePath = $"{DataDirectory()}/agentbenchmark.csv";
    
    var keys = new List<string> {
        AgentBenchmarkConventions.BenchmarkReasons.Pass,
        AgentBenchmarkConventions.BenchmarkReasons.FailNotCorrect,
        AgentBenchmarkConventions.BenchmarkReasons.FailNotCorrectBecauseImpersonation,
        AgentBenchmarkConventions.BenchmarkReasons.FailMinimumTurnCount,
        AgentBenchmarkConventions.BenchmarkReasons.FailMaximumTurnCount,
    };

    using (StreamWriter writer = new StreamWriter(filePath, append: false))
    {
        // Write the header row
        writer.WriteLine("\"benchmark\",\"score\"," + string.Join(",", keys.Select(key => $"\"{key}\"")));

        foreach (var item in testResults)
        {
            int pass = 0;
            int total = 0;

            // Write the data row
            var values = new List<int>();
            foreach (var key in keys)
            {
                if (item.Value.TryGetValue(key, out var value))
                {
                    values.Add(value);

                    total += value;
                    if (key == AgentBenchmarkConventions.BenchmarkReasons.Pass)
                    {
                        pass = value;
                    }    
                }
                else
                {
                    values.Add(0); // Add 0 value for missing keys
                }
            }
            int score = total > 0 ? (pass / total) * 100 : 0;
            writer.WriteLine($"\"{item.Key}\",{score}," + string.Join(",", values));
        }
    }
}

static void SaveResults(string testResults)
{
    string filePath = $"{DataDirectory()}/agentbenchmark.txt";
    bool fileExists = File.Exists(filePath);

    using (StreamWriter writer = new StreamWriter(filePath, append: fileExists))
    {
        writer.WriteLine(testResults);
    }
}