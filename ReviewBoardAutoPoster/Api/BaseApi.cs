using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
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
        protected XmlDocument GetXml(Uri uri, string verb, Dictionary<string, string> parameters = null, bool allowAutoRedirect = true)
        {
            var xml = GetValue(uri, verb, parameters, allowAutoRedirect, "application/xml");
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
        
        protected string GetValue(Uri uri, string verb, Dictionary<string, string> parameters = null, bool allowAutoRedirect = true, string accept = "*/*") {
            if (cookies == null)
            {
                PopulateCookie();
            }
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = verb;
            request.Accept = accept;
            request.CookieContainer = cookies;
            request.KeepAlive = false;
            request.AllowAutoRedirect = allowAutoRedirect;
            
            if (parameters != null)
            {
                WriteMultiPartFormData(request, parameters);
            }
            var response = request.GetResponse();
            string value;
            try
            {
                Stream responseStream = response.GetResponseStream();

                if (responseStream == null)
                    return null;

                value = new StreamReader(responseStream).ReadToEnd();
            }
            finally
            {
                response.Close();
            }
            return value;
        }

        private void PopulateCookie()
        {
            var request = (HttpWebRequest)WebRequest.Create(new Uri(siteUri + "api/"));
            string credentialsString = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes( string.Format("{0}:{1}", credentials.UserName, credentials.Password)));
            request.Headers.Add("Authorization", "Basic " + credentialsString);
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
                var name = parameter.Key;
                var fileName = string.Empty;
                if (parameter.Key.Contains("/"))
                {
                    var split = parameter.Key.Split('/');
                    name = split[0];
                    fileName = split[1];
                }
                requestWriter.WriteLine(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}", name, string.IsNullOrEmpty(fileName) ? string.Empty : string.Format("; filename=\"{0}\"", fileName)));
                requestWriter.WriteLine();
                requestWriter.WriteLine(parameter.Value);
            }

            requestWriter.WriteLine(string.Format("--{0}--", boundary));
            requestWriter.Flush();
        }
    }
}
