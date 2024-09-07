using BAIsic.Interlocutor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public record ChocolateTeamConfig(
        string TeamLeadSystemPrompt,
        string TeamMemberSystemPrompt,
        Dictionary<string, int> SecretValues,
        string CheckAnswerPrompt,
        string InitialPromptMessage,
        LlmSelectSpeakerAgentConfig SelectSpeakerAgentConfig
        );
    
}
