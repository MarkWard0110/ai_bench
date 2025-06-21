using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using BAIsic.LlmApi.Ollama;

public class OllamaBenchmark
{
    private OllamaClient _ollama;
    private HashSet<string> _runHistory = new HashSet<string>();
    private const int TimeoutMinutes = 30;
    private const string DurationResultsFile = "duration-results.csv";
    private const string InvokeLogFile = "invoke_log.csv";
    public OllamaBenchmark(string ollamaApiUrl)
    {
        _ollama = new OllamaClient(new HttpClient()
        {
            BaseAddress = new Uri(ollamaApiUrl),
            Timeout = TimeSpan.FromMinutes(TimeoutMinutes)
        });
    }

    public async Task<string[]> GetModels()
    {
        var models = await _ollama.ListLocalModelsAsync();
        return models.Select(m => m.Name).ToArray();
    }

    private void LoadRunHistoryTriplets()
    {
        _runHistory = new HashSet<string>();
        if (File.Exists(DurationResultsFile))
        {
            using (var reader = new StreamReader(DurationResultsFile))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var model = csv.GetField("Model") ?? string.Empty;
                    var ctx = csv.GetField("ContextSize") ?? string.Empty;
                    var prompt = SanatiseValue(csv.GetField("Prompt") ?? string.Empty);
                    var sampleIndex = csv.GetField("SampleIndex") ?? string.Empty;
                    // Use the same key format as in RunAsync
                    var modelKey = string.IsNullOrEmpty(ctx) ? model : $"{model}-ctx{ctx}";
                    _runHistory.Add($"{modelKey}-{prompt}-{sampleIndex}");
                }
            }
        }
    }

    public async Task<Dictionary<string, Dictionary<string, List<TimeSpan>>>> RunAsync(
        IEnumerable<string> models,
        List<string> prompts,
        int sampleCount)
    {
        return await RunAsync(models, prompts, sampleCount, new Dictionary<string, int>());
    }

    public async Task<Dictionary<string, Dictionary<string, List<TimeSpan>>>> RunAsync(
        IEnumerable<string> models,
        List<string> prompts,
        int sampleCount,
        Dictionary<string, int> modelContextSizes)
    {
        var results = new Dictionary<string, Dictionary<string, List<TimeSpan>>>();
        var modelCount = models.Count();
        var promptCount = prompts.Count;
        var total = modelCount * promptCount * sampleCount;
        int completed = 0;
        LoadRunHistoryTriplets();

        int modelIndex = 0;
        foreach (var model in models)
        {
            modelIndex++;
            int contextSize = 2048;
            if (modelContextSizes != null && modelContextSizes.TryGetValue(model, out var ctx))
                contextSize = ctx;
            string modelKey = $"{model}-ctx{contextSize}";
            if (!results.ContainsKey(modelKey))
                results[modelKey] = new Dictionary<string, List<TimeSpan>>();
            int promptIndex = 0;
            foreach (var prompt in prompts)
            {
                promptIndex++;
                var sanitizedPrompt = SanatiseValue(prompt);
                if (!results[modelKey].ContainsKey(prompt))
                    results[modelKey][prompt] = new List<TimeSpan>();
                for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                {
                    string runKey = $"{modelKey}-{sanitizedPrompt}-{sampleIndex}";
                    completed++;
                    double percentage = (double)completed / total * 100;
                    Console.WriteLine($"Running {percentage:F2}%: model {modelIndex} of {modelCount}: {DateTime.Now} {modelKey}, prompt {promptIndex} of {promptCount}: {prompt}, sample {sampleIndex + 1} of {sampleCount}");

                    if (_runHistory.Contains(runKey))
                    {
                        Console.WriteLine($"Skipping model {modelKey}, prompt {prompt}, sample {sampleIndex} as it has already been run.");
                        continue;
                    }

                    var stopwatch = Stopwatch.StartNew();
                    var chatRequest = new ChatRequest
                    {
                        Model = model,
                        Messages = [new Message
                        {
                            Role = "user",
                            Content = prompt
                        }],
                        Options = new RequestOptions
                        {
                            Temperature = 0.0f,
                            TopP = 0.0f,
                            NumPredict = 1024,
                            NumCtx = contextSize
                        },
                        Stream = false
                    };
                    var timeout = TimeSpan.FromMinutes(TimeoutMinutes);
                    using var cts = new CancellationTokenSource(timeout);
                    ChatResponse? modelResponse = null;
                    try
                    {
                        modelResponse = await _ollama.InvokeChatCompletionAsync(chatRequest, cancellationToken: cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        modelResponse = new ChatResponse()
                        {
                            Message = new Message()
                            {
                                Content = "<timeout waiting for a response>",
                                Role = string.Empty
                            },
                            EvalCount = -1,
                            EvalDuration = -1
                        };
                    }
                    if (cts.Token.IsCancellationRequested)
                    {
                        Console.WriteLine($"Model {modelKey} timed out for prompt {prompt}");
                    }
                    stopwatch.Stop();
                    var elapsed = !cts.Token.IsCancellationRequested ? stopwatch.Elapsed : TimeSpan.MinValue;
                    results[modelKey][prompt].Add(elapsed);
                    var tokensPerSecond = modelResponse.EvalCount < 0 ? -1 : modelResponse.EvalCount / (modelResponse.EvalDuration / 1e9);
                    SaveRecord(modelKey, prompt, elapsed, modelResponse.Message!.Content, tokensPerSecond!.Value, sampleIndex);
                }
            }
        }
        return results;
    }

    private void SaveRecord(string model, string prompt, TimeSpan duration, string response, double tokensPerSecond, int sampleIndex)
    {
        // Split model and context size
        string modelName = model;
        string contextSize = "";
        var ctxIdx = model.LastIndexOf("-ctx");
        if (ctxIdx > 0)
        {
            modelName = model.Substring(0, ctxIdx);
            contextSize = model.Substring(ctxIdx + 4); // skip '-ctx'
        }
        // Remove any leading 'x' or whitespace from contextSize
        if (!string.IsNullOrEmpty(contextSize) && (contextSize.StartsWith("x") || contextSize.StartsWith("X")))
            contextSize = contextSize.Substring(1);
        contextSize = contextSize.Trim();
        Console.WriteLine($"{modelName}, ctx: {contextSize}, duration: {duration}, tps: {tokensPerSecond}, sample: {sampleIndex}, {prompt}");
        var prompt_value = SanatiseValue(prompt);
        var response_value = SanatiseValue(response);
        SaveDurationResults(modelName, contextSize, prompt_value, duration, tokensPerSecond, sampleIndex);
        SaveInvokeLog(modelName, contextSize, prompt_value, response_value, duration, tokensPerSecond, sampleIndex);
    }

    private void SaveDurationResults(string model, string contextSize, string prompt, TimeSpan duration, double tokensPerSecond, int sampleIndex)
    {
        string filePath = DurationResultsFile;
        bool fileExists = File.Exists(filePath);
        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Model,ContextSize,Duration,TokensPerSecond,Prompt,SampleIndex");
            }
            writer.WriteLine($"{model},{contextSize},{duration},{tokensPerSecond},{prompt},{sampleIndex}");
        }
    }

    private string SanatiseValue(string value)
    {
        value = value.Replace("\"", "\"\"");
        value = value.Replace("\r", "");
        value = value.Replace("\\n", "\\\\n");
        value = value.Replace("\n", "\\n");
        return value;
    }

    private void SaveInvokeLog(string model, string contextSize, string prompt, string response, TimeSpan duration, double tokensPerSecond, int sampleIndex)
    {
        string filePath = InvokeLogFile;
        bool fileExists = File.Exists(filePath);
        using (StreamWriter writer = new StreamWriter(filePath, append: true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Model,ContextSize,Duration,TokensPerSecond,Prompt,Response,SampleIndex");
            }
            writer.WriteLine($"{model},{contextSize},{duration},{tokensPerSecond},{prompt},{response},{sampleIndex}");
        }
    }
}