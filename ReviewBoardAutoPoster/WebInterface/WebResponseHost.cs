using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using ReviewBoardTfsAutoMerger.Api;
using System.Linq;

namespace ReviewBoardTfsAutoMerger.WebInterface
{
    /// <summary>
    /// The service, which allows a web-based status tracking of the server
    /// </summary>
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class WebSiteService : IWebSiteService
    {
        private readonly Configuration.Configuration config = new Configuration.Configuration();
        private readonly ReviewboardApi api;

        private static readonly IDictionary<string, string> allowed =
            new Dictionary<string, string>
                {
                    {".html", "text/html"},
                    {".htm", "text/html"},
                    {".js", "text/javascript"},
                    {".css", "text/css"},
                    {".png", "image/png"},
                    {".gif", "image/gif"},
                    {".jpg", "image/jpeg"}
                };

        /// <summary>
        /// Initializes a new instance of dashboard service
        /// </summary>
        public WebSiteService()
        {
            api = new ReviewboardApi(new Uri(config.ReviewBoardServer), new NetworkCredential(config.ReviewBoardUserName, config.ReviewBoardPassword));
        }

        /// <summary>
        /// Gets some file from server
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Stream Get(string fileName)
        {
            Debug.Assert(WebOperationContext.Current != null);

            string pathToWebSite = Debugger.IsAttached
                ? "../../WebSite"
                : "WebSite";
            var physicalPathToFile =
                Path.Combine(
                    Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetEntryAssembly().CodeBase).AbsolutePath),
                        pathToWebSite), fileName);

            var response = WebOperationContext.Current.OutgoingResponse;

            if (!File.Exists(physicalPathToFile))
            {
                response.SetStatusAsNotFound();
                return new MemoryStream(Encoding.UTF8.GetBytes("Not found"));
            }

            var extension = Path.GetExtension(physicalPathToFile);
            if (!IsAllowedExtension(extension) || extension == null)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                return new MemoryStream(Encoding.UTF8.GetBytes("Forbidden"));
            }

            response.ContentType = allowed[extension];
            var stream = new FileStream(physicalPathToFile, FileMode.Open, FileAccess.Read);

            //OperationContext.Current.OperationCompleted += (sender, args) => stream.Dispose();

            return stream;
        }

        public List<ReviewRequest> GetReviewRequests()
        {
            return api.GetReviewRequests();
        }

        public List<ReviewRequest> MergeReviews(MergeModel model)
        {
            var baseRequest = api.GetReviewRequestById(model.BaseId);

            var orderedEnumerable = model.SubsequentIds.OrderBy(x => x).ToList();
            foreach (var subsequentId in orderedEnumerable)
            {
                var nextRequest = api.GetReviewRequestById(subsequentId);

                var diffs = api.GetAllDiffs(nextRequest.Id);

                foreach (var diff in diffs)
                {
                    api.UploadDiff(baseRequest.Id, diff);
                }

                var newSummary = MergeSummaries(baseRequest, nextRequest);
                api.UpdateReviewRequest(baseRequest.Id, new Dictionary<string, string>
                                        {
                                            {"summary", newSummary},
                                            {"description", string.Format("{0}\r\n{1}", baseRequest.Description, nextRequest.Description)}
                                        });

                api.PublishDraft(baseRequest.Id);

                api.UpdateReviewRequestStatus(nextRequest.Id, "discarded");

                api.PublishDraft(nextRequest.Id);
            }
            

            return GetReviewRequests();
        }

        private string MergeSummaries(ReviewRequest baseRequest, ReviewRequest nextRequest)
        {
            var baseMatch = config.ParsingPreviousSummaryExpression.Match(baseRequest.Summary);
            var nextMatch = config.ParsingPreviousSummaryExpression.Match(nextRequest.Summary);

            return string.Format("{0}, {1}: {2} and {3}", baseMatch.Groups["revisions"], nextMatch.Groups["revisions"],
                                 baseMatch.Groups["summary"], nextMatch.Groups["summary"]);
        }

        private static bool IsAllowedExtension(string extension)
        {
            return allowed.Keys.Contains(extension);
        }
    }




    /// <summary>
    /// The contract for communicating with dashboard information of the server
    /// It is web-enabled, so is mostly designed to be accessed through browser
    /// </summary>
    [ServiceContract(Namespace = "http://yezutov.com/reviewboardutilities/website")]
    public interface IWebSiteService
    {
        /// <summary>
        /// Gets some file from server
        /// </summary>
        /// <param name="fileName">The name of the file to return</param>
        /// <returns></returns>
        [OperationContract, WebGet(UriTemplate = "get/{*fileName}")]
        Stream Get(string fileName);

        [OperationContract, WebGet(UriTemplate = "getReviewRequests")]
        List<ReviewRequest> GetReviewRequests();

        [OperationContract, WebInvoke(UriTemplate = "mergeReviews", Method = "POST", RequestFormat = WebMessageFormat.Json)]
        List<ReviewRequest> MergeReviews(MergeModel model);
    }

    public class MergeModel
    {
        public int BaseId { get; set; }
        public int[] SubsequentIds { get; set; }
    }

}