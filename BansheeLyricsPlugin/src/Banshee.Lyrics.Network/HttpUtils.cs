
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Banshee.Lyrics.Network
{
    public class HttpUtils
    {
        public static string ReadHtmlContent (String url)
        {
            string html = null;
              try
            {
                html = GetHtml (url);
            } catch (Exception e)
            {
                Hyena.Log.DebugFormat ("{0}, {1}", e.Message, url);
                return null;
            }
            return html;
        }
        
        private static string GetHtml (string url)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (url);
            request.Timeout = 6000;
            if (ProxyManager.Instance.isHttpProxy ()) {
                request.Proxy = ProxyManager.Instance.getProxy (url);
            }
            
            HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
            if (response.ContentLength == 0) {
                return null;
            }
            
            StreamReader reader;
            if (response.ContentType.Contains ("utf")) {
                reader = new StreamReader (response.GetResponseStream ());
            } else {
                reader = new StreamReader (response.GetResponseStream (), Encoding.GetEncoding ("iso-8859-1"));
            }
            
            //read all bytes from the stream
            string source = reader.ReadToEnd ();
            reader.Close ();
            return source;
        }
    }
}