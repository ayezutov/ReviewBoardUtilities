using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml;

namespace ReviewBoardTfsAutoMerger.Api
{
    public class ReviewboardApi: BaseApi
    {
        public ReviewboardApi(Uri siteUri, NetworkCredential credentials) : base(siteUri, credentials)
        {
        }

        public IEnumerable<string> GetUsers()
        {
            var xmlDoc = GetXml(new Uri(siteUri + "api/users/"), "GET");

            XmlNodeList userNameNodes = xmlDoc.SelectNodes("//users/array/item/username");
            return userNameNodes != null ? userNameNodes.OfType<XmlNode>().Select(n => n.InnerText) : new string[0];
        }

        public void UpdateChangesetNumber
            (int reviewId, int changeSetNumber)
        {
            try
            {
                GetXml(new Uri(string.Format("{0}api/review-requests/{1}/draft/", siteUri, reviewId)), "PUT",
                   new Dictionary<string, string>
                       {
                           {"changenum", changeSetNumber.ToString(CultureInfo.InvariantCulture)},
                           {"public", "true"}
                       }, false);
            }
            catch (WebException ex)
            {
                // exception intentionally swallowed to overcome a defect in review board
            }
            
        }

        public ReviewRequest GetReviewRequestByChangeset(int changestNumber)
        {
            var xmlDoc = GetXml(new Uri(string.Format("{0}/api/review-requests/?changenum={1}", siteUri,changestNumber)), "GET");

            var itemNode = xmlDoc.SelectSingleNode("//review_requests/array/item");

            if (itemNode == null)
                return null;

            return ReadReviewRequest(itemNode);
        }

        private static ReviewRequest ReadReviewRequest(XmlNode itemNode)
        {
            string changeNum = GetNodeText(itemNode, "changenum");
            return new ReviewRequest
                       {
                           Id = int.Parse(GetNodeText(itemNode, "id")),
                           ChangeNum = string.IsNullOrEmpty(changeNum) 
                               ? (int?) null 
                               : int.Parse(changeNum),
                           Description = GetNodeText(itemNode, "description"),
                           Summary = GetNodeText(itemNode, "summary"),
                           Submitter = GetNodeText(itemNode, "links/submitter/title")
                       };
        }

        private static string GetNodeText(XmlNode itemNode, string xpath)
        {
            XmlNode value = itemNode.SelectSingleNode(xpath);
            return value == null ? null : value.InnerText;
        }

        public List<ReviewRequest> GetReviewRequests()
        {
            var xmlDoc = GetXml(new Uri(string.Format("{0}/api/review-requests/?status=pending&max-results=100000", siteUri)), "GET");

            var items = xmlDoc.SelectNodes("//review_requests/array/item");

            var results = new List<ReviewRequest>();

            if (items == null)
                return results;

            results.AddRange(items.Cast<XmlNode>().Select(ReadReviewRequest));
            results.Sort((r1, r2) => r1.Id - r2.Id);
            return results;
        }

        public ReviewRequest GetReviewRequestById(int baseId)
        {
            var xmlDoc = GetXml(new Uri(string.Format("{0}/api/review-requests/{1}/", siteUri, baseId)), "GET");

            var itemNode = xmlDoc.SelectSingleNode("//review_request");

            if (itemNode == null)
                return null;

            return ReadReviewRequest(itemNode);
        }

        public List<string> GetAllDiffs(int reviewRequestId)
        {
            var node = GetXml(new Uri(string.Format("{0}/api/review-requests/{1}/diffs/", siteUri, reviewRequestId)),
                              "GET");

            var items = node.SelectNodes("//diffs/array/item");

            if (items == null)
                return new List<string>();

            var revisions = items.Cast<XmlNode>().Select(n => GetNodeText(n, "revision"));

            return revisions.Select(r => GetDiffText(reviewRequestId, r)).ToList();
        }

        private string GetDiffText(int reviewRequestId, string revision)
        {
            return GetValue(new Uri(string.Format("{0}/r/{1}/diff/{2}/raw/", siteUri, reviewRequestId, revision)),
                                "GET");
        }

        public void UploadDiff(int reviewRequestId, string diff, string baseDir = "/")
        {
            GetXml(new Uri(string.Format("{0}/api/review-requests/{1}/diffs/", siteUri, reviewRequestId)),
                              "POST", new Dictionary<string, string>
                                          {
                                             {"path/somefile.txt", diff},
                                             {"basedir", baseDir}
                                         });
        }

        public void PublishDraft(int reviewRequestId)
        {
            GetXml(new Uri(string.Format("{0}api/review-requests/{1}/draft/", siteUri, reviewRequestId)), "PUT",
                   new Dictionary<string, string>
                       {
                           {"public", "true"}
                       }, false);
        }

        public void UpdateReviewRequest(int reviewRequestId, Dictionary<string, string> dictionary)
        {
            GetXml(new Uri(string.Format("{0}api/review-requests/{1}/draft/", siteUri, reviewRequestId)), "PUT",
                   dictionary, false);
        }

        public void UpdateReviewRequestStatus(int id, string status)
        {
            GetXml(new Uri(string.Format("{0}api/review-requests/{1}/", siteUri, id)), "PUT",
                   new Dictionary<string, string>
                       {
                           {"status", status}
                       }, false);
        }
    }

    public class ReviewRequest
    {
        public int Id { get; set; }

        public int? ChangeNum { get; set; }

        public string Summary { get; set;}

        public string Description { get; set; }

        public string Submitter { get; set; }
    }
}
