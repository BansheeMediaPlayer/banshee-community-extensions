//
// Channel.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
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
using System.Collections.Generic;

using DBus;

using Banshee.Telepathy.API.DBus;

using Telepathy;

namespace Banshee.Telepathy.API.Channels
{
    public abstract class Channel : IDisposable
    {
        private Connection conn;
        private IRequests requests = null;
        protected IDictionary<string, object> channel_properties;
        private string chann_path = null;
        private uint target_handle;
        private string channel_type;
        private uint initiator;

        public event EventHandler <EventArgs> ChannelReady;

        private Channel ()
        {
        }

        protected Channel (Connection conn)
        {
            if (conn == null) {
                throw new ArgumentNullException ("conn");
            }

            this.conn = conn;
            InitializeRequestsInterface ();
        }

        protected Channel (Connection conn, uint target_handle, string channel_type) : this (conn)
        {
            if (target_handle == 0 || target_handle == conn.SelfHandle) {
                throw new ArgumentException ("Invalid TargetId", "target_id");
            }

            this.target_handle = target_handle;
            this.ChannelType = channel_type;
        }

        public uint SelfHandle {
            get { return conn.SelfHandle; }
        }

        public uint InitiatorHandle {
            get { return initiator; }
        }

        protected Connection Connection {
            get { return conn; }
        }

        public virtual uint TargetHandle {
            get { return target_handle; }
            protected set { target_handle = value; }
        }

        public string ChannPath {
            get { return chann_path; }
            protected set { chann_path = value; }
        }

        protected IRequests Requests {
            get { return requests; }
        }

        public string ChannelType {
            get { return channel_type; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("channel_type");
                }
                channel_type = value;
            }
        }

        public abstract void Request ();

        public void Close ()
        {
            try {
                if (chann_path != null) {
                    IChannel channel = DBusUtility.GetProxy <IChannel> (conn.Bus, conn.BusName, chann_path);
                    // some channels can't be closed, so catch exception and log
                    // also, channel might be cleaned up by Telepathy already, so attempting to close
                    // may result in an exception
                    channel.Close ();
                    channel = null;
                    chann_path = null;
                }
            }
            catch (Exception) {
            }
        }

        protected void InitializeRequestsInterface ()
        {
            requests = conn.Requests;
            requests.NewChannels += OnNewChannels;
            requests.ChannelClosed += OnChannelClosed;     // TODO do I really need this?
            //self_handle = cm.SelfHandle;

        }

        protected abstract void ProcessNewChannel (ChannelDetails c);

        protected void OnNewChannels (ChannelDetails[] channels)
        {
            if (chann_path != null) {
                return;
            }

            foreach (ChannelDetails c in channels) {

                ObjectPath object_path = c.Channel;
                string channel_type = (string) c.Properties[Constants.CHANNEL_IFACE + ".ChannelType"];
                HandleType handle_type = (HandleType) c.Properties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                uint target_handle = (uint) c.Properties[Constants.CHANNEL_IFACE + ".TargetHandle"];

                Console.WriteLine ("NewChannel detected: object path {0}, channel_type {1}, handle_type {2}, target_handle {3}",
                                   object_path, channel_type, handle_type.ToString (), target_handle);

                initiator = (uint) DBusUtility.GetProperty (Connection.Bus, Connection.BusName, object_path.ToString (),
                                                                     Constants.CHANNEL_IFACE, "InitiatorHandle");

                if (channel_type.Equals (ChannelType)  &&
                    (target_handle == TargetHandle || initiator == TargetHandle)) {
                        ChannPath = object_path.ToString ();
                        ProcessNewChannel (c);
                        return;
                }

            }
        }

        protected virtual void OnChannelClosed (ObjectPath object_path)
        {
            if (object_path.ToString ().Equals (chann_path)) {
                chann_path = null;
            }
        }

        protected virtual void OnChannelReady (EventArgs args)
        {
            EventHandler <EventArgs> handler = ChannelReady;
            if (handler != null) {
                handler (this, args);
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (!disposing) {
                return;
            }

            if (requests != null) {
                try {
                    requests.NewChannels -= OnNewChannels;
                    requests.ChannelClosed -= OnChannelClosed;
                }
                catch (Exception) {}
                requests = null;
            }

            Close ();
        }

    }
}
