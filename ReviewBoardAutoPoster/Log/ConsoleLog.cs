using System;

namespace ReviewBoardTfsAutoMerger.Log
{
    class ConsoleLog : ILog
    {
        public void Info(string message)
        {
            Console.WriteLine("Info   : {0}", message);
        }

        public void Warning(string message)
        {
            Console.WriteLine("Warning: {0}", message);
        }

        public void Error(string message)
        {
            Console.WriteLine("Error  : {0}", message);
        }
    }
}