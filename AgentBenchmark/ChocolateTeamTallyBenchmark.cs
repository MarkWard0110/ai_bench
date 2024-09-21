using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class ChocolateTeamTallyBenchmark
    {
        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> All_Benchmarks(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions)
        {
            var selectors = SpeakerSelectors.All;
            var agents = ChocolateTeamAgents.All;
            List<(string GameName, string GamePrompt, string CheckAnswerPrompt)> games = [
                ChocolateTeamGames.TallyV1(secretValues),
                ChocolateTeamGames.TallyV2(secretValues),
                ];

            foreach (var game in games)
            {
                foreach (var selector in selectors)
                {
                    foreach (var agent in agents)
                    {
                        yield return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, httpClient, requestOptions, game, selector, agent);
                    }
                }
            }
        }

        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> AllAndAutoGen_Benchmarks(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions)
        {
            var selectors = SpeakerSelectors.All;
            var agents = ChocolateTeamAgents.All;
            List<(string GameName, string GamePrompt, string CheckAnswerPrompt)> games = [
                ChocolateTeamGames.TallyV1(secretValues),
                ChocolateTeamGames.TallyV2(secretValues),
                ];

            // AutoGen agents are not included in the list of All agents.
            yield return await AutoGenTally_AutoGenSelector_AutoGenAgent_Benchmark(secretValues, model, httpClient, requestOptions);
            yield return await AutoGenTally_BAIsicV1Selector_AutoGenV2Agent_Benchmark(secretValues, model, httpClient, requestOptions);

            foreach (var game in games)
            {
                foreach (var selector in selectors)
                {
                    foreach (var agent in agents)
                    {
                        yield return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, httpClient, requestOptions, game, selector, agent);
                    }
                }
            }
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> AutoGenTally_AutoGenSelector_AutoGenAgent_Benchmark(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions)
        {
            var game = ChocolateTeamGames.AutoGenTally(secretValues);
            var selector = SpeakerSelectors.AutoGenSelector();
            var agent = ChocolateTeamAgents.AutoGenAgent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> AutoGenTally_BAIsicV1Selector_AutoGenV2Agent_Benchmark(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions)
        {
            var game = ChocolateTeamGames.AutoGenTally(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.AutoGenV2Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, httpClient, requestOptions, game, selector, agent);
        }

    }
}
