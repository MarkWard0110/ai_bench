using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using OllamaSharp;
using OllamaSharp.Models;

public class OllamaBenchmark
{
    private OllamaApiClient _ollama;
    private HashSet<string> _runHistory;

    public OllamaBenchmark()
    {
        _ollama = new OllamaApiClient("http://quorra.homelan.binaryward.com:11434");
    }

    public async Task<string[]> GetModels()
    {
        var models = await _ollama.ListLocalModels();
        return models.Select(m => m.Name).ToArray();
    }

    private void LoadRunHistoryPairs()
    {
        if (File.Exists("duration-results.csv"))
        {
            using (var reader = new StreamReader("duration-results.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<dynamic>();
                foreach (var record in records)
                {
                    var model = record.Model;
                    var prompt = record.Prompt;
                    _runHistory.Add($"{model}-{prompt}");
                }
            }
        }
    }
    public async Task<Dictionary<string, Dictionary<string, TimeSpan>>> RunAsync(IEnumerable<string> models, List<string> prompts)
    {
        var results = new Dictionary<string, Dictionary<string, TimeSpan>>();
        var modelCount = models.Count();
        var promptCount = prompts.Count;
        var total = modelCount * promptCount;
        int modelIndex = 0;
        _runHistory = new HashSet<string>();
        LoadRunHistoryPairs();

        foreach (var model in models)
        {
            modelIndex++;
            results[model] = new Dictionary<string, TimeSpan>();
            int promptIndex = 0;
            foreach (var prompt in prompts)
            {
                promptIndex++;

                // Calculate and output the total percentage of completion
                int completed = ((modelIndex - 1) * promptCount) + promptIndex;
                double percentage = (double)completed / total * 100;

                Console.WriteLine($"Running {percentage}%: model {modelIndex} of {modelCount}: {DateTime.Now} {model}, prompt {promptIndex} of {promptCount}: {prompt}");

                var sanitizedPrompt = SanatiseValue(prompt);
                if (_runHistory.Contains($"{model}-{sanitizedPrompt}"))
                {
                    Console.WriteLine($"Skipping model {model} and prompt {prompt} as it has already been run.");
                    continue;
                }

                var stopwatch = Stopwatch.StartNew();
                var modelRequest = new GenerateCompletionRequest
                {
                    Model = model,
                    Prompt = prompt,
                    // Options = new RequestOptions
                    // {
                    //     Temperature = 0.9f
                    // }
                };

                var timeout = TimeSpan.FromMinutes(5);

                using var cts = new CancellationTokenSource(timeout);

                ConversationContextWithResponse? modelResponse = null;
                try
                {
                    modelResponse = await _ollama.GetCompletion(modelRequest, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    modelResponse = new ConversationContextWithResponse(string.Empty, [], null);
                }
                if (cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine($"Model {model} timed out for prompt {prompt}");
                }

                stopwatch.Stop();
                var elapsed = !cts.Token.IsCancellationRequested ? stopwatch.Elapsed : TimeSpan.MinValue;
                results[model][prompt] = elapsed;
                var tokensPerSecond = modelResponse.Metadata != null ? modelResponse.Metadata.EvalCount / (modelResponse.Metadata.EvalDuration / 1e9) : -1;
                SaveRecord(model, prompt, elapsed, modelResponse.Response, tokensPerSecond);

            }
        }
        return results;
    }

    private void SaveRecord(string model, string prompt, TimeSpan duration, string response, double tokensPerSecond)
    {
        Console.WriteLine($"{model}, duration: {duration}, tps: {tokensPerSecond}, {prompt}");
        var prompt_value = SanatiseValue(prompt);
        var response_value = SanatiseValue(response);
        SaveDurationResults(model, prompt_value, duration, tokensPerSecond);
        SaveInvokeLog(model, prompt_value, response_value, duration, tokensPerSecond);

    }

    private void SaveDurationResults(string model, string prompt, TimeSpan duration, double tokensPerSecond)
    {
        string filePath = "duration-results.csv";
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: fileExists))
        {
            if (!fileExists)
            {
                writer.WriteLine("\"Model\",\"Duration\",\"TokensPerSecond\",\"Prompt\"");
            }
            writer.WriteLine($"\"{model}\",\"{duration}\",\"{tokensPerSecond}\",\"{prompt}\"");
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

    private void SaveInvokeLog(string model, string prompt, string response, TimeSpan duration, double tokensPerSecond)
    {
        string filePath = "invoke_log.csv";
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, append: fileExists))
        {
            if (!fileExists)
            {
                writer.WriteLine("\"Model\",\"Duration\",\"TokensPerSecond\",\"Prompt\",\"Response\"");
            }

            writer.WriteLine($"\"{model}\",\"{duration}\",\"{tokensPerSecond}\",\"{prompt}\",\"{response}\"");
        }

    }
}