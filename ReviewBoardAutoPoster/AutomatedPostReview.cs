using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Text;
using ReviewBoardTfsAutoMerger.Api;
using ReviewBoardTfsAutoMerger.Configuration;
using ReviewBoardTfsAutoMerger.LDAP;
using ReviewBoardTfsAutoMerger.Log;

namespace ReviewBoardTfsAutoMerger
{
    public class AutomatedPostReview
    {
        private readonly ILog log;
        private IEnumerable<string> users;
        private readonly Configuration.Configuration config;
        readonly VersionControlServer vcs;
        private readonly ReviewboardApi api;

        public AutomatedPostReview(ILog log, Configuration.Configuration config)
        {
            this.log = log;
            this.config = config;
            api = new ReviewboardApi(new Uri(config.ReviewBoardServer),
                                         new NetworkCredential(config.ReviewBoardUserName, config.ReviewBoardPassword));
            var teamProjectCollection = new TfsTeamProjectCollection(config.ServerUri);
            vcs = teamProjectCollection.GetService<VersionControlServer>();
        }

        public void Run()
        {
            try
            {
                users = api.GetUsers();
                
                int previousChangesetId = config.LastProcessedChangesetId;

                var history = vcs.QueryHistory(config.FolderPath, VersionSpec.Latest, 0,
                                               RecursionType.Full, "",
                                               new ChangesetVersionSpec(previousChangesetId), VersionSpec.Latest,
                                               10, true, false, true, true);

                var changesets = history.OfType<Changeset>().Where(c => c.ChangesetId > previousChangesetId).ToArray();
                log.Info(string.Format("Found {0} changesets", changesets.Length));

                foreach (var changeset in changesets)
                {
                    if (!ProcessChangeset(changeset)) break;
                }
            }
            catch (WebException ex)
            {
                log.Warning(string.Format("{0}\r\n{1}", ex, ex.Response));
            }
        }

        private bool ProcessChangeset(Changeset changeset)
        {
            var result = RunPostReview(changeset);

            if (result.IsSuccess)
            {
                if (result.ReviewId > 0)
                {
                    log.Info(string.Format("{0} review #{1}", result.IsNewRequest ? "Created" : "Updated", result.ReviewId));
                    if (result.IsNewRequest)
                    {
                        api.UpdateChangesetNumber(result.ReviewId, changeset.ChangesetId);
                    }
                    config.LastProcessedChangesetId = changeset.ChangesetId;
                }
                else
                {
                    log.Warning("There was no error while executing post-review, however review id was not found.");
                }
            }
            else
            {
                log.Error("There was an error while creating a review");
                return false;
            }
            return true;
        }

        private PostReviewResult RunPostReview(Changeset changeset)
        {
            var domainContext = new PrincipalContext(ContextType.Domain, config.Domain);
            var commiter = UserPrincipal.FindByIdentity(domainContext, changeset.Committer);
            log.Info(string.Format("Running post-review for {0}-{1}: {2}", changeset.ChangesetId, changeset.Committer,
                              changeset.Comment));
            
            var commiterName = commiter == null
                                   ? changeset.Committer
                                   : commiter.SamAccountName;

            var commiterSecurityGroups = new List<Principal>();
                if (commiter != null)
                {
                    commiterSecurityGroups = MembershipDetector.GetAllUserGroups(commiter);
                }
            
            log.Info(string.Format("Security groups for {0}: {1}", commiter, string.Join(", ", commiterSecurityGroups)));

            ReviewRequest reviewRequest = GetRootReviewRequest(changeset);

            var reviewConfig =
                config.CodeReviewer.CodeReviewersInfo.FirstOrDefault(cr => string.Equals(commiterName, cr.Name, StringComparison.InvariantCulture))
                ?? config.CodeReviewer.CodeReviewersInfo.FirstOrDefault(cr => commiterSecurityGroups.Any(sg => string.Equals(sg.Name, cr.SecurityGroup, StringComparison.InvariantCulture)))
                    ?? (config.CodeReviewer.CodeReviewersInfo.FirstOrDefault(cr => string.Equals(cr.Name, "Default")))
                        ?? new CodeReviewersReviewerInfo
                               {
                                   Groups = new List<string>(),
                                   Reviewers = new List<string>()
                               };

            var sb = new StringBuilder();

            if (reviewRequest != null)
            {
                FormParametersForExistingRequest(reviewRequest, changeset, commiterName, sb);
            }
            else
            {
                FormParametersForNewRequest(changeset, reviewConfig, commiterName, sb);
            }

            return RunPostReviewProcess(reviewRequest, sb.ToString());
        }

