using System.IO;
using System.Reflection;

namespace ReviewBoardTfsAutoMerger.Log
{
    public class FileLog: ILog
    {
        public void Info(string message)
        {
            WriteToFile("Info   : {0}", message);
        }

        public void Warning(string message)
        {
            WriteToFile("Warning: {0}", message);
        }

        public void Error(string message)
        {
            WriteToFile("Error  : {0}", message);
        }

        private void WriteToFile(string prefix, string message)
        {
            File.WriteAllLines(Assembly.GetExecutingAssembly().Location + ".log", new []{string.Format(prefix, message)});
        }
    }
}