using BAIsic.Interlocutor;
using BAIsic.Interlocutor.Ollama;
using BAIsic.LlmApi.Ollama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class ChocolateTeamGameEngine
    {
        public static async Task<(string BenchmarkName, string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> RunBenchmark(Dictionary<string, int> secretValues, string model, HttpClient httpClient, RequestOptions requestOptions, (string GameName, string GamePrompt, string CheckAnswerPrompt) game, (string SelectorName, LlmSelectSpeakerAgentConfig Config) selector, (string AgentName, string TeamLeadSystemPrompt, string TeamMemberSystemPrompt) agent)
        {
            var benchmarkName = $"{game.GameName}/{selector.SelectorName}/{agent.AgentName}";
            Console.WriteLine($"Benchmark: {model}/{benchmarkName}");

            var numberTeamConfig = new ChocolateTeamConfig(
                TeamLeadSystemPrompt: agent.TeamLeadSystemPrompt,
                TeamMemberSystemPrompt: agent.TeamMemberSystemPrompt,
                SecretValues: secretValues,
                CheckAnswerPrompt: game.CheckAnswerPrompt,
                InitialPromptMessage: game.GamePrompt,
                SelectSpeakerAgentConfig: selector.Config
            );
            int attempt = 0;
            string bencharkResult = string.Empty;
            while (attempt < 3)
            {
                attempt++;
                try
                {
                    var result = await ChocolateTeamsGameEngine(model, httpClient, numberTeamConfig, requestOptions);
                    return (benchmarkName, result.BenchmarkResult, result.BenchmarkConversationResult);
                }
                catch (Exception ex)
                {
                    bencharkResult = $"Attempt {attempt} failed: {ex.Message}";
                    Console.WriteLine(bencharkResult);
                    await Task.Delay(1000);
                }
            }
           
            return (benchmarkName, bencharkResult, []);
        }

        public static async Task<(string BenchmarkResult, List<ConversationResult> BenchmarkConversationResult)> ChocolateTeamsGameEngine(string model, HttpClient httpClient, ChocolateTeamConfig chocolateTeamConfig, RequestOptions requestOptions)
        {
            List<IConversableAgent> agents = [];
            Dictionary<string, List<string>> speakerTransitionsDict = [];

            string[] teams = ["A", "B", "C"];
            var teamCount = 3;

            // Outer loop for prefixes 'A', 'B', 'C'
            foreach (var prefix in teams)
            {
                // generate the list of players in the team
                var playerList = string.Join(", ", Enumerable.Range(0, teamCount).Select(i => $"{prefix}{i}"));

                // Add 3 nodes with each prefix to the graph using a loop
                for (int i = 0; i < teamCount; i++)
                {
                    string nodeId = $"{prefix}{i}";
                    int secretValue = chocolateTeamConfig.SecretValues[nodeId];

                    // Create a ConversableAgent for each node 
                    var systemMessage = i == 0 ? chocolateTeamConfig.TeamLeadSystemPrompt : chocolateTeamConfig.TeamMemberSystemPrompt;
                    systemMessage = systemMessage.Replace("{nodeId}", nodeId);
                    systemMessage = systemMessage.Replace("{secretValue}", secretValue.ToString());
                    systemMessage = systemMessage.Replace("{teamMemberList}", playerList);
                    systemMessage = systemMessage.Replace("{teamName}", prefix);

                    agents.Add(new ConversableAgent(
                            name: nodeId,
                            systemPrompt: systemMessage,
                            description: systemMessage
                        ).AddOllamaGenerateReply(model, httpClient, requestOptions));

                    speakerTransitionsDict[agents.Last().Name] = [];
                }

                // Add edges between nodes with the same prefix using a nested loop
                for (int sourceNode = 0; sourceNode < 3; sourceNode++)
                {
                    string sourceId = $"{prefix}{sourceNode}";
                    for (int targetNode = 0; targetNode < 3; targetNode++)
                    {
                        string targetId = $"{prefix}{targetNode}";
                        if (sourceNode != targetNode)  // To avoid self-loops
                        {
                            speakerTransitionsDict[sourceId].Add(targetId);
                        }
                    }
                }
            }

            // Adding edges between teams
            speakerTransitionsDict["A0"].Add("B0");
            speakerTransitionsDict["A0"].Add("C0");
            speakerTransitionsDict["B0"].Add("A0");
            speakerTransitionsDict["B0"].Add("C0");
            speakerTransitionsDict["C0"].Add("A0");
            speakerTransitionsDict["C0"].Add("B0");

            // setup each agent's list of agents it can chat with.
            foreach (var agent in agents)
            {
                // get a list of the transitions for this agent as a string
                var transitionList = string.Join(",", speakerTransitionsDict[agent.Name]);
                agent.SystemPrompt = agent.SystemPrompt!.Replace("{transitionList}", transitionList);
            }

            var selectSpeakerAgent = new LlmSelectSpeakerAgent("selectSpeaker", chocolateTeamConfig.SelectSpeakerAgentConfig)
                .AddOllamaGenerateReply(model, httpClient, requestOptions);

            var agentSelectSpeaker = new LlmSpeakerSelector(selectSpeakerAgent, 30);


            static Task<bool> Terminate(bool isInitialMessage, IAgent agent, BAIsic.Interlocutor.Message message)
            {
                return Task.FromResult(message.Text.Contains("TERMINATE", StringComparison.OrdinalIgnoreCase));
            }

            var initiatorAgent = new ConversableAgent("conversationInitiator", "Conversation initiator");

            agents.Add(initiatorAgent);
            speakerTransitionsDict[initiatorAgent.Name] = ["A0"];

            int maxTurnCount = 20;
            int minimumTurnCount = 12;
            var groupConversation = new GroupConversation(agents, agentSelectSpeaker.SelectSpeakerAsync, allowedTransitions: speakerTransitionsDict, maxTurnCount: maxTurnCount, terminationHandler: Terminate);

            var initialMessage = new BAIsic.Interlocutor.Message(AgentConsts.Roles.User, chocolateTeamConfig.InitialPromptMessage);

            var result = await groupConversation.InitiateChatAsync(initiatorAgent, initialMessage);

            string failReason = string.Empty;
            bool isFail = false;
            if (result == null || result[0] == null)
            {
                throw new Exception("The group chat did not return conversation history");
            }
            else if (result[0].TurnCount < minimumTurnCount)
            {
                failReason = AgentBenchmarkConventions.BenchmarkReasons.FailMinimumTurnCount;
                isFail = true;
            }
            else if (maxTurnCount == result[0].TurnCount)
            {
                failReason = AgentBenchmarkConventions.BenchmarkReasons.FailMaximumTurnCount;
                isFail = true;
            }
            //else if (!result[0].Conversation.First().Messages.Last().Text.Contains("TERMINATE", StringComparison.OrdinalIgnoreCase))
            //{
            //    failReason = AgentBenchmarkConventions.BenchmarkReasons.FailNoTerminate;
            //    isFail = true;
            //}

            List<ConversationResult> benchmarkConversationResult = [.. result];

            if (!isFail)
            {
                var checkAnswer = await IsAnswerCorrectSmallLLM(result[0].Conversation.First().Messages.Last(), chocolateTeamConfig.CheckAnswerPrompt, httpClient, requestOptions);
                if (checkAnswer.CheckConversationResult != null)
                {
                    benchmarkConversationResult.AddRange(checkAnswer.CheckConversationResult);
                }

                if (!checkAnswer.IsCorrect)
                {
                    failReason = AgentBenchmarkConventions.BenchmarkReasons.FailIncorrect;
                    isFail = true;

                    // identify a reason it failed to get the correct answer
                    //var impersonationAnswer = await IsImpersonating(result, checkModel, httpClient, requestOptions);

                    //if (impersonationAnswer.CheckConversationResults != null)
                    //{
                    //    benchmarkConversationResult.AddRange(impersonationAnswer.CheckConversationResults);
                    //}

                    //if (impersonationAnswer.IsImpersonating)
                    //{
                    //    failReason = AgentBenchmarkConventions.BenchmarkReasons.FailNotCorrectBecauseImpersonation;
                    //    isFail = true;
                    //}
                }                
            }



            if (isFail)
            {
                Console.WriteLine($"Benchmark: {failReason}");
                return (failReason, benchmarkConversationResult);
            }
            else
            {
                Console.WriteLine($"Benchmark: passed");
                return (AgentBenchmarkConventions.BenchmarkReasons.Pass, benchmarkConversationResult);
            }
        }

        private static async Task<(bool IsCorrect, ConversationResult? CheckConversationResult)> IsAnswerCorrect(string numbersAnswer, string checkAnswerPrompt, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var model = "bespoke-minicheck:7b-fp16";

            var initiatorAgent = new Agent("feeder");

            var llmAgent = new Agent("llm")
                .AddOllamaGenerateReply(model, httpClient, requestOptions ?? new RequestOptions());

            BAIsic.Interlocutor.Message claimCheck = new(AgentConsts.Roles.User, checkAnswerPrompt.Replace("{claimAnswer}", numbersAnswer));

            var conversation = new DialogueConversation();
            var conversationResult = await conversation.InitiateChat(initiatorAgent, claimCheck, llmAgent, maximumTurnCount: 1);

            if (conversationResult.Conversation.Last().Messages.Last().Text.Contains("no", StringComparison.OrdinalIgnoreCase))
            {
                // The answer was classified as not correct
                return (false, conversationResult);
            }
            else if (conversationResult.Conversation.Last().Messages.Last().Text.Contains("yes", StringComparison.OrdinalIgnoreCase))
            {   // The answer was classified as correct
                return (true, conversationResult);
            }
            else
            {
                throw new NotImplementedException();
                // The answer was not classified as correct or not correct
                //return false;
            }

        }

        private static async Task<(bool IsCorrect, List<ConversationResult>? CheckConversationResult)> IsAnswerCorrectSmallLLM(BAIsic.Interlocutor.Message numbersAnswer, string checkAnswerPrompt, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            // get answers (in format for the check answers)
            var answer = await GetAnswer(numbersAnswer, httpClient, requestOptions);  

            // check the answer
            var checkAnswer = await IsAnswerCorrect(answer.AnswerText, checkAnswerPrompt, httpClient!, requestOptions);

            List<ConversationResult> conversationResult = new List<ConversationResult>();
            if (answer.ConversationResult != null)
            {
                conversationResult.Add(answer.ConversationResult);
            }
            if (checkAnswer.CheckConversationResult != null)
            {
                conversationResult.Add(checkAnswer.CheckConversationResult);
            }

            return (checkAnswer.IsCorrect, conversationResult);
        }

        private static async Task<(string AnswerText, ConversationResult? ConversationResult)> GetAnswer(BAIsic.Interlocutor.Message numbersAnswer, HttpClient httpClient, RequestOptions? requestOptions = null)
        {
            var model = "llama3.1:8b-instruct-q8_0";
            var initiatorAgent = new Agent("feeder");
            var getAnswerPrompt = @"The user has provided a JSON-formatted answer.
Output only the JSON provided by the user without making any changes or assumptions.

Without a greeting or additional information.";

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            requestOptions.NumCtx = 128;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            var llmAgent = new Agent("llm", getAnswerPrompt)
                .AddOllamaGenerateReply(model, httpClient, new OllamaOptions() {  RequestOptions = requestOptions, ResponseFormat = "json"});
            requestOptions.NumCtx = 2048;

            var conversation = new DialogueConversation();
            var conversationResult = await conversation.InitiateChat(initiatorAgent, numbersAnswer, llmAgent, maximumTurnCount: 1);

            return (conversationResult.Conversation.Last().Messages.Last().Text, conversationResult);

        }

        private static async Task<(bool IsImpersonating, List<ConversationResult>? CheckConversationResults)> IsImpersonating(List<ConversationResult> groupChatConversations, string model, HttpClient httpClient, RequestOptions requestOptions)
        {
            string[] teams = ["A", "B", "C"];
            var teamCount = 3;
            List<ConversationResult> conversationResults = [];

            // get the first 9 conversations from groupChatConversations
            var teamConversations = groupChatConversations[0].Conversation.Take(teams.Length * teamCount).ToList();

            foreach (var prefix in teams)
            {
                // generate the list of players in the team
                var playerList = string.Join(", ", Enumerable.Range(0, teamCount).Select(i => $"{prefix}{i}"));

                for (int i = 0; i < teamCount; i++)
                {
                    string nodeId = $"{prefix}{i}";
            
                    var memberConversation = teamConversations.FirstOrDefault(c => c.Agent.Name == nodeId);
                    if (memberConversation == null)
                    {
                        throw new Exception($"Conversation for {nodeId} not found");
                    }
                    var assistantMessages = memberConversation.Messages.Where(m => m.Role == AgentConsts.Roles.Assistant).ToList();
                    if (assistantMessages.Count == 0)
                    {
                        continue;
                    }

                    StringBuilder assistantMessage = new StringBuilder();
                    foreach (var message in assistantMessages)
                    {
                        assistantMessage.AppendLine("assistant:");
                        assistantMessage.AppendLine(message.Text);
                        assistantMessage.AppendLine();
                    }

                    // use agents to check if the conversation contains impersonation
                    var initiatorAgent = new Agent("feeder")
                        .AddStringLiteralGenerateReply("Respond with only the classification");

                    var initialMessage = new BAIsic.Interlocutor.Message(AgentConsts.Roles.User, assistantMessage.ToString());

                    var llmAgent = new Agent("llm", systemPrompt: $@"Task:
  1. Review the conversation and list the name the assistant identifies as.
  2. Classify the conversation: 
    * Incorrect: Classify the conversation as incorrect if the assistant identified other than ""{nodeId}"". 
    * Correct: Classify it as correct if every time the assistant identified as ""{nodeId}"".
")
                        .AddOllamaGenerateReply(model, httpClient, requestOptions);

                    
                    var conversation = new DialogueConversation();
                    var conversationResult = await conversation.InitiateChat(initiator:initiatorAgent, initialMessage: initialMessage, participant:llmAgent, maximumTurnCount: 2);
                    conversationResults.Add(conversationResult);

                    if (conversationResult.Conversation.Last().Messages.Last().Text.Contains("incorrect", StringComparison.OrdinalIgnoreCase))
                    {
                        // The answer was classified as not correct - having impersonation
                        return (true, conversationResults);
                    }
                    else if (!conversationResult.Conversation.Last().Messages.Last().Text.Contains("correct", StringComparison.OrdinalIgnoreCase))
                    {
                        // The answer was not classified as correct or not correct
                        throw new NotImplementedException();
                    }
                }

            }

            return (false, conversationResults);
        }
    }
}
