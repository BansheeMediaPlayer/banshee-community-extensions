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
        private const string API_ROOT = "ws.audioscrobbler.com/fingerprint/query/";

        private Stream response_stream;

        public FingerprintRequest ()
        {}

        public Stream GetResponseStream ()
        {
            return response_stream;
        }

        public void Send (TrackInfo track, byte[] fingerprint)
        {
            response_stream = Post (API_ROOT, BuildPostData (track), fingerprint);
        }

        public int GetFpId ()
        {
            if (response_stream.Length < 4)
                return -1;

            int ret = 0;
            ret = response_stream.ReadByte();
            ret = (ret << 8 ) + response_stream.ReadByte();
            ret = (ret << 8 ) + response_stream.ReadByte();
            ret = (ret << 8 ) + response_stream.ReadByte();

            return ret;
        }

        private string BuildPostData (TrackInfo track)
        {
            StringBuilder data = new StringBuilder ();
            data.AppendFormat ("artist={0}", Uri.EscapeDataString (track.ArtistName));
            data.AppendFormat ("&album={0}", Uri.EscapeDataString (track.AlbumTitle));
            data.AppendFormat ("&track={0}", Uri.EscapeDataString (track.TrackTitle));

            if (track.TrackNumber > 0)
                data.AppendFormat ("&tracknum={0}", track.TrackNumber);

            if (track.Year > 0)
                data.AppendFormat ("&year={0}", track.Year);

            data.AppendFormat ("&genre={0}", Uri.EscapeDataString (track.Genre));
            data.AppendFormat ("&duration={0}", track.Duration.TotalSeconds);
            data.AppendFormat ("&username={0}", Uri.EscapeDataString (track.AlbumTitle));
            data.AppendFormat ("&samplerate={0}", track.SampleRate);
            data.AppendFormat ("&mbid={0}", Uri.EscapeDataString (track.MusicBrainzId));
            string[] slashedUri = track.Uri.LocalPath.Split('/', '\\');
            data.AppendFormat ("&filename={0}", Uri.EscapeDataString (slashedUri [slashedUri.Length - 1]));

            SHA256Managed cr = new  SHA256Managed ();
            byte[] hash = cr.ComputeHash (File.ReadAllBytes (track.Uri.AbsoluteUri));

            data.AppendFormat ("&sha256={0}", ToHex (hash));
            data.AppendFormat ("&fpversion={0}", Uri.EscapeDataString (GetlfmpVersion ()));

            return data.ToString ();
        }

        string ToHex (byte[] hash)
        {
            StringBuilder bld = new StringBuilder (hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                bld.Append (hash[i].ToString ("X2"));
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
            // Do not trust docs : it doesn't work if parameters are in the request body
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (String.Concat (uri, "?", urlParams));
            request.UserAgent = LastfmCore.UserAgent;
            request.Timeout = 10000;
            request.Method = "POST";
            request.KeepAlive = false;
            request.ContentType = "multipart/form-data";
            //request.Expect = "100-continue";
            byte[] data2 = Encoding.ASCII.GetBytes ("fpdata=");
            Stream newStream = request.GetRequestStream ();
            newStream.Write (data2, 0, data2.Length);
            newStream.Write (fingerprint, 0, fingerprint.Length);
            newStream.Close ();

            request.ContentLength = newStream.Length;

            HttpWebResponse response = null;
            try {
                response = (HttpWebResponse) request.GetResponse ();
            } catch (WebException e) {
                Log.DebugException (e);
                response = (HttpWebResponse)e.Response;
            }
            return response.GetResponseStream ();
        }

#endregion
    }
}