        private PostReviewResult RunPostReviewProcess(ReviewRequest reviewRequest, string arguments)
        {
            var process = new Process
                              {
                                  StartInfo = new ProcessStartInfo
                                                  {
                                                      FileName = "post-review",
                                                      Arguments = arguments,
                                                      CreateNoWindow = true,
                                                      RedirectStandardOutput = true,
                                                      RedirectStandardError = true,
                                                      UseShellExecute = false
                                                  }
                              };
            log.Info(process.StartInfo.Arguments);
            process.Start();
            int result = 0;


            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.EnableRaisingEvents = true;

            DataReceivedEventHandler dataReceived = (sender, e) =>
                                                        {
                                                            var regexString = Regex.Escape(config.ReviewBoardServer + "/r/") +
                                                                              "(?<reviewId>\\d+)/";
                                                            var regex = new Regex(regexString);

                                                            var line = e.Data;
                                                            log.Info(line);
                                                            var match = regex.Match(line ?? string.Empty);
                                                            if (match.Success)
                                                            {
                                                                log.Info("Found match for reviewId");
                                                                result = int.Parse(match.Groups["reviewId"].Value);
                                                            }
                                                        };

            process.OutputDataReceived += dataReceived;
            process.ErrorDataReceived += dataReceived;


            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                log.Error(string.Format("post-review existed with code '{0}'", process.ExitCode));
                return PostReviewResult.Error;
            }
            return new PostReviewResult
                       {
                           ReviewId = result,
                           IsNewRequest = reviewRequest == null,
                           IsSuccess = true
                       };
        }

        private ReviewRequest GetRootReviewRequest(Changeset changeset, List<int> visited = null)
        {
            var match = config.ReferenceToPrevious.Match(changeset.Comment);
            if (!match.Success)
                return null;

            var rootChangesetId = int.Parse(match.Groups["id"].Value);
            ReviewRequest requestByChangeset = api.GetReviewRequestByChangeset(rootChangesetId);

            if (requestByChangeset != null)
            {
                return requestByChangeset;
            }

            if (visited != null && visited.Contains(rootChangesetId))
                return null;

            (visited ?? (visited = new List<int>())).Add(changeset.ChangesetId);
            
            var parentChangeset = vcs.GetChangeset(rootChangesetId);
            return GetRootReviewRequest(parentChangeset, visited);
        }

        private void FormParametersForNewRequest(Changeset changeset, CodeReviewersReviewerInfo reviewConfig, string commiterName, StringBuilder sb)
        {
            FormCommonAuthenticationAndRepositoryParameters(changeset, commiterName, sb);

            sb.AppendFormat(@" --summary=""C{0}: {1}""", changeset.ChangesetId,
                                              changeset.Comment.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).
                                                  FirstOrDefault());
            sb.AppendFormat(" --description=\"Review for chageset {0} by {1}:\r\n{2}\"", changeset.ChangesetId,
                                              changeset.Committer, changeset.Comment);

            if (reviewConfig.Groups.Any())
            {
                sb.AppendFormat(@" --target-groups=""{0}""", string.Join(",", reviewConfig.Groups));
            }

            if (reviewConfig.Reviewers.Any())
            {
                sb.AppendFormat(@" --target-people=""{0}""", string.Join(",", reviewConfig.Reviewers));
            }

            sb.AppendFormat(@" --publish");
        }

        private void FormCommonAuthenticationAndRepositoryParameters(Changeset changeset, string commiterName, StringBuilder sb)
        {
            sb.AppendFormat(@" --repository-url=""{0}""", config.SvnServer);
            sb.AppendFormat(@" --server=""{0}""", config.ReviewBoardServer);
            sb.AppendFormat(@" --username=""{0}""", config.ReviewBoardUserName);
            sb.AppendFormat(@" --password=""{0}""", config.ReviewBoardPassword);
            sb.AppendFormat(@" --submit-as=""{0}""",
                            users.Any(u => u.Equals(commiterName)) ? commiterName : config.NotExistentUserName);
            sb.AppendFormat(@" --revision-range=""{0}:{1}""", changeset.ChangesetId - 1, changeset.ChangesetId);
        }

        private void FormParametersForExistingRequest(ReviewRequest reviewRequest, Changeset changeset, string commiterName, StringBuilder sb)
        {
            sb.AppendFormat(" --review-request-id={0}", reviewRequest.Id);

            FormCommonAuthenticationAndRepositoryParameters(changeset, commiterName, sb);


            var summaryMatch = config.ParsingPreviousSummaryExpression.Match(reviewRequest.Summary);
            string comment = config.ReferenceToPrevious.Replace(changeset.Comment.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty, string.Empty);

            string summary = summaryMatch.Success
                                 ? string.Format(config.UpdateSummaryExpression, summaryMatch.Groups["revisions"],
                                                 changeset.ChangesetId,
                                                 summaryMatch.Groups["summary"], 
                                                 comment)
                                 : string.Format("C{0}: {1}", changeset.ChangesetId, comment);

            string description = string.Format("{0}\r\nReview for chageset {1} by {2}:\r\n{3}", 
                reviewRequest.Description, changeset.ChangesetId,
                                              changeset.Committer, changeset.Comment);


            sb.AppendFormat(@" --summary=""{0}""", summary);
            sb.AppendFormat(" --description=\"{0}\"", description);

            sb.AppendFormat(@" --publish");
        }
    }
}