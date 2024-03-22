using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.AI.Details.Common.CLI.details.console
{
    public struct ConsoleTempWriter : IDisposable
    {
        private int _tempCount;

        public void WriteTemp(string message) => Overwrite(message, Console.Write);

        public void AppendTemp(string message)
        {
            Console.Write(message);
            _tempCount += message.Length;
        }

        public void WriteErrorTemp(string message) => Overwrite(message, ConsoleHelpers.WriteError);

        public void Clear() => Overwrite(string.Empty, Console.Write);

        public void Dispose()
        {
            Overwrite(string.Empty, Console.Write);
        }

        private void Overwrite(string message, Action<string> writer)
        {
            try
            {
                // 1. Go back to the start of temp string (does not delete anything just moves the caret so to speak)
                Console.WriteLine(new string('\b', _tempCount));

                // 2. Write out new message
                writer(message);

                // 3. If the new message is shorter than the last, overwrite remaining old chars with space and then go back to
                //    end of new message
                int delta = _tempCount - message.Length;
                if (delta > 0)
                {
                    Console.Write(new string(' ', delta));
                    Console.Write(new string('\b', delta));
                }
            }
            finally
            {
                _tempCount = message?.Length ?? 0;
            }
        }
    }
}
