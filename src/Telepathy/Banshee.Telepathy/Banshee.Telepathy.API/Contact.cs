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

//using Data = Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.DBus;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.API
{
     public class Contact : IDisposable
    {
        private readonly ChannelInfoCollection supported_channels = new ChannelInfoCollection ();

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

        public ChannelInfoCollection SupportedChannels {
            get { return supported_channels; }
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

            if (Status > ConnectionPresenceType.Offline && Status < ConnectionPresenceType.Unknown) {
                uint [] handle = { Handle };

                IDictionary <uint, RequestableChannelClass []> dictionary =
                    capabilities.GetContactCapabilities (handle);

                if (dictionary.ContainsKey (Handle)) {
                    //Log.DebugFormat ("{0} Loading services on Contact.Initialize()", this.Name);
                    LoadSupportedChannels (dictionary [Handle]);
                }
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

        protected void LoadSupportedChannels (RequestableChannelClass[] classes)
        {
            LoadSupportedChannels (classes, true);
        }

        protected virtual void LoadSupportedChannels (RequestableChannelClass [] classes, bool clear)
        {
            string service;
            string content_type;
            string description;
            HandleType target_type;

            lock (supported_channels) {
                if (clear) {
                    supported_channels.Clear ();
                }

                foreach (RequestableChannelClass c in classes) {
                    if (c.FixedProperties.ContainsKey (Constants.CHANNEL_TYPE_DBUSTUBE + ".ServiceName")) {
                         service = (string) c.FixedProperties[Constants.CHANNEL_TYPE_DBUSTUBE + ".ServiceName"];
                         target_type = (HandleType) c.FixedProperties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                         supported_channels.Add (new DBusTubeChannelInfo (ChannelType.DBusTube, target_type, service));

                        //Log.DebugFormat ("Contact {0} has {1}", this.Name, service);
                    }

                    else if (c.FixedProperties.ContainsKey (Constants.CHANNEL_TYPE_STREAMTUBE + ".Service")) {
                        service = (string) c.FixedProperties[Constants.CHANNEL_TYPE_STREAMTUBE + ".Service"];
                        target_type = (HandleType) c.FixedProperties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                        supported_channels.Add (new StreamTubeChannelInfo (ChannelType.StreamTube, target_type, service, null));
                    }

                    else if (c.FixedProperties.ContainsKey (Constants.CHANNEL_TYPE_FILETRANSFER + ".ContentType")) {
                        content_type = (string) c.FixedProperties[Constants.CHANNEL_TYPE_FILETRANSFER + ".ContentType"];
                        description = (string) c.FixedProperties[Constants.CHANNEL_TYPE_FILETRANSFER + ".Description"];
                        target_type = (HandleType) c.FixedProperties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                        supported_channels.Add (new FileTransferChannelInfo (ChannelType.StreamTube, target_type, content_type, description));
                    }

                    //Log.Debug ((string) c.FixedProperties[Constants.CHANNEL_IFACE + ".ChannelType"]);
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
            if (caps.ContainsKey (handle)) {
                LoadSupportedChannels (caps[Handle]);
            }
        }
    }
}
