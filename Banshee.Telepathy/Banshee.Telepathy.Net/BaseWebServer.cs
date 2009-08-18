//
// BaseWebServer.cs
//
// Author:
//   Aaron Bockover <aaron@aaronbock.net>
//   James Wilcox   <snorp@snorp.net>
//   Neil Loknath   <neil.loknath@gmail.com             
//
// Copyright (C) 2005-2006 Novell, Inc.
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
using System.Collections.Generic;

namespace Banshee.Web
{
    public abstract class BaseWebServer
    {
        protected Socket server;
        
        protected readonly ArrayList clients = new ArrayList();
              
        public BaseWebServer () 
        {
        }

        public BaseWebServer (EndPoint ep) : this ()
        {
            this.EndPoint = ep;
            DefaultEndPoint = null;
        }

        public BaseWebServer (EndPoint ep, EndPoint default_ep) : this (ep)
        {
            this.DefaultEndPoint = default_ep;
        }

        private string name = "WebServer";
        public string Name {
            get { return name; }
            set {
                if (value == null) {
                    throw new ArgumentNullException ("name");
                }
                else if (running) {
                    throw new InvalidOperationException ("Cannot set Name while running.");
                }
                name = value;
            }
        }

        public AddressFamily AddressFamily {
            get { return end_point.AddressFamily; }
        }
            
        private EndPoint end_point = new IPEndPoint (IPAddress.Any, 8089);
        protected EndPoint EndPoint {
            get { return end_point; }
            set {
                if (value == null) {
                    throw new ArgumentNullException ("end_point");
                }
                if (running) {
                    throw new InvalidOperationException ("Cannot set EndPoint while running.");
                }
                end_point = value; 
            }
        }

        private EndPoint default_end_point = new IPEndPoint (IPAddress.Any, 0);
        protected EndPoint DefaultEndPoint {
            get { return default_end_point; }
            set {
                if (running) {
                    throw new InvalidOperationException ("Cannot set DefaultEndPoint while running.");
                }
                default_end_point = value; 
            }
        }
        
        private bool running;
        public bool Running {
            get { return running; }
            protected set { running = value; }
        }

        private int chunk_length = 8192;
        public int ChunkLength {
            get { return chunk_length; }
            set {
                if (chunk_length < 1) {
                    throw new ArgumentOutOfRangeException ("chunk_length", "Must be > 0");
                }
                if (running) {
                    throw new InvalidOperationException ("Cannot change ChunkLength while server is running.");
                }
                chunk_length = value;
            }
        }

        public void Start ()
        {
            Start (10);
        }
        
        public virtual void Start (int backlog) 
        {
            if (backlog < 0) {
                throw new ArgumentOutOfRangeException ("backlog");
            }
            
            if (running) {
                return;
            }
            
            server = new Socket (this.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.IP);
            try {
                server.Bind (this.EndPoint);
            } catch (System.Net.Sockets.SocketException) {
                if (DefaultEndPoint != null) {
                    server.Bind (this.DefaultEndPoint);
                }
                else {
                    throw;
                }
            }
            
            server.Listen (backlog);

            running = true;
            Thread thread = new Thread (ServerLoop);
            thread.Name = this.Name;
            thread.IsBackground = true;
            thread.Start ();
        }

        public virtual void Stop () 
        {
            running = false;
            
            if (server != null) {
                server.Close ();
                server = null;
            }

            foreach (Socket client in (ArrayList) clients.Clone ()) {
                client.Close ();
            }
        }
        
        private void ServerLoop ()
        {
            while (true) {
                try {
                    if (!running) {
                        break;
                    }
                    
                    Socket client = server.Accept ();
                    clients.Add (client);
                    ThreadPool.QueueUserWorkItem (HandleConnection, client);
                } catch (SocketException) {
                    break;
                }
            }
        }
        
        private void HandleConnection (object o) 
        {
            Socket client = (Socket) o;

            try {
                while (HandleRequest(client));
            } catch (IOException) {
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            } finally {
                clients.Remove (client);
                client.Close ();
            }
        }

        protected virtual long ParseRangeRequest (string line)
        {
            long offset = 0;
            if (String.IsNullOrEmpty (line)) {
                return offset;
            }

            string [] split_line = line.Split (' ', '=', '-');
            foreach (string word in split_line) {
                if (long.TryParse (word, out offset)) {
                    return offset;
                }
            }

            return offset;
        }
        
