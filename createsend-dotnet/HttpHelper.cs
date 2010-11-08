﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Net;

namespace createsend_dotnet
{
    internal static class CreateSendOptions
    {
        static string api_key;
        static string base_uri;

        static CreateSendOptions()
        {
            api_key = string.IsNullOrEmpty(ConfigurationManager.AppSettings["api_key"]) ? api_key : ConfigurationManager.AppSettings["api_key"];
            base_uri = string.IsNullOrEmpty(ConfigurationManager.AppSettings["base_uri"]) ? "http://api.createsend.com/api/v3" : ConfigurationManager.AppSettings["base_uri"];
        }

        public static string ApiKey
        {
            get
            {
                return api_key;
            }
        }

        public static string BaseUri
        {
            get
            {
                return base_uri;
            }
        }

        public static string VersionNumber
        {
            get
            {
                return "0.0.1";
            }
        }

    }

    internal class HttpHelper
    //public class HttpHelper
    {
        private static NetworkCredential authCredentials = new NetworkCredential(CreateSendOptions.ApiKey, "x");

        public static string Get(string path, string query)
        {
            return MakeRequest("GET", CreateSendOptions.BaseUri + path + query, null);
        }

        public static string Post(string path, string query, string payload)
        {
            return MakeRequest("POST", CreateSendOptions.BaseUri + path + query, payload);
        }

        public static string Put(string path, string query, string payload)
        {
            return MakeRequest("PUT", CreateSendOptions.BaseUri + path + query, payload);
        }

        public static string Delete(string path, string query)
        {
            return MakeRequest("DELETE", CreateSendOptions.BaseUri + path + query, null);
        }

        static string MakeRequest(string method, string uri, string payload)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            req.Method = method;
            req.ContentType = "application/xml";
            req.UserAgent = "createsend-dotnet-#" + CreateSendOptions.VersionNumber;

            req.Credentials = authCredentials;

            if (method != "GET" && !string.IsNullOrEmpty(payload))
            {
                using (System.IO.StreamWriter os = new System.IO.StreamWriter(req.GetRequestStream()))
                {
                    os.Write(payload);
                    os.Close();
                }
            }

            try
            {
                using (System.Net.HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    if (resp == null)
                        return "";
                    else
                    {
                        System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                        return sr.ReadToEnd().Trim();
                    }
                }
            }
            catch (WebException we)
            {
                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    switch ((int)((HttpWebResponse)we.Response).StatusCode)
                    {
                        case 400:
                            throw ThrowReworkedCustomException(we);
                        case 401:
                        case 404:
                        default:
                            throw we;
                    }
                }
                else
                {
                    throw we;
                }
            }
        }

        private static Exception ThrowReworkedCustomException(WebException we)
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(((HttpWebResponse)we.Response).GetResponseStream());
            ErrorResult apiExceptionResult = XMLSerializer.Deserialize<ErrorResult>(sr.ReadToEnd().Trim());

            return new Exception(string.Format("The CreateSend API responded with the following error - {0}: {1}", apiExceptionResult.Code, apiExceptionResult.Message));
        }

        public static void OverrideAuthenticationCredentials(string username, string password)
        {
            authCredentials = new NetworkCredential(username, password);
        }
    }
}
