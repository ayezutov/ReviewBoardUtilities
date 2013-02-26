using System;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ReviewBoardTfsAutoMerger.Configuration
{
    [DataContract]
    public class Configuration
    {
        private static readonly Regex referenceToPrevious = new Regex(@"\s*\[RootChangeset\#(?<id>\d+)\][\s\:]*");

        private const string lastProcessedKey = "tfsServer.lastProcessed";

        public Regex ReferenceToPrevious
        {
            get { return referenceToPrevious; }
            private set { }
        }

        [DataMember]
        public string ReviewBoardServer
        {
            get { return ConfigurationManager.AppSettings["reviewBoard.url"]; }
            private set { }
        }

        [DataMember]
        public string ReviewBoardUserName
        {
            get { return ConfigurationManager.AppSettings["reviewBoard.user"]; }
            private set { }
        }

        [DataMember]
        public string ReviewBoardPassword
        {
            get { return ConfigurationManager.AppSettings["reviewBoard.password"]; }
            private set { }
        }

        public Uri ServerUri
        {
            get { return new Uri(ConfigurationManager.AppSettings["tfsServer.url"]); }
        }

        public string FolderPath
        {
            get { return ConfigurationManager.AppSettings["tfsServer.pathToMonitor"]; }
        }

        public string SvnServer
        {
            get { return ConfigurationManager.AppSettings["svnServer.url"]; }
        }

        public CodeReviewersReviewerInfoCollection CodeReviewer
        {
            get { return ((CodeReviewersConfigurationSection)ConfigurationManager.GetSection("codeReview")).Settings; }
        }

        public int LastProcessedChangesetId
        {
            get { return int.Parse(ConfigurationManager.AppSettings[lastProcessedKey]); }
            set
            {
                System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings[lastProcessedKey].Value = value.ToString(CultureInfo.InvariantCulture);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public int Timeout
        {
            get
            {
                int timeout;

                if (!int.TryParse(ConfigurationManager.AppSettings["timeout"], out timeout))
                {
                    timeout = 60000;
                }

                return timeout;
            }
        }

        public string UpdateSummaryExpression
        {
            get { return "{0}, C{1}: {2} and {3}"; }
        }

        public readonly Regex ParsingPreviousSummaryExpression = new Regex(@"(?<revisions>(C\d+[\s,]*)+)\s*:\s*(?<summary>.+)");

        public string Domain
        {
            get { return ConfigurationManager.AppSettings["ldap.domain"] ?? UserPrincipal.Current.Context.Name; }
        }

        public string NotExistentUserName
        {
            get { return ConfigurationManager.AppSettings["not.existing.user"] ?? "not-existing"; }
        }

        public int WebSiteHost { get { return int.Parse(ConfigurationManager.AppSettings["website.port"] ?? "0"); } }
    }
}