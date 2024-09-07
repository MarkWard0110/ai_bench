using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class ChocolateTeamReportBenchmark
    {       
        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> All_Benchmarks(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var selectors = SpeakerSelectors.All;
            var agents = ChocolateTeamAgents.All;
            var game = ChocolateTeamGames.ReportV1(secretValues);

            foreach (var selector in selectors)
            {
                foreach (var agent in agents)
                {
                    yield return await ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
                }
            }
        }
        public static Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ReportV1_BAIsicV1Selector_ChocolateTeamV1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.ReportV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV1Agent();

            return ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ReportV1_BAIsicV1Selector_ChocolateTeamV1_1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.ReportV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV1_1Agent();

            return ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ReportV1_BAIsicV1Selector_ChocolateTeamV2Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.ReportV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV2Agent();

            return ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ReportV1_BAIsicV1Selector_ChocolateTeamV2_1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.ReportV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV2_1Agent();

            return ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }

        public static Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ReportV1_BAIsicV1Selector_ChocolateTeamV3Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var game = ChocolateTeamGames.ReportV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV3Agent();

            return ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }
        public static Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ReportV1_BAIsicV1Selector_ChocolateTeamV3_1Agent_Benchmark(Dictionary<string, int> secretValues, string model, string checkModel, HttpClient httpClient, RequestOptions? requestOptions = null)
        {

            var game = ChocolateTeamGames.ReportV1(secretValues);
            var selector = SpeakerSelectors.BAIsicV1Selector();
            var agent = ChocolateTeamAgents.ChocolateTeamV3_1Agent();

            return ChocolateTeamGameEngine.RunBenchmark(secretValues, model, checkModel, httpClient, requestOptions, game, selector, agent);
        }
    }
}
