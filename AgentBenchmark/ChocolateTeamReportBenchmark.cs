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
        public static async IAsyncEnumerable<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> All_Benchmarks(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions, string[] skipBenchmarks)
        {
            var selectors = SpeakerSelectors.All;
            var agents = ChocolateTeamAgents.All;
            List<(string GameName, string GamePrompt, string CheckAnswerPrompt)> games = [
               ChocolateTeamGames.ReportV1(secretValues),
                ChocolateTeamGames.ReportV2(secretValues)
            ];

            foreach (var game in games)
            {
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
}
