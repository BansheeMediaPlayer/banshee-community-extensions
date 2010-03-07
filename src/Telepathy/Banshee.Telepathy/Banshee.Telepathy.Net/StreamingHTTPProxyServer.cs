//
// StreamingHTTPProxyServer.cs
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
using Banshee.Sources;
using Banshee.Telepathy.Data;
using Banshee.Web;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

using Mono.Unix;

namespace Banshee.Telepathy.Net
{
    internal class StreamingHTTPProxyServer : BaseHttpServer
    {
        public StreamingHTTPProxyServer() : base (new IPEndPoint (IPAddress.Any, 7777), "StreamingHTTPProxyServer")
        {
//            port = 7777;
//            (this.EndPoint as IPEndPoint).Port = (int) port;
        }

//        public override void Start (int backlog) 
//        {
//            base.Start (backlog);
//           port = (ushort)(server.LocalEndPoint as IPEndPoint).Port;
//        }

        protected override bool HandleRequest (Socket client) 
        {
            if (!client.Connected) {
                return false;
            }
            
            bool keep_connection = false;

            Socket stream_socket = null;
            try {
                stream_socket = GetServerSocket (client);
                Hyena.Log.DebugFormat ("Server socket from stream tube is {0}", 
                    stream_socket == null ? "null" : stream_socket.Connected.ToString ());
    
                if (stream_socket != null && stream_socket.Connected) {
                    keep_connection = true;
                
                    Hyena.Log.Debug ("Sending stream request through telepathy tube...");
                    keep_connection = ProxyHTTPRequest (client, stream_socket);
                    Hyena.Log.Debug ("Sent stream request through tube...");
                    
                    ReceiveData (stream_socket, client);
                }
            } finally {
                if (stream_socket != null) {
                    stream_socket.Close ();
                }
            }
        
            return keep_connection;
        }

        protected override void HandleValidRequest (Socket client, string [] split_request, string [] body_request)
        {
            throw new NotImplementedException ("HandleValidRequest not implemented.");
        }

        private void ReceiveData (Socket input, Socket output)
        {
            if (input == null || output == null || !input.Connected || !output.Connected) {
                return;
            }

            Hyena.Log.Debug ("Waiting for stream...");
            
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (output, false))) {
                using (BinaryReader reader = new BinaryReader (new NetworkStream (input, false))) {
                    
                    while (true) {
                        byte [] buffer = reader.ReadBytes (ChunkLength);
                        if (buffer == null) {
                            break;
                        }
                        
                        writer.Write (buffer);
                        
                        if (buffer.Length < ChunkLength) {
                            break;
                        }
                    }
                }
            }
        }
        
        private bool ProxyHTTPRequest (Socket input, Socket output)
        {
            bool keep_connection = true;

            string request = String.Empty;
            string line = null;
            
            if (input == null || output == null || !input.Connected || !output.Connected) {
                return keep_connection;
            }

            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (output, false))) {
                using(StreamReader reader = new StreamReader (new NetworkStream (input, false))) {
                    do {
                        line = reader.ReadLine ();
                        if (line.ToLower () == "connection: close") {
                            keep_connection = false;
                        }

                        request += line + "\r\n";
                    } while (line != String.Empty && line != null);
                }

                writer.Write (Encoding.UTF8.GetBytes (request));
            }

            return keep_connection;
        }
            
        private Socket GetServerSocket (Socket client)
        {
            TrackInfo track = Banshee.ServiceStack.ServiceManager.PlayerEngine.CurrentTrack;
            if (track == null) {
                return null;
            }
            
            Socket stream_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            
            Contact contact = ContactTrackInfo.From (track).Contact;
            
            if (contact != null) {
                DispatchManager dm = contact.DispatchManager;

                StreamActivity activity = dm.Get <StreamActivity> (contact, StreamingServer.ServiceName);
                if (activity != null) {
                    stream_socket.Connect (new UnixEndPoint (activity.Address));
                }
            }

            return stream_socket;
        }

//        private ushort port;
//        public ushort Port {
//            get { 
//                return port;
//            }
//        }

        private static IPAddress local_address = IPAddress.Parse("127.0.0.1");
        public IPAddress IPAddress {
            get {
                return local_address;
            }
        }
        
        public string HttpBaseAddress {
            get {
                return String.Format("http://{0}:{1}/", IPAddress, Port);
            }
        }
    }
}
