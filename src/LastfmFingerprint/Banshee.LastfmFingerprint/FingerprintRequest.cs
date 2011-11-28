//
// LastfmRequest.cs
//
// Authors:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
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
    public class FingerprintRequest
    {
        private const string API_ROOT = "http://ws.audioscrobbler.com/fingerprint/query/";

        private Stream response_stream;

        public FingerprintRequest ()
        {}

        public void Send (TrackInfo track, byte[] fingerprint, LastfmAccount account)
        {
            response_stream = Post (API_ROOT, BuildPostData (track, account), fingerprint);
        }

        public int GetFpId ()
        {
            if (response_stream == null)
                return -1;

            StringBuilder bld = new StringBuilder ();
            char c;
            while (response_stream.CanRead)
            {
                c = (char)(byte)response_stream.ReadByte ();
                if (c == ' ')
                    break;
                bld.Append (c);
            }
            int ret;
            if (Int32.TryParse (bld.ToString (), out ret)) {
                return ret;
            } else {
                return -2;
            }
        }

        private string EscapeUri (string str)
        {
            //TODO if c > 'z' || c < 'a' ==> %ascii
            return Uri.EscapeUriString (str ?? string.Empty).Replace ("-", "%2D").Replace (".", "%2E").Replace("(", "%28").Replace (")", "%29");
        }

        private string BuildPostData (TrackInfo track, LastfmAccount account)
        {
            StringBuilder data = new StringBuilder ();
            data.AppendFormat ("artist={0}", EscapeUri (track.ArtistName));
            data.AppendFormat ("&duration={0}", (int)track.Duration.TotalSeconds);

            string[] slashedUri = track.Uri.LocalPath.Split('/', '\\');
            data.AppendFormat ("&filename={0}", EscapeUri(slashedUri [slashedUri.Length - 1]));
            data.AppendFormat ("&fpversion={0}", EscapeUri (GetlfmpVersion ()));
            data.AppendFormat ("&genre={0}", EscapeUri (track.Genre));
            data.AppendFormat ("&samplerate={0}", track.SampleRate);

            SHA256Managed cr = new  SHA256Managed ();
            byte[] hash = cr.ComputeHash (File.ReadAllBytes (track.Uri.AbsolutePath));
            data.AppendFormat ("&sha256={0}", ToHex (hash));
            data.AppendFormat ("&track={0}", EscapeUri (track.TrackTitle));

            if (!string.IsNullOrEmpty(track.AlbumTitle))
                data.AppendFormat ("&album={0}", EscapeUri (track.AlbumTitle));

            if (track.TrackNumber > 0)
                data.AppendFormat ("&tracknum={0}", track.TrackNumber);

            if (track.Year > 0)
                data.AppendFormat ("&year={0}", track.Year);
            // TODO change account here
            if (account == null || String.IsNullOrEmpty (account.UserName)) {
                // anonymous seems to stuck in 3 requests
                // TODO add a wait system when anon
                data.AppendFormat ("&username={0}", EscapeUri ("fp client 1.6"));
            } else {
                string time = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds.ToString ();
                data.AppendFormat ("time={0}", time);//seconds since 1er janvier 1970
                data.AppendFormat ("&username={0}", account.UserName);
                data.AppendFormat ("&auth={0}", ConvertToMd5(account.Password + time));
                data.AppendFormat ("&authlower={0}", ConvertToMd5(account.Password.ToLower() + time));
            }

            if (!string.IsNullOrEmpty(track.MusicBrainzId))
                data.AppendFormat ("&mbid={0}", EscapeUri (track.MusicBrainzId));

            return data.ToString ();
        }

        string ConvertToMd5 (string input)
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

        string ToHex (byte[] hash)
        {
            StringBuilder bld = new StringBuilder (hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                bld.Append (hash[i].ToString ("x2"));
            return bld.ToString ();
        }

        string GetlfmpVersion ()
        {
            //TODO get from lib
            return "1";
        }

        #region HTTP helpers

        private Stream Post (string uri, string urlParams, byte[] fingerprint)
        {
            Log.DebugFormat ("send post last.fm fingerprint.");
            // Do not trust docs : it doesn't work if parameters are in the request body
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (String.Concat (uri, "?", urlParams));
            request.UserAgent = LastfmCore.UserAgent;
            request.Timeout = 10000;
            request.ProtocolVersion = HttpVersion.Version11;
            request.Method = "POST";
            request.ServicePoint.Expect100Continue = false;
            string boundary = "----" + System.Guid.NewGuid().ToString();
            request.PreAuthenticate = true;
            request.AllowWriteStreamBuffering = true;

            request.UserAgent = "libcurl-agent/1.0";
            request.Accept = "*/*";

            // Build Contents for Post
            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"fpdata\"\r\n\r\n", boundary);
            string footer = string.Format("\r\n--{0}--\r\n", boundary);

            // This is sent to the Post
            byte[] bytes = Encoding.UTF8.GetBytes(header);
            byte[] bytes2 = Encoding.UTF8.GetBytes(footer);

            request.ContentLength = bytes.Length + fingerprint.Length + bytes2.Length;
            request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);

            using(Stream newStream = request.GetRequestStream ())
            {
                newStream.Write (bytes, 0, bytes.Length);
                newStream.Write (fingerprint, 0, fingerprint.Length);
                newStream.Write (bytes2, 0, bytes2.Length);
                newStream.Close ();

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
        }

#endregion
    }
}
