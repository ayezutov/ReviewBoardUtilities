using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;

namespace ReviewBoardTfsAutoMerger.Api
{
    public class BaseApi
    {
        protected readonly Uri siteUri;
        private readonly NetworkCredential credentials;

        public BaseApi(Uri siteUri, NetworkCredential credentials)
        {
            this.siteUri = siteUri;
            this.credentials = credentials;
        }

        private CookieContainer cookies;
        protected XmlDocument GetXml(Uri uri, string verb, Dictionary<string, string> parameters = null, bool allowAutoRedirect = true) {
            if (cookies == null)
            {
                PopulateCookie();
            }
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = verb;
            request.Accept = "application/xml";
            request.CookieContainer = cookies;
            request.KeepAlive = false;
            request.AllowAutoRedirect = allowAutoRedirect;
            
            if (parameters != null)
            {
                WriteMultiPartFormData(request, parameters);
            }
            var response = request.GetResponse();
            string xml;
            try
            {
                Stream responseStream = response.GetResponseStream();

                if (responseStream == null)
                    return null;

                xml = new StreamReader(responseStream).ReadToEnd();
            }
            finally
            {
                response.Close();
            }
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                return xmlDoc;
            }
            catch(XmlException)
            {
                return null;
            }
        }

        private void PopulateCookie()
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(siteUri + "api/users/"));
            request.Credentials = credentials;
            request.PreAuthenticate = true;
            request.KeepAlive = false;
            cookies = new CookieContainer();
            request.CookieContainer = cookies;

            var response = request.GetResponse();
            response.Close();
        }

        private void WriteMultiPartFormData(HttpWebRequest request, Dictionary<string, string> parameters)
        {
            var boundary = Guid.NewGuid().ToString();
            request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

            var requestWriter = new StreamWriter(request.GetRequestStream());
            foreach (var parameter in parameters)
            {
                requestWriter.WriteLine(string.Format("--{0}", boundary));
                requestWriter.WriteLine(string.Format("Content-Disposition: form-data; name=\"{0}\"", parameter.Key));
                requestWriter.WriteLine();
                requestWriter.WriteLine(parameter.Value);
            }

            requestWriter.WriteLine(string.Format("--{0}", boundary));
            requestWriter.Flush();
        }
    }
}
