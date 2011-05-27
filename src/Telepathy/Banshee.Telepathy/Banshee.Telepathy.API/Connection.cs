//
// Connection.cs
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

using Telepathy;

//using Data = Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API
{
    [Flags]
    public enum ConnectionCapabilities {
        None = 0,
        DBusTube = 1,
        StreamTube = 2,
        FileTransfer = 4
    }

    public class Connection : IDisposable
    {
        private ConnectionCapabilities capabilities_mask;
        private readonly ChannelInfoCollection supported_channels = new ChannelInfoCollection ();

        public event EventHandler <EventArgs> Disconnected;

        protected Connection ()
        {
        }

        public Connection (Account account) : this (account.BusName, account.ObjectPath, account.AccountId, account.AccountObjectPath)
        {
        }

        public Connection (Account account, ConnectionCapabilities capabilities_mask) : this (account.BusName, account.ObjectPath, account.AccountId, account.AccountObjectPath, capabilities_mask)
        {
        }

        public Connection (string bus_name, string object_path, string account_id, string account_path)
        {
            this.AccountId = account_id;
            this.AccountObjectPath = account_path;
            this.BusName = bus_name;
            this.ObjectPath = object_path;

            this.conn = DBusUtility.GetProxy <IConnection> (bus, bus_name, object_path);
            self_handle = (uint) DBusUtility.GetProperty (bus, bus_name, object_path,
                                                   Constants.CONNECTION_IFACE, "SelfHandle");
            Initialize ();
        }

        public Connection (string bus_name,
                           string object_path,
                           string account_id,
                           string account_path,
                           ConnectionCapabilities capabilities_mask) : this (bus_name, object_path, account_id, account_path)
        {
            if (!CapabilitiesSupported (capabilities_mask)) {
                throw new NotSupportedException ("Capabilities not supported");
            }

            //HACK reset in case there was a crash
            //capabilities.SetSelfCapabilities (new Dictionary <string, object> [0] );
         }

        private DispatchManager dispatch_manager = null;
        public DispatchManager DispatchManager {
            get { return dispatch_manager; }
        }

        private ChannelHandler channel_handler = null;
        internal ChannelHandler ChannelHandler {
            get { return channel_handler; }
        }

        private IConnection conn = null;
        internal IConnection DBusProxy {
            get { return conn; }
            private set { conn = value; }
        }

        private IRequests requests = null;
        internal IRequests Requests {
            get { return requests; }
            private set { requests = value; }
        }

        private IContactCapabilities capabilities = null;
        internal IContactCapabilities ContactCapabilities {
            get { return capabilities; }
            private set { capabilities = value; }
        }

        private Roster roster = null;
        public virtual Roster Roster {
            get { return roster; }
            protected set { roster = value; }
        }

        private string bus_name;
        public string BusName {
            get { return bus_name; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("bus_name");
                }
                bus_name = value;
            }
        }

        private string object_path;
        public string ObjectPath {
            get { return object_path; }
            private set {
                if (value == null) {
                   throw new ArgumentNullException ("object_path");
                }
                object_path = value;
            }
        }

        private string account_path;
        public string AccountObjectPath {
            get { return account_path; }
            private set {
                if (value == null) {
                   throw new ArgumentNullException ("account_path");
                }
                account_path = value;
            }
        }

        private string account_id;
        public string AccountId {
            get { return account_id; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("account_id");
                }
                account_id = value;
            }
        }

        private uint self_handle;
        public uint SelfHandle {
            get { return self_handle; }
            private set { self_handle = value; }
        }

        private BusType bus = BusType.Session;
        public BusType Bus {
            get { return bus; }
            private set { bus = value; }
        }

        private ConnectionStatus status = ConnectionStatus.Connected;
        public ConnectionStatus Status {
            get { return status; }
            private set { status = value; }
        }

        private string cache_dir;
        public string CacheDirectory {
            get { return cache_dir; }
            set { cache_dir = value; }
        }

        private string client_name;
        public string ClientName {
            get { return client_name; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("client_name");
                }
                client_name = value;
            }
        }

        public ChannelInfoCollection SupportedChannels {
            get { return supported_channels; }
        }

        public override bool Equals (object obj)
        {
            if (obj as Connection == null) {
                throw new ArgumentNullException ("obj");
            }

            return object_path.Equals ((obj as Connection).ObjectPath);
        }

        public override int GetHashCode ()
        {
            return object_path.GetHashCode ();
        }

        public void AdvertiseSupportedChannels (string client_name, IList<Data.ChannelInfo> channels)
        {
            ClientName = client_name;

            foreach (Data.ChannelInfo channel in channels) {
                AddChannel (channel);
            }

            RegisterChannelHandler ();
        }

        private void AddChannel (Data.ChannelInfo channel)
        {
            if (channel.Type == ChannelType.DBusTube &&
               !CapabilitiesSupported (ConnectionCapabilities.DBusTube)) {
                throw new InvalidOperationException ("This connection does not support DBus tubes.");
            }
            else if (channel.Type == ChannelType.StreamTube &&
               !CapabilitiesSupported (ConnectionCapabilities.StreamTube)) {
                throw new InvalidOperationException ("This connection does not support Stream tubes.");
            }
            else if (channel.Type == ChannelType.FileTransfer &&
               !CapabilitiesSupported (ConnectionCapabilities.FileTransfer)) {
                throw new InvalidOperationException ("This connection does not support file transfers.");
            }
            lock (supported_channels) {
                supported_channels.Add (channel);
            }
        }

        public bool CapabilitiesSupported (ConnectionCapabilities mask)
        {
            if (IsMaskValid (mask)) {
                return (this.capabilities_mask & mask) == mask;
            }

            return false;
        }

        protected virtual void Initialize ()
        {
            requests = DBusUtility.GetProxy <IRequests> (bus, bus_name, object_path);
            capabilities = DBusUtility.GetProxy <IContactCapabilities> (bus, bus_name, object_path);


            conn.StatusChanged += OnStatusChanged;
            conn.SelfHandleChanged += OnSelfHandleChanged;

            LoadCapabilities ();

            dispatch_manager = new DispatchManager (this);


            CreateRoster ();
        }

        protected virtual void CreateRoster ()
        {
            //Log.DebugFormat ("Creating Roster for {0}", this.AccountId);
            roster = new Roster (this);
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

            if (dispatch_manager != null) {
                dispatch_manager.Dispose ();
            }

            if (ChannelHandler == null) {
                ChannelHandler.Destroy ();
            }

            if (roster != null) {
                roster.Dispose ();
                roster = null;
            }

            if (conn != null) {
                try {
                    conn.SelfHandleChanged -= OnSelfHandleChanged;
                    conn.StatusChanged -= OnStatusChanged;
                }
                catch (Exception) {}
                conn = null;
            }

            capabilities = null;
            dispatch_manager = null;
            requests = null;
        }

        protected virtual bool IsMaskValid (ConnectionCapabilities mask)
        {
            const ConnectionCapabilities max_caps = ConnectionCapabilities.DBusTube |
                ConnectionCapabilities.StreamTube |
                    ConnectionCapabilities.FileTransfer;

            if (mask <= ConnectionCapabilities.None || mask > max_caps) {
                return false;
            }

            return true;
        }

        private void LoadCapabilities ()
        {
            //FIXME hardcoding use of jabber connections due to Mono bug
            // https://bugzilla.novell.com/show_bug.cgi?id=481687
            // should really being using IRequests.RequestableChannelClasses
            // property, but the IConvertible object returned by dbus-sharp cannot be unboxed

            string protocol = conn.GetProtocol ();
            if (protocol != null && protocol.Equals ("jabber")) {
                capabilities_mask = ConnectionCapabilities.DBusTube |
                ConnectionCapabilities.FileTransfer |
                ConnectionCapabilities.StreamTube;
            }
        }

        protected virtual void RegisterChannelHandler ()
        {
            IDictionary<string, object> [] caps;
            int channel_count;

            lock (supported_channels) {
                channel_count = supported_channels.Count;
                if (channel_count == 0) {
                    return;
                }

                caps =  new Dictionary<string, object>[channel_count];
                int counter = 0;

                foreach (Data.ChannelInfo channel in supported_channels) {
                    caps[counter] = new Dictionary<string, object> ();

                    if (channel.Type == ChannelType.DBusTube) {
                        DBusTubeChannelInfo tube_info = channel as DBusTubeChannelInfo;
                        if (tube_info != null) {
                            caps[counter].Add ("org.freedesktop.Telepathy.Channel.ChannelType", Constants.CHANNEL_TYPE_DBUSTUBE);
                            caps[counter].Add (Constants.CHANNEL_TYPE_DBUSTUBE + ".ServiceName", tube_info.Service);
                        }

                        //Log.DebugFormat ("{0} adding service {1}", this.account_id, service.Service);
                    }
                    else if (channel.Type == ChannelType.StreamTube) {
                        StreamTubeChannelInfo tube_info = channel as StreamTubeChannelInfo;
                        if (tube_info != null) {
                            caps[counter].Add ("org.freedesktop.Telepathy.Channel.ChannelType", Constants.CHANNEL_TYPE_STREAMTUBE);
                            caps[counter].Add (Constants.CHANNEL_TYPE_STREAMTUBE + ".Service", tube_info.Service);
                        }
                    }
                    else if (channel.Type == ChannelType.FileTransfer) {

                        FileTransferChannelInfo transfer_info = channel as FileTransferChannelInfo;
                        if (transfer_info != null) {
                            caps[counter].Add ("org.freedesktop.Telepathy.Channel.ChannelType", Constants.CHANNEL_TYPE_FILETRANSFER);
                            caps[counter].Add (Constants.CHANNEL_TYPE_FILETRANSFER + ".ContentType", transfer_info.ContentType);
                            caps[counter].Add (Constants.CHANNEL_TYPE_FILETRANSFER + ".Description", transfer_info.Description);
                        }
                    }

                    caps[counter].Add ("org.freedesktop.Telepathy.Channel.TargetHandleType", channel.TargetHandleType);
                    counter++;
                }
            }

            if (ChannelHandler == null) {
                channel_handler = ChannelHandler.Create (ClientName, caps);
            }
        }

        protected virtual void OnDisconnected (EventArgs args)
        {
            EventHandler <EventArgs> handler = Disconnected;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnStatusChanged (ConnectionStatus status, ConnectionStatusReason reason)
        {
            this.status = status;

            if (status == ConnectionStatus.Disconnected) {
                OnDisconnected (EventArgs.Empty);
            }
        }

        private void OnSelfHandleChanged (uint handle)
        {
            //this.SelfHandle = handle;
        }
/*
		private bool IsConnectionSupported ()
        {
            bool supports_dbustube = false;
            bool supports_filetransfer = false;

            Properties p = bus.GetObject<Properties> (bus_name, object_path);
            object o = p.Get (Constants.REQUESTS_IFACE, "RequestableChannelClasses");

            RequestableChannelClass [] classes = (RequestableChannelClass []) Convert.ChangeType (o, typeof (RequestableChannelClass []));

            foreach (RequestableChannelClass c in classes) {
                if (c.FixedProperties.ContainsKey ("org.freedesktop.Telepathy.Channel.ChannelType")) {
                    if (c.FixedProperties["org.freedesktop.Telepathy.Channel.ChannelType"].Equals (Constants.CHANNEL_TYPE_DBUSTUBE)) {
                        supports_dbustube = true;
                    }
                    else if (c.FixedProperties["org.freedesktop.Telepathy.Channel.ChannelType"].Equals (Constants.CHANNEL_TYPE_FILETRANSFER)) {
                        supports_filetransfer = true;
                    }
                }
            }

            if (supports_dbustube && supports_filetransfer) {
                return true;
            }

            return false;
        }
*/
    }
}

