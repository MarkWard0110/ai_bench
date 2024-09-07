using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public static class AgentBenchmarkConventions
    {
        public static class BenchmarkReasons
        {
            public const string Pass = "pass";
            public const string FailMinimumTurnCount = "fail: minimum turn count";
            public const string FailMaximumTurnCount = "fail: maximum turn count";
            //public const string FailNoTerminate = "fail: no terminate"; // can't happen if max turn count is checked
            public const string FailNotCorrect = "fail: not correct";
            public const string FailNotCorrectBecauseImpersonation = "fail: not correct (impersonation)";

        }
    }
}
