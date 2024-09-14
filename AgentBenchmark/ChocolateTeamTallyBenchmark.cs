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
        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> All_Benchmarks(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
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
                        yield return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
                    }
                }
            }
        }

        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> AllAndAutoGen_Benchmarks(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var selectors = SpeakerSelectors.All;
            var agents = ChocolateTeamAgents.All;
            List<(string GameName, string GamePrompt, string CheckAnswerPrompt)> games = [
                ChocolateTeamGames.TallyV1(secretValues),
                ChocolateTeamGames.TallyV2(secretValues),
                ];

            // AutoGen agents are not included in the list of All agents.
            yield return await AutoGenTally_AutoGenSelector_AutoGenAgent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions);
            yield return await AutoGenTally_BAIsicV1Selector_AutoGenV2Agent_Benchmark(secretValues, model, checkModel, httpClient, requestOptions);

            foreach (var game in games)
            {
                foreach (var selector in selectors)
                {
                    foreach (var agent in agents)
                    {
                        yield return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
                    }
                }
            }
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> AutoGenTally_AutoGenSelector_AutoGenAgent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.AutoGenTally(secretValues);
            var selector = SpeakerSelectors.AutoGenSelector();
            var agent = ChocolateTeamAgents.AutoGenAgent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> AutoGenTally_BAIsicV1Selector_AutoGenV2Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.AutoGenTally(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.AutoGenV2Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> TallyV1_BAIsicV1Selector_ChocolateTeamV1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.TallyV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV1Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> TallyV1_BAIsicV1Selector_ChocolateTeamV1_1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.TallyV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV1_1Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> TallyV2_BAIsicV1Selector_ChocolateTeamV2Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.TallyV2(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV2Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> TallyV2_BAIsicV1Selector_ChocolateTeamV2_1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.TallyV2(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV2_1Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> TallyV2_BAIsicV1Selector_ChocolateTeamV3Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.TallyV2(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV3Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }
        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> TallyV2_BAIsicV1Selector_ChocolateTeamV3_1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {

            var game = ChocolateTeamGames.TallyV2(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV3_1Agent();

            return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }
    }
}
