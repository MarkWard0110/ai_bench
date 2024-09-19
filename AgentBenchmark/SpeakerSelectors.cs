using BAIsic.Interlocutor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class SpeakerSelectors
    {
        public static readonly List<(string SelectorName, LlmSelectSpeakerAgentConfig Config)> All = [
            // AutoGen has been delisted due to its performance with smaller models
            BAIsicV1Selector(),
        ];

        public static readonly List<(string SelectorName, LlmSelectSpeakerAgentConfig Config)> AllWithAutoGen = [
            AutoGenSelector(),
            BAIsicV1Selector(),
        ];
        public static (string SelectorName, LlmSelectSpeakerAgentConfig Config) AutoGenSelector()
        {
            LlmSelectSpeakerAgentConfig config = new LlmSelectSpeakerAgentConfig()
            {
                SelectSpeakerSystemMessageTemplate = @"You are in a role play game. The following roles are available:
{roles}.
Read the following conversation.
Then select the next role from {agentlist} to play. Only return the role.",

                NoneSelectedPrompt = @"You didn't choose a speaker. As a reminder, to determine the speaker use these prioritised rules:
1. If the context refers to themselves as a speaker e.g. ""As the..."" , choose that speaker's name
2. If it refers to the ""next"" speaker name, choose that name
3. Otherwise, choose the first provided speaker's name in the context
The names are case-sensitive and should not be abbreviated or changed.
The only names that are accepted are {agentlist}.
Respond with ONLY the name of the speaker and DO NOT provide a reason.",

                SelectSpeakerPrompt = "Read the above conversation. Then select the next role from {agentlist} to play. Only return the role.",
            };

            config.ManySelectedPrompt = config.NoneSelectedPrompt;
            return ("AutoGenSelector", config);
        }

        public static (string SelectorName, LlmSelectSpeakerAgentConfig Config) BAIsicV1Selector()
        {
            LlmSelectSpeakerAgentConfig config = new LlmSelectSpeakerAgentConfig()
            {
                SelectSpeakerSystemMessageTemplate =
@"You are a coordinator responsible for managing a group of agents. Your primary task is to choose which agent should be called next based on the user's instructions and the context of the conversation.

You have a list of agent names that you may choose from: [{agentlist}].

Critical Rule:

If the user's prompt explicitly specifies ""next"" and an agent name follows ""next"" and that name matches an agent in your list, you must select that agent, without exception. This rule applies even if other agents were mentioned earlier in the conversation.

Secondary Rules:

If the user's suggestion does not match any names in the list, ignore the user's suggestion and select the most contextually appropriate agent from the list.
If the user does not provide any explicit instructions, choose the agent that best continues the flow of the conversation, ensuring logical progression.

Only return the agent's name and nothing else.",

                NoneSelectedPrompt =
@"Is this agent in the list of agent names that was given to you?
You didn't choose a valid agent name. You have a list of agent names that you may choose from: [{agentlist}].  As a reminder, to determine the next agent use these prioritized rules:
1. If the context refers to themselves as a speaker e.g. ""As the..."" , choose that agent's name.
2. If it refers to the ""next"" agent name and the name is in the list, choose that name.
3. Otherwise, choose one of the provided agent's name on behalf of the user.
4. Do not answer with the invalid agent name.
The names are case-sensitive and should not be abbreviated or changed.
The only names that are accepted are [{agentlist}].
Respond with ONLY the name of the agent and DO NOT provide a reason.",
                SelectSpeakerPrompt = string.Empty
            };

            config.ManySelectedPrompt = config.NoneSelectedPrompt;

            return ("bAIsicV1Selector", config);
        }
    }
}
