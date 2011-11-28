//
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Banshee.Lyrics.Network
{
    public class HttpUtils
    {
        public static string ReadHtmlContent (String url, Encoding encoding)
        {
            string html = null;
            try {
                html = GetHtml (url, encoding);
            } catch {
            }
            return html;
        }

        private static string GetHtml (string url, Encoding encoding)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create (url);
            request.Timeout = 6000;

            using (var response = (HttpWebResponse)request.GetResponse ()) {
                if (response.ContentLength == 0) {
                    return null;
                }

                if (encoding == null) {
                    if (response.ContentType.Contains ("utf")) {
                        encoding = Encoding.UTF8;
                    } else {
                        encoding = Encoding.GetEncoding ("iso-8859-1");
                    }
                }

                using (var reader = new StreamReader (response.GetResponseStream (), encoding)) {
                    // Read all bytes from the stream
                    return reader.ReadToEnd ();
                }
            }
        }
    }
}