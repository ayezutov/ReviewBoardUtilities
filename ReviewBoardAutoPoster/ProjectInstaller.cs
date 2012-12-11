using System.ComponentModel;
using System.Diagnostics;


namespace ReviewBoardTfsAutoMerger
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            if (!EventLog.SourceExists("ReviewBoard.TFS.AutoPoster.Source"))
            {
                EventLog.CreateEventSource("ReviewBoard.TFS.AutoPoster.Source", 
                    "ReviewBoard.TFS.AutoPoster");
            }
        }
    }
}
