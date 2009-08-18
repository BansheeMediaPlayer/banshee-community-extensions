//
// Contact.cs
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
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.API
{
     public class Contact : IDisposable
    {
        private readonly IDictionary <string, ContactService> services = new Dictionary <string, ContactService> ();
        
        public event EventHandler <ContactStatusEventArgs>  ContactUpdated;
        public event EventHandler <EventArgs> ContactServicesChanged;
                
        protected Contact ()
        {
             //Initialize ();
        }

        protected internal Contact (Roster roster, string member_name, 
                                    uint handle, ConnectionPresenceType status) : this ()
        {
            this.Roster = roster;
            this.Name = member_name;
            this.Handle = handle;
            this.Status = status;
        }

        public Connection Connection {
            get {
                if (roster != null) {
                    return roster.Connection; 
                }
                return null;
            }
        }

        public DispatchManager DispatchManager {
            get {
                if (Connection != null) {
                    return Connection.DispatchManager; 
                }
                return null;
            }
        }
        
        public string AccountId {
            get {
                if (Connection != null) {
                    return Connection.AccountId; 
                }
                return String.Empty;
            }
        }

        private string member_name;
        public string Name {
            get { return member_name; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("member_name");
                }
                member_name = value; 
            }
        }

        private uint handle;
        public uint Handle {
            get { return handle; }
            protected set { 
                if (value == 0) {
                   throw new ArgumentException ("Cannot be zero", "handle");
                }
                handle = value; 
            }
        }

        private IContactCapabilities capabilities = null;
        internal IContactCapabilities ContactCapabilities {
            get { return capabilities; }
            private set { capabilities = value; }
        }

        private ConnectionPresenceType status;
        public ConnectionPresenceType Status {
            get { return status; }
            protected set { 
                if (value < ConnectionPresenceType.Unset || value > ConnectionPresenceType.Error) {
                    throw new ArgumentOutOfRangeException ("status");
                }
                status = value; 
            }
        }

        private string status_message = null;
        public string StatusMessage {
            get { return status_message; }
            internal protected set {
                if (value == null) {
                    throw new ArgumentNullException ("status_message");
                }
                status_message = value;
            }
        }

        private Roster roster;
        public Roster Roster {
            get { return roster; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("roster");
                }
                roster = value;
            }
        }

        private Avatar avatar;
        public Avatar Avatar {
            get { return avatar; }
            protected set { avatar = value; }
        }
        
        public override string ToString ()
        {
            return String.Format ("{0}:{1}", roster.Connection.AccountId, member_name);
        }

        public override bool Equals (object obj)
        {
            Contact contact = obj as Contact;
            if (contact == null) {
                throw new ArgumentNullException ("obj");
            }
            
            return contact.AccountId.Equals (AccountId) && contact.Handle == Handle;
        }

        public override int GetHashCode ()
        {
            return AccountId.GetHashCode () + handle.GetHashCode ();
        }

        protected internal virtual void Initialize ()
        {
            capabilities = Connection.ContactCapabilities;
            capabilities.ContactCapabilitiesChanged += OnContactCapabilitiesChanged;
            
            uint [] handle = { Handle };

            IDictionary <uint, RequestableChannelClass []> dictionary = 
                capabilities.GetContactCapabilities (handle);

            if (dictionary.ContainsKey (Handle)) {
                //Log.DebugFormat ("{0} Loading services on Contact.Initialize()", this.Name);
                LoadServices (dictionary [Handle]);
            }

            avatar = new Avatar (this);
        }

        public void Dispose ()
        {
            Dispose (true);
        }
        
        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                if (capabilities != null) {
                    try {
                        capabilities.ContactCapabilitiesChanged -= OnContactCapabilitiesChanged;
                    }
                    catch (Exception) {}
                    capabilities = null;
                }

                if (avatar != null) {
                    avatar.Dispose ();
                    avatar = null;
                }
                
                // this happens in Connection class
                //DispatchManager.RemoveAll (this);
                                
                roster = null;
            }
        }
        
        protected internal void Update (string member_name, 
                                        uint handle,
                                        ConnectionPresenceType status)
        {
            Update (member_name, handle, status, String.Empty);
        }

        protected internal virtual void Update (string member_name, 
                                        uint handle,
                                        ConnectionPresenceType status,
                                        string status_message)
        {
            this.Name = member_name;
            this.Handle = handle;
            this.Status = status;
            this.StatusMessage = status_message;

            OnContactUpdated (new ContactStatusEventArgs (status));
        }

        public bool HasService (ContactService service)
        {
            return HasService (service.Service);
        }
        
        public bool HasService (string service)
        {
            lock (services) {
                return services.ContainsKey (service);
            }
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

        protected void LoadServices (RequestableChannelClass[] classes)
        {
            LoadServices (classes, true);
        }
        
        protected virtual void LoadServices (RequestableChannelClass [] classes, bool clear)
        {
            string service;
            HandleType target_type;

            lock (services) {
                if (clear) {
                    services.Clear ();
                }
        
                foreach (RequestableChannelClass c in classes) {
                    if (c.FixedProperties.ContainsKey (Constants.CHANNEL_TYPE_DBUSTUBE + ".ServiceName")) {
                         service = (string) c.FixedProperties[Constants.CHANNEL_TYPE_DBUSTUBE + ".ServiceName"];
                         target_type = (HandleType) c.FixedProperties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                         services.Add (service, new ContactService (ContactServiceType.DBusTransport,
                                       target_type,
                                       service));
                        
                        //Log.DebugFormat ("Contact {0} has {1}", this.Name, service);
                    }
                     
                    else if (c.FixedProperties.ContainsKey (Constants.CHANNEL_TYPE_STREAMTUBE + ".Service")) {
                        service = (string) c.FixedProperties[Constants.CHANNEL_TYPE_STREAMTUBE + ".Service"];
                        target_type = (HandleType) c.FixedProperties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                        services.Add (service, new ContactService (ContactServiceType.SocketTransport,
                                                                   target_type,
                                                                   service));
                    }
                    
                    //Log.Debug ((string) c.FixedProperties[Constants.CHANNEL_IFACE + ".ChannelType"]);
                }

                foreach (DBusActivity activity in DispatchManager.GetAll <DBusActivity> (this)) {
                    if (activity.IsSelfInitiated && !HasService (activity.Service)) {
                        activity.Close ();
                    }
                }

                foreach (StreamActivityListener activity in DispatchManager.GetAll <StreamActivityListener> (this)) {
                    if (activity.IsSelfInitiated && !HasService (activity.Service)) {
                        activity.Close ();
                    }
                }
            }
            
            OnContactServicesChanged (EventArgs.Empty);
        }
        
        protected virtual void OnContactUpdated (ContactStatusEventArgs args)
        {
            EventHandler <ContactStatusEventArgs> handler = ContactUpdated;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnContactServicesChanged (EventArgs args)
        {
            EventHandler <EventArgs> handler = ContactServicesChanged;
            if (handler != null) {
                handler (this, args);
            }
        }
        
        private void OnContactCapabilitiesChanged (IDictionary <uint,RequestableChannelClass[]> caps)
        {
            //Log.DebugFormat ("OnContactCapabilitiesChanged: {0}", this.Name);
            if (caps.ContainsKey (handle)) {
                LoadServices (caps[Handle]);
            }
        }
    }
}
