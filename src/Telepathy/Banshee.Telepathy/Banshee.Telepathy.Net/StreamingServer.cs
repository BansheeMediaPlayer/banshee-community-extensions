//
// StreamingServer.cs
//
// Author:
//   Neil Loknath   <neil.loknath@gmail.com             
//
// Copyright (C) 2009 Neil Loknath
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
using System.Text;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Web;

using Mono.Unix;

namespace Banshee.Telepathy.Net
{
    // HACK using a proxy, StreamingHTTPProxyServer, to get to this due to Mono bug
    // https://bugzilla.novell.com/show_bug.cgi?id=481687
    // We need to use Unix sockets for the StreamTube, but GStreamer does not provide
    // a plugin for Unix sockets. So, the HTTP proxy is used to hook everything up.
    internal class StreamingServer : BaseHttpServer
    {
        static StreamingServer ()
        {
            local_address = "/tmp/banshee-TS-" + GenerateRandomString (8);
        }
        
        public StreamingServer () : base (new UnixEndPoint (StreamingServer.Address), "Streaming Server")
        {
        }

        public override void Stop () 
        {
            base.Stop ();

            if (File.Exists (Address)) {
                try {
                    File.Delete (Address);
                }
                catch (Exception e) {
                    Hyena.Log.Exception (e);
                }
            }
        }

        protected override bool HandleRequest (Socket client)
        {
            if (Banshee.Telepathy.Data.ContactContainerSource.AllowStreamingSchema.Get ()) {
                base.HandleRequest (client);
            }

            return false;
        }
        
        protected override void HandleValidRequest (Socket client, string [] split_request, string [] body_request)
        {
            Hyena.Log.Debug ("Processing stream request from telepathy tube...");
            
            long offset = 0;
            foreach (string line in body_request) {
                if (line.ToLower ().Contains ("range:")) {
                    offset = ParseRangeRequest (line);
                }
            }
            
            if(split_request[1].StartsWith("/")) {
               split_request[1] = split_request[1].Substring(1);
            }

            string [] nodes = split_request[1].Split('/');
            string body = String.Empty;
            HttpStatusCode code = HttpStatusCode.OK;

            if (nodes.Length == 2 && nodes[0] != String.Empty) {

                bool track_found = false;
                
                long id = 0;
                try {
                    id = Convert.ToInt64 (nodes[0]);
                    
                    Hyena.Log.Debug ("Attempting to stream track through tube...");
                    
                    StreamTrack (client, id, offset);
                    track_found = true;
                } catch {}
                
                
                if (!track_found) {
                    code = HttpStatusCode.BadRequest;
                    body = GetHtmlHeader("Invalid Request");
                    body += String.Format("<p>Stream error with id `{0}'</p>", id);
                }
                
            } else {
               code = HttpStatusCode.BadRequest;
               body = GetHtmlHeader("Invalid Request");
               body += String.Format("<p>The request '{0}' could not be processed by server.</p>",
                   Escape (split_request[1]));
            }

            WriteResponse(client, code, body + GetHtmlFooter());
        }

        protected void StreamTrack (Socket client, long track_id)
        {
            StreamTrack (client, track_id, 0);
        }
        
        protected virtual void StreamTrack (Socket client, long track_id, long offset)
        {
            Stream stream;
            
            DatabaseTrackInfo track = DatabaseTrackInfo.Provider.FetchSingle ((int) track_id);
            if (track != null) {
                stream = new FileStream (track.LocalPath, FileMode.Open, FileAccess.Read);
                if (stream != null) {
                    if (offset > 0) {
                        stream.Position = offset;
                    }
                    
                    Hyena.Log.Debug ("Sending stream through tube...");
                    
                    WriteResponseStream (client, 
                                         stream, 
                                         offset == 0 ? track.FileSize : track.FileSize - offset, 
                                         new FileInfo (track.LocalPath).Name, 
                                         offset);
                    stream.Close ();
                }
            }
            
            client.Close ();
        }

        private static string GenerateRandomString (int length)
        {
            Random random = new Random ();
            StringBuilder builder = new StringBuilder ();

            for (int i = 0; i < length; i++) {
                char c = (char) (int) (Math.Floor (26 * random.NextDouble () + 65));
                builder.Append (c);
            }

            return builder.ToString ();
        }
        
        protected static string GetHtmlHeader (string title)
        {
            return String.Format ("<html><head><title>{0} - Banshee Telepathy Browser</title></head><body><h1>{0}</h1>", 
                title);
        }
        
        protected static string GetHtmlFooter ()
        {
            return String.Format ("<hr /><address>Generated on {0} by " + 
                "Banshee Telepathy Plugin (<a href=\"http://banshee-project.org\">http://banshee-project.org</a>)",
                DateTime.Now.ToString ());
        }

        public static string ServiceName {
            get { return "bansheetelepathystreamer"; }
        }

        private static string local_address;
        public static string Address {
            get {
                return local_address;
            }
        }
    }
}
