using BAIsic.Interlocutor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentBenchmark
{
    public class ReceiveNextFilter
    {
        private bool _isReady = false;

        public Task<Message?> FilterNext(Message? message)
        {
            /*
             * We do not want to filter the initial instruction message.
             * This is the first message in agent will receive.  We will 
             * skip this message and return it as is.
             */
            if (!_isReady)
            {
                _isReady = true;
                return Task.FromResult(message);
            }

            if (message == null)
            {
                return Task.FromResult(message);
            }

            string pattern = @"NEXT:\s*\S+";
            string result = Regex.Replace(message.Text, pattern, "", RegexOptions.IgnoreCase);
            var modifiedMessage = message with { Text = result };

            return Task.FromResult<BAIsic.Interlocutor.Message?>(modifiedMessage);
        }
    }
}
