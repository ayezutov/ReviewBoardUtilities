using System.Diagnostics;

namespace ReviewBoardTfsAutoMerger.Log
{
    class EventLogBasedLogger : ILog
    {
        private readonly EventLog log;

        public EventLogBasedLogger(EventLog log)
        {
            this.log = log;
        }

        public void Info(string message)
        {
            log.WriteEntry(message, EventLogEntryType.Information);
        }

        public void Warning(string message)
        {
            log.WriteEntry(message, EventLogEntryType.Warning);
        }

        public void Error(string message)
        {
            log.WriteEntry(message, EventLogEntryType.Error);
        }
    }
}