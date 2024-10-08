﻿using AgentBenchmark;
using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;

Console.WriteLine("Agent Benchmark!");
const int TimeoutMinutes = 30;

string[] models = File.ReadAllLines("models.txt")
                       .Select(line => line.Trim())
                       .Where(line => !line.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(line))
                       .ToArray();

string[] games = File.ReadAllLines("games.txt")
                      .Select(line => line.Trim().ToLower())
                      .Where(line => !line.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(line))
                      .ToArray();

string rounds = File.ReadAllText("rounds.txt").Trim();

var httpClient = new HttpClient()
{
    BaseAddress = new Uri("http://quorra.homelan.binaryward.com:11434"),
    Timeout = TimeSpan.FromMinutes(TimeoutMinutes)
};
// random number generator for positive int 
var random = new Random();


Dictionary<string, Dictionary<string, int>> allBenchmarkData = [];

string[] teams = ["A", "B", "C"];
var teamCount = 3;

int maxRoundCount = int.Parse(rounds);
foreach (var model in models)
{
    Dictionary<string, Dictionary<string, int>> benchmarkData = [];
    Console.WriteLine($"Model: {model}");
    for (int t = 1; t <= maxRoundCount; t++)
    {
        Console.WriteLine($"Round {t}");
        int randomPositiveInt = random.Next(1, int.MaxValue);
        var requestOptions = new RequestOptions()
        {
            NumCtx = 2048,
            Temperature = 0.1f,
            TopP = 0.1f,
            Seed = randomPositiveInt,
        };
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
        if (games.Contains("tally"))
        {
            await foreach (var reportResult in ChocolateTeamTallyBenchmark.All_Benchmarks(secretValues, model, httpClient, requestOptions))
            {
                UpdateBenchmarkData(benchmarkData, reportResult, model, requestOptions);
            }
        }
        
        // Report
        if (games.Contains("report"))
        {
            await foreach (var reportResult in ChocolateTeamReportBenchmark.All_Benchmarks(secretValues, model, httpClient, requestOptions))
            {
                UpdateBenchmarkData(benchmarkData, reportResult, model, requestOptions);
            }
        }

        // Odd/Even
        if (games.Contains("oddeven"))
        {
            await foreach (var reportResult in ChocolateTeamOddEvenBenchmark.All_Benchmarks(secretValues, model, httpClient, requestOptions))
            {
                UpdateBenchmarkData(benchmarkData, reportResult, model, requestOptions);
            }
        }

        Console.WriteLine(WriteStatus(benchmarkData, maxRoundCount));
        Console.WriteLine($"Round {t} done");
        Console.WriteLine("");
    }

    SaveBenchmark(benchmarkData);
    allBenchmarkData = allBenchmarkData.Concat(benchmarkData).ToDictionary(x => x.Key, x => x.Value);
}

Console.WriteLine("Agent Benchmark complete!");
var benchmarkResults = WriteStatus(allBenchmarkData, maxRoundCount);
Console.WriteLine(benchmarkResults);
SaveTxtResults(benchmarkResults);


static void UpdateBenchmarkData(Dictionary<string, Dictionary<string, int>> benchmarkData, (string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult) result, string model, RequestOptions requestOptions)
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

    WriteBenchmarkConversation(benchmarkName, result.BenchmarkResult, model, result.BenchmarkConversationResult, requestOptions);
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

static void WriteBenchmarkConversation(string benchmarkName, string benchmarkResult, string model, List<ConversationResult> benchmarkConversationResult, RequestOptions requestOptions)
{
    var benchmarkFileName = $"{DataDirectory()}/{benchmarkName.Replace("/", "_").Replace(":", "_")}.json";
    bool fileExists = File.Exists(benchmarkFileName);
    var benchmarkConversationData = new BenchmarkConversationData(benchmarkName, benchmarkResult, model, requestOptions, benchmarkConversationResult.Select(x => new BenchmarkConversationResult(x.TurnCount, x.Conversation.Select(y => new BenchmarkConversationHistory(y.Agent.Name, y.Messages)).ToImmutableList())).ToImmutableList());

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

    Dictionary<string, Dictionary<string, int>>? fileData = null;

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
        AgentBenchmarkConventions.BenchmarkReasons.FailIncorrect,
        AgentBenchmarkConventions.BenchmarkReasons.FailIncorrectBecauseImpersonation,
        AgentBenchmarkConventions.BenchmarkReasons.FailMinimumTurnCount,
        AgentBenchmarkConventions.BenchmarkReasons.FailMaximumTurnCount,
    };

    using (StreamWriter writer = new StreamWriter(filePath, append: false))
    {
        // Write the header row
        writer.WriteLine("\"benchmark\",\"score\",\"round total\"," + string.Join(",", keys.Select(key => $"\"{key}\"")));

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
            var score = total > 0 ? ((double)pass / total) * 100 : 0;
            writer.WriteLine($"\"{item.Key}\",{score},{total}," + string.Join(",", values));
        }
    }
}

static void SaveTxtResults(string testResults)
{
    string filePath = $"{DataDirectory()}/agentbenchmark.txt";
    bool fileExists = File.Exists(filePath);

    using (StreamWriter writer = new StreamWriter(filePath, append: fileExists))
    {
        writer.WriteLine(testResults);
    }
}