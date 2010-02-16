//
// LCDClient.cs
//
// Authors:
//   André Gaul
//
// Copyright (C) 2010 André Gaul
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
using System.Net;
using System.Net.Sockets;
using System.Text;

using Hyena;

namespace Banshee.LCD
{
    public class LCDClient : IDisposable
    {
        class StateObject
        {
            internal byte[] buf;
            internal Socket socket;
            internal LCDClient lcdclient;
            internal StateObject(int size, Socket socket, LCDClient lcdclient) {
                this.buf = new byte[size];
                this.socket = socket;
                this.lcdclient = lcdclient;
            }
        }

        private Socket socket;
        private string host;
        private ushort port;
        private bool connecting;
        private const int bufsize = 512;
        private byte[] recvbuf;
        public LCD lcd;

        public Dictionary<LCDScreen, HashSet<LCDWidget> > screens;
        public delegate void ConnectedHandler();
        public event ConnectedHandler Connected;
        //TODO: public event ConnectedHandler Disconnected;

        public LCDClient(string host, ushort port)
        {
            this.host = host;
            this.port = port;
            recvbuf = new byte[bufsize];
            screens = new Dictionary<LCDScreen, HashSet<LCDWidget> >();
            socket = new Socket(AddressFamily.InterNetwork,
                                SocketType.Stream,
                                ProtocolType.Tcp);
            lcd = null;
            connecting = true;
            Hyena.Log.Debug("Connecting to "+host+":"+port.ToString());

            try {
                IPAddress[] addr = Dns.GetHostEntry(host).AddressList;
                socket.BeginConnect(addr, port, new AsyncCallback(OnConnect),this);
            }
            catch (Exception e) {
                Hyena.Log.Debug("Could not connect to "+host+":"+port.ToString()+": "+e.ToString());
            }
        }

        public void Dispose ()
        {
            Hyena.Log.Debug("Disposing LCDClient");
            socket.Close();
        }

        public void SendString(string str)
        {
            //Hyena.Log.Debug("Send string: "+str);
            byte[] sendbuf= Encoding.ASCII.GetBytes(str);
            socket.BeginSend(sendbuf,0,sendbuf.Length,SocketFlags.None,new AsyncCallback(OnSend),this);
        }

        private static void OnConnect(IAsyncResult result)
        {
            LCDClient client = (LCDClient)result.AsyncState;
            if (!result.IsCompleted) {
                Hyena.Log.Debug("Not yet connected to "+client.host+":"+client.port.ToString());
                return;
            }
            if (!client.socket.Connected)
            {
                Hyena.Log.Warning("Could not connect to "+client.host+":"+client.port.ToString());
                client.connecting = false;
                return;
            }
            client.socket.EndConnect(result);
            Hyena.Log.Debug("Connected to "+client.host+":"+client.port.ToString());


            client.socket.BeginReceive(client.recvbuf,0,bufsize,SocketFlags.None,new AsyncCallback(OnReceive),client);
            client.SendString("hello\n");
        }

        private static void OnSend(IAsyncResult result)
        {
            LCDClient client = (LCDClient)result.AsyncState;
            client.socket.EndSend(result);
            //int bytecount = client.socket.EndSend(result);
            //Hyena.Log.Debug("Sent "+bytecount.ToString()+" bytes");
        }

        private static void OnReceive(IAsyncResult result)
        {
            LCDClient client = (LCDClient)result.AsyncState;

            int bytecount = client.socket.EndReceive(result);
            string s=Encoding.ASCII.GetString(client.recvbuf,0,bytecount);

            client.recvbuf.Initialize();
            //Hyena.Log.Debug("Received "+bytecount.ToString()+" bytes: "+s.Trim());
            if (client.connecting)
            {
                string[] ssplit = s.Split(" \n".ToCharArray(), 20);
                client.lcd = new LCD(Int32.Parse(ssplit[7]),
                              Int32.Parse(ssplit[9]),
                              Int32.Parse(ssplit[11]),
                              Int32.Parse(ssplit[13]));
                client.SendString("client_set -name Banshee\n");
                client.connecting=false;

                client.Connected();

            }
            client.socket.BeginReceive(client.recvbuf,0,bufsize,SocketFlags.None,new AsyncCallback(OnReceive),client);
        }

        public void RegScreen(LCDScreen screen)
        {
            if (screens.ContainsKey(screen))
            {
                Hyena.Log.Warning("Screen "+screen.name+" already registered");
                return;
            }
            screens.Add(screen, new HashSet<LCDWidget>());

            SendString("screen_add "+screen.name+"\n");
            UpdScreen(screen);
        }

        public void UnregScreen(LCDScreen screen)
        {
            if (!screens.ContainsKey(screen))
            {
                Hyena.Log.Warning("Screen "+screen.name+" not registered");
                return;
            }

            foreach(LCDWidget widget in screens[screen])
                SendString("widget_del "+widget.name+"\n");

            SendString("screen_del "+screen.name+"\n");
            screens.Remove(screen);
        }

        public void UpdScreen(LCDScreen screen)
        {
            if (!screens.ContainsKey(screen))
            {
                Hyena.Log.Warning("Screen "+screen.name+" not registered");
                return;
            }

            SendString(screen.GetStringSet()+"\n");
        }

        public void RegWidget(LCDScreen screen, LCDWidget widget)
        {
            if (!screens.ContainsKey(screen))
            {
                Hyena.Log.Warning("Screen "+screen.name+" not registered");
                return;
            }

            if (screens[screen].Contains(widget))
            {
                Hyena.Log.Warning("Widget "+widget.name+" already registered in screen "+screen.name);
                return;
            }

            screens[screen].Add(widget);
            SendString("widget_add "+screen.name+" "+widget.name+" "+widget.typename+"\n");
        }

        public void UnregWidget(LCDScreen screen, LCDWidget widget)
        {
            if (!screens.ContainsKey(screen))
            {
                Hyena.Log.Warning("Screen "+screen.name+" not registered");
                return;
            }

            if (!screens[screen].Contains(widget))
            {
                Hyena.Log.Warning("Widget "+widget.name+" not registered in screen "+screen.name);
                return;
            }

            SendString("widget_del "+screen.name+"\n");
            screens[screen].Remove(widget);
        }

        public void UpdWidgetsAll(LCDParser parser)
        {
            foreach(KeyValuePair<LCDScreen, HashSet<LCDWidget>> pair in screens)
            {
                LCDScreen screen=pair.Key;
                foreach(LCDWidget widget in pair.Value)
                {
                    SendString("widget_set "+screen.name+" "+widget.name+" "+widget.GetSetString(parser)+"\n");
                }
            }
        }

        public void UpdWidget(LCDScreen screen, LCDWidget widget, LCDParser parser)
        {
            if (!screens.ContainsKey(screen))
            {
                Hyena.Log.Warning("Screen "+screen.name+" not registered");
                return;
            }

            if (!screens[screen].Contains(widget))
            {
                Hyena.Log.Warning("Widget "+widget.name+" not registered in screen "+screen.name);
                return;
            }

            SendString("widget_set "+screen.name+" "+widget.name+" "+widget.GetSetString(parser)+"\n");
        }
    }
}
