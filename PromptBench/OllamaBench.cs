using System;
using System.Collections.Generic;
using System.Diagnostics;
using OllamaSharp;
using OllamaSharp.Models;

public class OllamaBenchmark
{
    private OllamaApiClient _ollama;

    public OllamaBenchmark()
    {
        _ollama = new OllamaApiClient("http://quorra.homelan.binaryward.com:11434");
    }

    public async Task<string[]> GetModels()
    {
        var models = await _ollama.ListLocalModels();
        return models.Select(m => m.Name).ToArray();
    }
    public async Task<Dictionary<string, Dictionary<string, TimeSpan>>> RunAsync(IEnumerable<string> models, List<string> prompts)
    {
        var results = new Dictionary<string, Dictionary<string, TimeSpan>>();

        foreach (var model in models)
        {
            results[model] = new Dictionary<string, TimeSpan>();

            foreach (var prompt in prompts)
            {
                Console.WriteLine($"Running model: {DateTime.Now} {model}, prompt: {prompt}");
                var stopwatch = Stopwatch.StartNew();
                var modelRequest = new GenerateCompletionRequest
                {
                    Model = model,
                    Prompt = prompt,
                    // Options = new RequestOptions
                    // {
                    //     Temperature = 0.0f,
                    // }
                };

                var timeout = TimeSpan.FromMinutes(5);

                using var cts = new CancellationTokenSource(timeout);

                ConversationContextWithResponse? modelResponse = null;
                try {
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