        protected virtual bool HandleRequest (Socket client) 
        {
            if (client == null || !client.Connected) {
                return false;
            }
            
            bool keep_connection = true;
            
            using (StreamReader reader = new StreamReader(new NetworkStream(client, false))) {

                string request = reader.ReadLine ();
                
                if (request == null) {
                    return false;
                }

                List <string> body = new List <string> ();
                string line = null;
                
                do {
                    line = reader.ReadLine ();
                    if (line.ToLower () == "connection: close") {
                        keep_connection = false;
                    }
                    body.Add (line);
                } while (line != String.Empty && line != null);

                string [] split_request = request.Split ();
                
                if (split_request.Length < 3) {
                    WriteResponse (client, HttpStatusCode.BadRequest, "Bad Request");
                    return keep_connection;
                } else {
                    try {
                        HandleValidRequest (client, split_request, body.ToArray () );
                    } catch (IOException) {
                        keep_connection = false;
                    } catch (Exception e) {
                        keep_connection = false;
                        Console.Error.WriteLine("Trouble handling request {0}: {1}", split_request[1], e);
                    }
                }
            }

            return keep_connection;
        }

        protected void HandleValidRequest(Socket client, string [] split_request)
        {
            HandleValidRequest (client, split_request, null);
        }
        
        protected abstract void HandleValidRequest(Socket client, string [] split_request, string [] request_body);
            
        protected void WriteResponse (Socket client, HttpStatusCode code, string body) 
        {
            WriteResponse (client, code, Encoding.UTF8.GetBytes (body));
        }
        
        protected virtual void WriteResponse (Socket client, HttpStatusCode code, byte [] body) 
        {
            if (client == null || !client.Connected) {
                return;
            }
            else if (body == null) {
                throw new ArgumentNullException ("body");
            }
            
            string headers = String.Empty;
            headers += String.Format("HTTP/1.1 {0} {1}\r\n", (int) code, code.ToString ());
            headers += String.Format("Content-Length: {0}\r\n", body.Length);
            headers += "Content-Type: text/html\r\n";
            headers += "Connection: close\r\n";
            headers += "\r\n";
            
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                writer.Write (Encoding.UTF8.GetBytes (headers));
                writer.Write (body);
            }
            
            client.Close ();
        }

        protected void WriteResponseStream (Socket client, Stream response, long length, string filename)
        {
            WriteResponseStream (client, response, length, filename, 0);
        }
        
        protected virtual void WriteResponseStream (Socket client, Stream response, long length, string filename, long offset)
        {
            if (client == null || !client.Connected) {
                return;
            }
            else if (response == null) {
                throw new ArgumentNullException ("response");
            }
            else if (length < 1) {
                throw new ArgumentOutOfRangeException ("length", "Must be > 0");
            }
            else if (offset < 0) {
                throw new ArgumentOutOfRangeException ("offset", "Must be positive.");
            }

            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                string headers = "HTTP/1.1 200 OK\r\n";

                if (offset > 0) {
                    headers = "HTTP/1.1 206 Partial Content\r\n";
                    headers += String.Format ("Content-Range: {0}-{1}\r\n", offset, offset + length);
                }

                if (length > 0) {
                    headers += String.Format ("Content-Length: {0}\r\n", length);
                }
                
                if (filename != null) {
                    headers += String.Format ("Content-Disposition: attachment; filename=\"{0}\"\r\n",
                        filename.Replace ("\"", "\\\""));
                }
                
                headers += "Connection: close\r\n";
                headers += "\r\n";
                
                writer.Write (Encoding.UTF8.GetBytes(headers));
                    
                using (BinaryReader reader = new BinaryReader (response)) {
                    while (true) {
                        byte [] buffer = reader.ReadBytes (ChunkLength);
                        if (buffer == null) {
                            break;
                        }
                        
                        writer.Write(buffer);
                        
                        if (buffer.Length < ChunkLength) {
                            break;
                        }
                    }
                }
            }
        }

        protected static string Escape (string input)
        {
            return String.IsNullOrEmpty (input) ? "" : System.Web.HttpUtility.HtmlEncode (input);
        }
        
        protected static string GetHtmlHeader (string title)
        {
            return String.Format ("<html><head><title>{0} - Banshee Web Server</title></head><body><h1>{0}</h1>", 
                title);
        }
        
        protected static string GetHtmlFooter ()
        {
            return String.Format ("<hr /><address>Generated on {0} by " + 
                "Banshee Web Server (<a href=\"http://banshee-project.org\">http://banshee-project.org</a>)",
                DateTime.Now.ToString ());
        }
    }
}
