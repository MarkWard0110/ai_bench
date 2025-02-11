﻿using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class ChocolateTeamOddEvenBenchmark
    {
        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> All_Benchmarks(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions, string[] skipBenchmarks)
        {
            var selectors = SpeakerSelectors.All;
            var agents = ChocolateTeamAgents.All;
            var game = ChocolateTeamGames.OddEvenV1(secretValues);

            foreach (var selector in selectors)
            {
                foreach (var agent in agents)
                {
                    if (ChocolateTeamGameEngine.SkipBenchmark(skipBenchmarks, model, game, selector, agent))
                    {
                        continue;
                    }
                    yield return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, httpClient, requestOptions, game, selector, agent);
                }
            }
        }
    }
}
