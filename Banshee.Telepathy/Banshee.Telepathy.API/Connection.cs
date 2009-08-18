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
using Telepathy.Draft;

using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API
{
    [Flags]
    public enum ConnectionCapabilities {
        None = 0,
        DBusTransport = 1,
        SocketTransport = 2,
        FileTransfer = 4
    }

    public class Connection : IDisposable
    {
        private ConnectionCapabilities capabilities_mask;
        private readonly IDictionary <string, ContactService> services = new Dictionary <string, ContactService> ();

        public event EventHandler <EventArgs> Disconnected;

        protected Connection ()
        {
        }
        
        protected Connection (string bus_name, string object_path)
        {
            this.BusName = bus_name;
            this.ObjectPath = object_path;
        }

        public Connection (string bus_name, string object_path, string account_id) : this (bus_name, object_path)
        {
            this.AccountId = account_id;
            this.conn = DBusUtility.GetProxy <IConnection> (bus, bus_name, object_path);
            self_handle = (uint) DBusUtility.GetProperty (bus, bus_name, object_path, 
                                                   Constants.CONNECTION_IFACE, "SelfHandle");
            Initialize ();
        }

        public Connection (string bus_name, string object_path, 
                           string account_id, ConnectionCapabilities capabilities_mask) : this (bus_name, object_path, account_id)
        {
            if (!CapabilitiesSupported (capabilities_mask)) {
                throw new NotSupportedException ("Capabilities not supported");
            }

            //HACK reset in case there was a crash
            capabilities.SetSelfCapabilities (new Dictionary <string, object> [0] );
         }

        private DispatchManager dispatch_manager = null;
        public DispatchManager DispatchManager {
            get { return dispatch_manager; }
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

        public int ServiceCount ()
        {
            lock (services) {
                return services.Count;
            }
        }
        
        public bool HasService (string service)
        {
            lock (services) {
                return services.ContainsKey (service);
            }
        }

        public bool HasService (ContactService service)
        {
            lock (services) {
                return services.ContainsKey (service.ToString ());
            }
        }

        public void AddService (ContactService service)
        {
            AddService (service, true);
        }
        
        public void AddService (ContactService service, bool advertise)
        {
            if (service.Type == ContactServiceType.DBusTransport && 
               !CapabilitiesSupported (ConnectionCapabilities.DBusTransport)) {
                throw new InvalidOperationException ("This connection does not support DBus transport.");
            } 
            else if (service.Type == ContactServiceType.SocketTransport &&
               !CapabilitiesSupported (ConnectionCapabilities.SocketTransport)) {
                throw new InvalidOperationException ("This connection does not support Socket transport.");
            } 
            else if (!services.ContainsKey (service.ToString ())) {
                lock (services) {
                    services.Add (service.ToString (), service);
                }
                if (advertise) {
                    AdvertiseServices ();
                }
            }
        }

        public bool RemoveService (ContactService service)
        {
            if (services.ContainsKey (service.ToString ())) {
                lock (services) {
                    services.Remove (service.ToString ());
                }
                AdvertiseServices ();
                return true;
                }

            return false;
        }

        public ContactService GetService (string service)
        {
            lock (services) {
                if (services.ContainsKey (service)) {
                    return services[service];
                }
                else {
                    throw new InvalidOperationException ("Service not found.");
                }
            }
        }
        
        public IEnumerable <ContactService> GetAllServices ()
        {
            lock (services) {
                foreach (KeyValuePair <string, ContactService> kv in services) {
                    yield return kv.Value;
                }
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

            // reset capabilities ie. do not advertise our Telepathy extension
            if (capabilities != null) {
                try {
                    capabilities.SetSelfCapabilities (new Dictionary <string, object> [0] );
                }
                catch (Exception e) {
                    Console.WriteLine (e.Message);
                }
                capabilities = null;
            }

            if (dispatch_manager != null) {
                dispatch_manager.Dispose ();
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

            dispatch_manager = null;
            requests = null;
        }
        
        protected virtual bool IsMaskValid (ConnectionCapabilities mask)
        {
            const ConnectionCapabilities max_caps = ConnectionCapabilities.DBusTransport |
                ConnectionCapabilities.SocketTransport |
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
            // property, but the IConvertible object returned by NDesk cannot be unboxed

            string protocol = conn.GetProtocol ();
            if (protocol != null && protocol.Equals ("jabber")) {
                capabilities_mask = ConnectionCapabilities.DBusTransport |
                ConnectionCapabilities.FileTransfer |
                ConnectionCapabilities.SocketTransport; 
            }
        }

        protected virtual void AdvertiseServices ()
        {
            IDictionary<string, object> [] caps;
            int service_count;
            
            lock (services) {
                service_count = services.Count;
                if (service_count == 0) {
                    return;
                }

                caps =  new Dictionary<string, object>[service_count];
                int counter = 0;

                foreach (ContactService service in services.Values) {
                    caps[counter] = new Dictionary<string, object> ();
    
                    if (service.Type == ContactServiceType.DBusTransport) {
                        caps[counter].Add ("org.freedesktop.Telepathy.Channel.ChannelType", Constants.CHANNEL_TYPE_DBUSTUBE);
                        caps[counter].Add (Constants.CHANNEL_TYPE_DBUSTUBE + ".ServiceName", service.Service);
                        
                        //Log.DebugFormat ("{0} adding service {1}", this.account_id, service.Service);
                    }
                    else if (service.Type == ContactServiceType.SocketTransport) {
                        caps[counter].Add ("org.freedesktop.Telepathy.Channel.ChannelType", Constants.CHANNEL_TYPE_STREAMTUBE);
                        caps[counter].Add (Constants.CHANNEL_TYPE_STREAMTUBE + ".Service", service.Service);
                    }

                    caps[counter].Add ("org.freedesktop.Telepathy.Channel.TargetHandleType", service.TargetHandleType);
                    counter++;
                }
            }

            //FIXME method does a 'replace,' so any caps previously set by other applications
            // will be wiped out! this method is really a helper for MC5 and should not be
            // used directly
            capabilities.SetSelfCapabilities (caps);
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
                
