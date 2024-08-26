using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using BAIsic.LlmApi.Ollama;

public class OllamaBenchmark
{
    private OllamaClient _ollama;
    private HashSet<string> _runHistory;
    private const int TimeoutMinutes = 30;
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
                    Console.WriteLine($"Model {model} timed out for prompt {prompt}");
                }

                stopwatch.Stop();
                var elapsed = !cts.Token.IsCancellationRequested ? stopwatch.Elapsed : TimeSpan.MinValue;
                results[model][prompt] = elapsed;
                var tokensPerSecond = modelResponse.EvalCount < 0 ? -1 : modelResponse.EvalCount / (modelResponse.EvalDuration / 1e9);
                SaveRecord(model, prompt, elapsed, modelResponse.Message!.Content, tokensPerSecond!.Value);
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