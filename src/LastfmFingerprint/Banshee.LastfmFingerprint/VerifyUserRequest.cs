//
// VerifyUserRequest.cs
//
// Authors:
//   Olivier Dufour   <olivier(dot)duff(at)gmail(dot)com>
//
// Copyright (C) 2009 Bertrand Lorentz
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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Security.Cryptography;

using Hyena;
using Hyena.Json;
using Banshee.Collection;

namespace Lastfm
{
    public class VerifyUserRequest
    {
        private const string API_ROOT = "http://ws.audioscrobbler.com/ass/pwcheck.php";
        private int error_code;
        private Stream response_stream;

        public VerifyUserRequest ()
        {}

        public bool Send (string user, string pwd)
        {
            response_stream = Get (API_ROOT, BuildPostData (user, pwd));
            if (response_stream == null) {
                error_code = -4;
                return false;
            }
            StreamReader reader = new StreamReader(response_stream);
            string response = reader.ReadToEnd ();

            if (response.Contains( "OK2" ))
                error_code = 0;
            else if (response.Contains( "OK" ))
                error_code = 0;
            else if (response.Contains( "INVALIDUSER" ))
                error_code = -1;
            else if (response.Contains( "BADPASSWORD" ))
                error_code = -2;
            else
                error_code = -3;
                
            return (error_code == 0); 
        }

        public string GetErrorString()
        {
            if (response_stream == null)
                return string.Empty;

            switch (error_code) {
                case -1:
                    return "Bad user name, please check your login.";
                case -2:
                    return "Bad password, please check your password.";
                case -3:
                    return "Error authentification.";
                case -4:
                    return "Webservice timeout.";
                default:
                    return String.Empty;
            }
        }

        private string BuildPostData (string user, string pwd)
        {
            string time = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds.ToString ();

            StringBuilder data = new StringBuilder ();
            data.AppendFormat ("time={0}", time);//seconds since 1er janvier 1970
            data.AppendFormat ("&username={0}", user);
            data.AppendFormat ("&auth={0}", ConvertToMd5(pwd + time));
            data.AppendFormat ("&auth2={0}", ConvertToMd5(pwd.ToLower() + time));
            data.Append ("&defaultplayer=");
            
            return data.ToString ();
        }

        public static string ConvertToMd5 (string input)
        {
            MD5 md5Hasher = MD5.Create ();
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();
    
            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // TODO check if need to fill the end of sbuilder until get a size of 32

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        #region HTTP helpers

        private Stream Get (string uri, string urlParams)
        {
            Log.DebugFormat ("send get last.fm verify user.");
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (String.Concat (uri, "?", urlParams));
            request.UserAgent = LastfmCore.UserAgent;
            request.Timeout = 10000;
            request.Method = "GET";

            HttpWebResponse response = null;
            try {
                response = (HttpWebResponse) request.GetResponse ();
            } catch (WebException e) {
                Log.DebugException (e);
                response = (HttpWebResponse)e.Response;
            }
            Log.DebugFormat ("get response stream.");
            return response != null ? response.GetResponseStream () : null;
        }

#endregion
    }
}
