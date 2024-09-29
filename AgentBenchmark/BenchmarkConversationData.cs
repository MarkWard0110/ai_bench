using BAIsic.Interlocutor;
using BAIsic.LlmApi.Ollama;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public record BenchmarkConversationData(string BenchmarkName, string BenchmarkResult, string Model, RequestOptions RequestOptions, ImmutableList<BenchmarkConversationResult> BenchmarkConversations);
    public record BenchmarkConversationResult(int TurnCount, ImmutableList<BenchmarkConversationHistory> Conversation);
    public record BenchmarkConversationHistory(string AgentName, ImmutableList<BAIsic.Interlocutor.Message> Messages);
}
