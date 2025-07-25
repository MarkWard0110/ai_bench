﻿using AgentBenchmark;
using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace AgentBenchmark
{
    public class Program
    {
        const string LabelSeperator = " | ";

        static string DataLabel = string.Empty;
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Agent Benchmark!");
            const int TimeoutMinutes = 30;


            var labelOption = new Option<string?>(
                  name: "--label",
                  description: "A label for the benchmark session.  It is the name of the data directory.",
                  isDefault: true,
                  parseArgument: result =>
                  {
                      if (result.Tokens.Count == 0)
                      {
                          return "default";
                      }
                      return result.Tokens[0].Value;
                  });

            var optionsCfgOption = new Option<string?>(
                  name: "--optionsCfg",
                  description: "The chat request options configuration file.",
                  isDefault: true,
                  parseArgument: result =>
                  {
                      if (result.Tokens.Count == 0)
                      {
                          return "config\\default-options.json";
                      }
                      return result.Tokens[0].Value;
                  });

            var rootCommand = new RootCommand();
            rootCommand.AddOption(labelOption);
            rootCommand.AddOption(optionsCfgOption);


            rootCommand.SetHandler(async (label, optionsCfg) =>
            {
                DataLabel = label!;
                await RunBenchmarkSession(TimeoutMinutes, optionsCfg!);

            }, labelOption, optionsCfgOption);

            await rootCommand.InvokeAsync(args);
        }

        static void UpdateBenchmarkData((string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult) result, string model, RequestOptions requestOptions)
        {

            var benchmarkName = result.BenchmarkName;
            Dictionary<string, Dictionary<string, int>> benchmarkData = new();
            benchmarkData[benchmarkName] = new Dictionary<string, int>
            {
                [result.BenchmarkResult] = 1
            };

            WriteBenchmarkConversation(benchmarkName, result.BenchmarkResult, model, result.BenchmarkConversationResult, requestOptions);
            SaveBenchmark(benchmarkData);
        }

        static string DataDirectory()
        {
            // Create a directory to store the benchmark data
            string dataDirectory = Path.Combine("data", DataLabel);

            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
            return dataDirectory;
        }

        static void WriteBenchmarkConversation(string benchmarkName, string benchmarkResult, string model, List<ConversationResult> benchmarkConversationResult, RequestOptions requestOptions)
        {
            var benchmarkFileName = $"{DataDirectory()}/{benchmarkName.Replace("/", "_").Replace(":", "_").Replace(LabelSeperator, "_")}.json";
            bool fileExists = File.Exists(benchmarkFileName);
            var benchmarkConversationData = new BenchmarkConversationData(benchmarkName, benchmarkResult, model, requestOptions, benchmarkConversationResult.Select(x => new BenchmarkConversationResult(x.TurnCount, x.Conversation.Select(y => new BenchmarkConversationHistory(y.Agent.Name, y.Messages)).ToImmutableList())).ToImmutableList());

            using (StreamWriter writer = new StreamWriter(benchmarkFileName, append: fileExists))
            {
                string jsonData = JsonSerializer.Serialize(benchmarkConversationData, options: new JsonSerializerOptions { WriteIndented = false });
                writer.WriteLine(jsonData);
            }
        }

        static void SaveBenchmark(Dictionary<string, Dictionary<string, int>> testResults)
        {
            Dictionary<string, Dictionary<string, int>> fileData = ReadBenchmarkData();

            // Merge the existing data with the new data
            foreach (var item in testResults)
            {
                var roundCount = 0;
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

                    foreach (var result in existingItem)
                    {
                        if (result.Key == "roundCount")
                        {
                            continue; // Skip roundCount key
                        }
                        roundCount += result.Value;
                    }
                    existingItem["roundCount"] = roundCount;
                }
                else
                {
                    fileData[item.Key] = item.Value;

                    foreach (var result in item.Value)
                    {
                        if (result.Key == "roundCount")
                        {
                            continue; // Skip roundCount key
                        }
                        roundCount += result.Value;
                    }
                    fileData[item.Key]["roundCount"] = roundCount;
                }
            }

            string filePath = $"{DataDirectory()}/agentbenchmark.json";
            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                string jsonData = JsonSerializer.Serialize(fileData, options: new JsonSerializerOptions { WriteIndented = true });
                writer.WriteLine(jsonData);
            }

            SaveBenchmarkCsv(fileData);
            SaveBenchmarkScoreCsv(fileData);
        }

        private static Dictionary<string, Dictionary<string, int>> ReadBenchmarkData()
        {
            string filePath = $"{DataDirectory()}/agentbenchmark.json";
            bool fileExists = File.Exists(filePath);

            Dictionary<string, Dictionary<string, int>>? fileData = null;
            if (fileExists)
            {
                var fileJson = File.ReadAllText(filePath);
                fileData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(fileJson);
            }

            fileData ??= [];

            return fileData;
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


        static void SaveBenchmarkScoreCsv(Dictionary<string, Dictionary<string, int>> testResults)
        {
            string filePath = $"{DataDirectory()}/benchmarkscore.csv";
            SortedSet<string> models = new();
            SortedSet<string> benchmarks = new();

            foreach (var item in testResults)
            {
                var slashIndex = item.Key.IndexOf($"{LabelSeperator}ChocolateTeam/");
                var model = item.Key.Substring(0, slashIndex);
                models.Add(model);
                var benchmark = item.Key.Substring(slashIndex + 1);
                benchmarks.Add(benchmark);
            }

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
                writer.WriteLine("\"model\"," + string.Join(",", benchmarks.Select(benchmark => $"\"{benchmark}\"")));
                foreach (var model in models)
                {
                    var record = new List<double>();
                    foreach (var benchmark in benchmarks)
                    {
                        var key = $"{model}{LabelSeperator}{benchmark}";
                        if (testResults.TryGetValue(key, out var item))
                        {
                            int pass = 0;
                            int total = 0;

                            foreach (var vkey in keys)
                            {
                                if (item.TryGetValue(vkey, out var value))
                                {
                                    total += value;
                                    if (vkey == AgentBenchmarkConventions.BenchmarkReasons.Pass)
                                    {
                                        pass = value;
                                    }
                                }
                            }
                            var score = total > 0 ? ((double)pass / total) * 100 : 0;
                            record.Add(score);
                        }
                        else
                        {
                            record.Add(0); // Add 0 value for missing keys
                        }
                    }
                    writer.WriteLine($"\"{model}\"," + string.Join(",", record));
                }
            }
        }

        static string[] ReadConfigFile(string path)
        {
            return File.ReadAllLines(path)
                       .Select(line => line.Trim().ToLower())
                       .Where(line => !line.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(line))
                       .ToArray();
        }

        static async Task RunBenchmarkSession(int TimeoutMinutes, string optionsCfg)
        {

            string[] models = ReadConfigFile("config\\models.txt");
            string[] games = ReadConfigFile("config\\games.txt");
            string rounds = File.ReadAllText("config\\rounds.txt").Trim();
            string[] dataMode = ReadConfigFile("config\\datamode.txt");
            string[] skipBenchmarks = ReadConfigFile("config\\skip-benchmarks.txt");
            Dictionary<string, Dictionary<string, int>> resumeBenchmarkData = ReadBenchmarkData(); // only used for resume benchmark logic

            RequestOptions? cfgOptions = JsonSerializer.Deserialize<RequestOptions>(File.ReadAllText(optionsCfg));
            if (cfgOptions == null)
            {
                throw new InvalidOperationException("Failed to deserialize RequestOptions from the configuration file.");
            }
            else
            {
                Console.WriteLine($"RequestOptions: {JsonSerializer.Serialize(cfgOptions)}");
            }

            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://quorra.homelan.binaryward.com:11434"),
                Timeout = TimeSpan.FromMinutes(TimeoutMinutes)
            };
            // random number generator for positive int 
            var random = new Random();

            string[] teams = ["A", "B", "C"];
            var teamCount = 3;

            int maxRoundCount = int.Parse(rounds);

            Console.WriteLine($"Data Label: {DataLabel}");

            IEnumerator<int[]>? setIterator = null;
            bool isRandomMode = false;
            if (dataMode.Contains("sequence"))
            {
                setIterator = GenerateSets(9, 1, 5).GetEnumerator();
            }
            else if (dataMode.Contains("random"))
            {
                isRandomMode = true;
                setIterator = GenerateRandomSets(9, 1, 5).GetEnumerator();
            }
            else if (dataMode.Contains("randomset"))
            {
                setIterator = File.ReadAllLines($"config/randomset.csv")
                    .Select(line => line.Split(',').Select(int.Parse).ToArray())
                    .GetEnumerator();
            }
            else
            {
                throw new Exception("Invalid data mode");
            }

            Stopwatch allStopWatch = new Stopwatch();
            allStopWatch.Start();
            for (int currentRound = 1; currentRound <= maxRoundCount; currentRound++)
            {
                Stopwatch roundStopWatch = new Stopwatch();
                roundStopWatch.Start();
                Console.WriteLine($"Round {currentRound}");
                if (!setIterator.MoveNext())
                {
                    Console.WriteLine("! End of data");
                    break;
                }

                var currentSet = setIterator.Current;
                if (isRandomMode)
                {
                    SaveRandomSet(currentSet);
                }

                foreach (var model in models)
                {
                    Console.WriteLine($"Model: {model}");
                    int randomPositiveInt = random.Next(1, int.MaxValue);
                    var requestOptions = new RequestOptions()
                    {
                        Seed = randomPositiveInt,

                        FrequencyPenalty = cfgOptions.FrequencyPenalty,
                        LogitsAll = cfgOptions.LogitsAll,
                        LowVram = cfgOptions.LowVram,
                        MainGpu = cfgOptions.MainGpu,
                        MinP = cfgOptions.MinP,
                        MiroStat = cfgOptions.MiroStat,
                        MiroStatEta = cfgOptions.MiroStatEta,
                        MiroStatTau = cfgOptions.MiroStatTau,
                        NumBatch = cfgOptions.NumBatch,
                        NumCtx = cfgOptions.NumCtx,
                        NumGpu = cfgOptions.NumGpu,
                        NumKeep = cfgOptions.NumKeep,
                        NumPredict = cfgOptions.NumPredict,
                        NumThread = cfgOptions.NumThread,
                        PresencePenalty = cfgOptions.PresencePenalty,
                        RepeatLastN = cfgOptions.RepeatLastN,
                        RepeatPenalty = cfgOptions.RepeatPenalty,
                        Stop = cfgOptions.Stop,
                        Temperature = cfgOptions.Temperature,
                        TopK = cfgOptions.TopK,
                        TopP = cfgOptions.TopP,
                        TypicalP = cfgOptions.TypicalP,
                        UseMLock = cfgOptions.UseMLock,
                        VocabOnly = cfgOptions.VocabOnly
                    };

                    Dictionary<string, int> secretValues = [];
                    var playerIndex = 0;
                    foreach (var prefix in teams)
                    {
                        for (int i = 0; i < teamCount; i++)
                        {
                            string nodeId = $"{prefix}{i}";
                            int secretValue = currentSet[playerIndex++];
                            secretValues[nodeId] = secretValue;
                        }
                    }


                    Stopwatch modelStopwatch = new Stopwatch();
                    modelStopwatch.Start();
                    // Tally
                    if (games.Contains("tally"))
                    {
                        await foreach (var reportResult in ChocolateTeamTallyBenchmark.All_Benchmarks(secretValues, model, httpClient, requestOptions, skipBenchmarks, currentRound, resumeBenchmarkData))
                        {
                            UpdateBenchmarkData(reportResult, model, requestOptions);
                        }
                    }

                    // Report
                    if (games.Contains("report"))
                    {
                        await foreach (var reportResult in ChocolateTeamReportBenchmark.All_Benchmarks(secretValues, model, httpClient, requestOptions, skipBenchmarks, currentRound, resumeBenchmarkData))
                        {
                            UpdateBenchmarkData(reportResult, model, requestOptions);
                        }
                    }

                    // Odd/Even
                    if (games.Contains("oddeven"))
                    {
                        await foreach (var reportResult in ChocolateTeamOddEvenBenchmark.All_Benchmarks(secretValues, model, httpClient, requestOptions, skipBenchmarks, currentRound, resumeBenchmarkData))
                        {
                            UpdateBenchmarkData(reportResult, model, requestOptions);
                        }
                    }

                    modelStopwatch.Stop();                
                    TimeSpan ts = modelStopwatch.Elapsed;

                    string elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
                    Console.WriteLine($"Model: {model} execution time: {elapsedTime}");
                }

                roundStopWatch.Stop();
                TimeSpan roundElapsedTime = roundStopWatch.Elapsed;
                string roundElapsed = $"{roundElapsedTime.Hours:00}:{roundElapsedTime.Minutes:00}:{roundElapsedTime.Seconds:00}.{roundElapsedTime.Milliseconds / 10:00}";
                Console.WriteLine($"Round {currentRound} done.  Execution time: {roundElapsed}");
                Console.WriteLine("");
            }

            allStopWatch.Stop();
            TimeSpan allElapsedTime = allStopWatch.Elapsed;
            string allElapsed = $"{allElapsedTime.Days:00}-{allElapsedTime.Hours:00}:{allElapsedTime.Minutes:00}:{allElapsedTime.Seconds:00}.{allElapsedTime.Milliseconds / 10:00}";
            Console.WriteLine($"All rounds done.  Execution time: {allElapsed}");
            Console.WriteLine("Agent Benchmark complete!");

        }

        private static void SaveRandomSet(int[] currentSet)
        {
            string filePath = $"{DataDirectory()}/randomset.csv";

            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine(string.Join(",", currentSet));
            }
        }


        static IEnumerable<int[]> GenerateSets(int arrayLength, int minValue, int maxValue)
        {
            int totalSets = (int)Math.Pow(maxValue - minValue + 1, arrayLength);

            for (int i = 0; i < totalSets; i++)
            {
                int[] set = new int[arrayLength];
                int temp = i;

                for (int position = 0; position < arrayLength; position++)
                {
                    set[position] = (temp % (maxValue - minValue + 1)) + minValue;
                    temp /= (maxValue - minValue + 1);
                }

                yield return set;
            }
        }

        static IEnumerable<int[]> GenerateRandomSets(int arrayLength, int minValue, int maxValue)
        {
            int totalSets = (int)Math.Pow(maxValue - minValue + 1, arrayLength);

            Random random = new Random();
            for (int i = 0; i < totalSets; i++)
            {
                int[] set = new int[arrayLength];

                for (int position = 0; position < arrayLength; position++)
                {
                    set[position] = random.Next(minValue, maxValue + 1);
                }

                yield return set;
            }
        }
    }
}