using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class ChocolateTeamGames
    {
        const string RootGameName = "ChocolateTeam";

        public static (string GameName, string GamePrompt, string CheckAnswerPrompt) AutoGenTally(Dictionary<string, int> secretValues)
        {
            var expectedAnswer = BuildCheckAnswerPrompt(BuildTallyAnswer(secretValues));

            return ($"{RootGameName}/AutoGenTally", @"There are 9 players in this game, split equally into Teams A, B, C. Therefore each team has 3 players, including the team leader.
The task is to find out the sum of chocolate count from all nine players. I will now start with the A0 team leader.
NEXT: A0", expectedAnswer);
        }

        public static (string GameName, string GamePrompt, string CheckAnswerPrompt) TallyV1(Dictionary<string, int> secretValues)
        {
            var expectedAnswer = BuildCheckAnswerPrompt(BuildTallyAnswer(secretValues));

            return ($"{RootGameName}/TallyV1", @"There are 9 players in this game, split equally into Teams A, B, C. Therefore, each team has 3 players, including the team leader.
The task is to find out the chocolate count from all nine players.  Each team lead must call on another team after they have their team's total.
Every player must keep track of all player's tally using a JSON format.  Every player must answer with the JSON format
{
A0:?
A1:?
A2:?
TeamATotal:?
B0:?
B1:?
B2:?
TeamBTotal:?
C0:?
C1:?
C2:?
TeamCTotal:?
}

The termination must include the JSON format with all the player's tally.

An example of team leader's answer:
I'm B0, a leader of Team B.  I have 1 chocolate.
{
A0:3
A1:5
A2:2
TeamATotal:10
B0:1
B1:?
B2:?
TeamBTotal:?
C0:?
C1:?
C2:?
TeamCTotal:?
}
NEXT: B1
", expectedAnswer);
        }

        public static (string GameName, string GamePrompt, string CheckAnswerPrompt) TallyV2(Dictionary<string, int> secretValues)
        {
            var expectedAnswer = BuildCheckAnswerPrompt(BuildTallyAnswer(secretValues));

            return ($"{RootGameName}/TallyV2", @"There are 9 players in this game, split equally into Teams A, B, C. Therefore, each team has 3 players, including the team leader.
The task is to find out the chocolate count from all nine players.  Each team lead must call on another team after they have their team's total.
Every player must keep track of all player's tally using a JSON format.  Every player must answer with the JSON format
{
A0:?
A1:?
A2:?
TeamATotal:?
B0:?
B1:?
B2:?
TeamBTotal:?
C0:?
C1:?
C2:?
TeamCTotal:?
}

The termination must include the JSON format with all the player's tally.

An example of team member's answer:
I'm A2, a member of Team A.  I have 2 chocolates.
{
A0:3
A1:5
A2:2
TeamATotal:10
B0:?
B1:?
B2:?
TeamBTotal:?
C0:?
C1:?
C2:?
TeamCTotal:?
}
NEXT: A0

An example of team leader's answer:
I'm B0, a leader of Team B.  I have 1 chocolate.
{
A0:3
A1:5
A2:2
TeamATotal:10
B0:1
B1:?
B2:?
TeamBTotal:?
C0:?
C1:?
C2:?
TeamCTotal:?
}
NEXT: B1
", expectedAnswer);
        }

        public static (string GameName, string GamePrompt, string CheckAnswerPrompt) ReportV1(Dictionary<string, int> secretValues)
        {
            var expectedAnswer = BuildCheckAnswerPrompt(BuildReportAnswer(secretValues));

            return ($"{RootGameName}/ReportV1", @"There are 9 players in this game, split equally into Teams A, B, C. Therefore, each team has 3 players, including the team leader.
The task is to find out the chocolate count from all nine players.  Each team lead must call on another team after they have completed.
Every player must keep track of all player's count using a JSON format.  Every player must answer with the JSON format
{
A0:?
A1:?
A2:?
B0:?
B1:?
B2:?
C0:?
C1:?
C2:?
}

The termination must include the JSON format with all the player's count.

An example of team member's answer:
I'm A2, a member of Team A.  I have 2 chocolates.
{
A0:3
A1:5
A2:2
B0:?
B1:?
B2:?
C0:?
C1:?
C2:?
}
NEXT: A0

An example of team leader's answer:
I'm B0, a leader of Team B.  I have 1 chocolate.
{
A0:3
A1:5
A2:2
B0:1
B1:?
B2:?
C0:?
C1:?
C2:?
}
NEXT: B1
", expectedAnswer);
        }

        public static (string GameName, string GamePrompt, string CheckAnswerPrompt) OddEvenV1(Dictionary<string, int> secretValues)
        {
            var expectedAnswer = BuildCheckAnswerPrompt(BuildOddEvenAnswer(secretValues));

            return ($"{RootGameName}/OddEvenV1", @"The task is determining who has the odd or even chocolate count among the nine players. After completing the task, each team lead must call on another team.
The list of odd and even is tracked in JSON format, and every player must answer in JSON format.
{
odd:[]
even:[]
}

The termination must include the JSON format with all players in the odd and even lists.

When answering, state whether your count is odd or even. 
When answering, must add your name to the odd or even list.
All players must add their names to the odd or even list.
", expectedAnswer);
        }


        private static string BuildCheckAnswerPrompt(string expectedAnswer)
        {
            return $@"Read the conversation.  Classify if it is correct or not correct.  Given the following correct answers 
{expectedAnswer}

Task:
  1. Classify the conversation: 
    * Not Correct: Classify the conversation as not correct if it does not contain the correct answers.
    * Not Correct: Classify the conversation as not correct if any answer is '?'.
    * Correct: Classify the conversation as correct if it contains all the correct answers for all players.  It must contain the full answer.  Verify that their answers match the correct ones
";
        }
        private static string BuildTallyAnswer(Dictionary<string, int> answers)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var pair in answers)
            {
                sb.AppendLine($"{pair.Key}: {pair.Value}");
            }

            // Outer loop for prefixes 'A', 'B', 'C'
            foreach (var prefix in new[] { "A", "B", "C" })
            {
                int teamTotal = 0;
                // Add 3 nodes with each prefix to the graph using a loop
                for (int i = 0; i < 3; i++)
                {
                    string nodeId = $"{prefix}{i}";
                    teamTotal += answers[nodeId];
                }

                sb.AppendLine($"Team {prefix} total: {teamTotal}");
            }

            return sb.ToString();
        }

        private static string BuildReportAnswer(Dictionary<string, int> answers)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var pair in answers)
            {
                sb.AppendLine($"{pair.Key}: {pair.Value}");
            }

            return sb.ToString();
        }

        private static string BuildOddEvenAnswer(Dictionary<string, int> answers)
        {
            var sb = new System.Text.StringBuilder();
            List<string> odd = [];
            List<string> even = [];
            foreach (var pair in answers)
            {
                if (pair.Value % 2 == 0)
                {
                    even.Add(pair.Key);
                }
                else
                {
                    odd.Add(pair.Key);
                }

            }

            sb.AppendLine($"odd:[{string.Join(",", odd)}]");
            sb.AppendLine($"even:[{string.Join(",", even)}]");

            return sb.ToString();
        }
    }
}
