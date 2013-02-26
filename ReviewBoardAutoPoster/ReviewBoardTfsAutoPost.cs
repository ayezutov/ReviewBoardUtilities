using System;
using System.Diagnostics;
using System.Security;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using ReviewBoardTfsAutoMerger.Log;

namespace ReviewBoardTfsAutoMerger
{
    partial class ReviewBoardTfsAutoPost : ServiceBase
    {
        public ReviewBoardTfsAutoPost()
        {
            InitializeComponent();
            //Debugger.Launch();
            try
            {
                if (!EventLog.SourceExists("ReviewBoard.TFS.AutoPoster.Source"))
                {
                    EventLog.CreateEventSource(
                        "ReviewBoard.TFS.AutoPoster.Source", "ReviewBoard.TFS.AutoPoster");
                }
            }
            catch (SecurityException)
            {}
            
            eventLog.Source = "ReviewBoard.TFS.AutoPoster.Source";
            eventLog.Log = "ReviewBoard.TFS.AutoPoster";
        }

        private Thread thread;
        protected override void OnStart(string[] args)
        {
            //Debugger.Launch();
            thread = new Thread(() =>
                                    {
                                        var log = new EventLogBasedLogger(eventLog);
                                        try
                                        {
                                            Program.Run(log);
                                        }
                                        catch(Exception ex)
                                        {
                                            log.Error(ex.ToString());
                                            throw;
                                        }
                                    });
            thread.Start();
        }

        protected override void OnStop()
        {
            thread.Abort();
        }
    }
}
