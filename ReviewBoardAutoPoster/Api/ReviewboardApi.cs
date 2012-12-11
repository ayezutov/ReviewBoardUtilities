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
                GetXml(new Uri(string.Format("{0}api/review-requests/{1}/", siteUri, reviewId)), "PUT",
                   new Dictionary<string, string>
                       {
                           {"changenum", changeSetNumber.ToString(CultureInfo.InvariantCulture)}
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

            string changeNum = GetNodeText(itemNode, "changenum");
            return new ReviewRequest
                       {
                           Id = int.Parse(GetNodeText(itemNode, "id")),
                           ChangeNum = changeNum == null ? (int?) null : int.Parse(changeNum),
                           Description = GetNodeText(itemNode, "description"),
                           Summary = GetNodeText(itemNode, "summary")
                       };
        }

        private static string GetNodeText(XmlNode itemNode, string xpath)
        {
            XmlNode value = itemNode.SelectSingleNode(xpath);
            return value == null ? null : value.InnerText;
        }
    }

    public class ReviewRequest
    {
        public int Id { get; set; }

        public int? ChangeNum { get; set; }

        public string Summary { get; set;}

        public string Description { get; set; }
    }
}
