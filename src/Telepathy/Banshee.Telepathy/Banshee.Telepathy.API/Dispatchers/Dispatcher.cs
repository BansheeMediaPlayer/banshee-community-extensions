//
// Dispatcher.cs
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

using Telepathy;

using Banshee.Telepathy.API.DBus;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.API.Dispatchers
{
    internal abstract class Dispatcher : IDisposable
    {
        protected IDictionary<string, object> channel_properties;

        private Dispatcher ()
        {
        }

        protected Dispatcher (Connection conn, string channel_type, string [] property_keys)
        {
            this.conn = conn;
            ChannelType = channel_type;
            this.property_keys = property_keys;

            InitializeRequestsInterface ();
        }

        private string [] property_keys;
        public string [] PropertyKeys {
            get { return property_keys; }
            protected set { value = property_keys; }
        }

        private string [] dispatch_keys;
        public string [] PropertyKeysForDispatching {
            get { return dispatch_keys; }
            protected set { dispatch_keys = value; }
        }

        public uint SelfHandle {
            get { return conn.SelfHandle; }
        }

        private Connection conn;
        protected Connection Connection {
            get { return conn; }
            set {
                if (conn == null) {
                   throw new ArgumentNullException ("conn");
                }
                conn = value;
            }
        }

        private IRequests requests = null;
        protected IRequests Requests {
            get { return requests; }
        }

        private Type dispatch_object = typeof (Dispatchable);
        public Type DispatchObject {
            get { return dispatch_object; }
            protected set {
                if (!value.IsSubclassOf (typeof (Dispatchable))) {
                    throw new ArgumentException ("Invalid type");
                }
                dispatch_object = value;
            }
        }

        private string channel_type;
        public string ChannelType {
            get { return channel_type; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("channel_type");
                }
                channel_type = value;
            }
        }

        public void Request (uint target_handle, HandleType handle_type, IDictionary <string, object> properties)
        {
            Request (target_handle, handle_type, properties, true);
        }

        public virtual void Request (uint target_handle,
                                     HandleType handle_type,
                                     IDictionary <string, object> properties,
                                     bool replace)
        {
            IDictionary <string, object> channel_specs = new Dictionary <string, object> ();
            channel_specs.Add (Constants.CHANNEL_IFACE + ".ChannelType",
                               ChannelType);
            channel_specs.Add (Constants.CHANNEL_IFACE + ".TargetHandleType",
                               handle_type);
            channel_specs.Add (Constants.CHANNEL_IFACE + ".TargetHandle",
                               target_handle);

            if (VerifyRequest (target_handle, properties)) {

                if (replace) {
                    CleanUpIfClosed (target_handle, GetDispatchingKey (properties));
                }

                CopyRequestProperties (properties, channel_specs);

                ObjectPath object_path;
                Requests.CreateChannel (channel_specs, out object_path, out channel_properties);
            }
        }

        protected virtual bool VerifyRequest (uint target_handle, IDictionary <string, object> properties)
        {
            if (target_handle < 1) {
                throw new ArgumentOutOfRangeException ("target_handle");
            }
            else if (property_keys != null && properties == null) {
                throw new ArgumentNullException ("properties");
            }
            else if (property_keys == null) {
                return true;
            }

            foreach (string key in property_keys) {
                if (!properties.ContainsKey (key)) {
                    throw new ArgumentException (String.Format ("No {0} property found.", key));
                }
            }

            return true;
        }

        protected void CopyRequestProperties (IDictionary <string, object> properties, IDictionary <string, object> channel_specs)
        {
            if (property_keys != null && properties == null) {
                throw new ArgumentNullException ("properties");
            }

            foreach (string key in property_keys) {
                if (properties.ContainsKey (key)) {
                    channel_specs.Add (ChannelType + "." + key, properties[key]);
                }
                else {
                    throw new InvalidOperationException (String.Format ("{0} not found", key));
                }
            }
        }

        // TODO - this needs work, but not really important
        private object GetDispatchingKey (IDictionary <string, object> properties)
        {
            return dispatch_keys != null ? properties[dispatch_keys[0]] : null;
        }

        private void CleanUpIfClosed (uint target_handle, object key)
        {
            if (target_handle < 1) {
                throw new ArgumentException ("target_handle should be > 0");
            }
            else if (key == null) {
                throw new ArgumentNullException ("key");
            }

            Contact contact = Connection.Roster.GetContact (target_handle);
            DispatchManager dm = contact.DispatchManager;

            Dispatchable d = dm.Get (contact, key, DispatchObject);
            if (d != null) {
                if (d.IsClosed) {
                    dm.Remove (contact, key, DispatchObject);
                }
                else {
                    throw new InvalidOperationException (String.Format ("{0} already has dispatchable object type {1} with key {2}",
                                                                        contact.Name, DispatchObject.FullName, key)
                                                         );
                }
            }
        }

        protected void InitializeRequestsInterface ()
        {
            requests = conn.Requests;
            requests.NewChannels += OnNewChannels;
        }

        protected abstract void ProcessNewChannel (string object_path,
                                                   uint initiator_handle,
                                                   uint target_handle,
                                                   ChannelDetails c);

        protected abstract bool CanProcess (ChannelDetails details);

        protected void OnNewChannels (ChannelDetails[] channels)
        {
            foreach (ChannelDetails c in channels) {

                string object_path = c.Channel.ToString ();
                string channel_type = (string) c.Properties[Constants.CHANNEL_IFACE + ".ChannelType"];
                HandleType handle_type = (HandleType) c.Properties[Constants.CHANNEL_IFACE + ".TargetHandleType"];
                uint target_handle = (uint) c.Properties[Constants.CHANNEL_IFACE + ".TargetHandle"];

                if (channel_type.Equals (ChannelType) && CanProcess (c)) {
                    Console.WriteLine ("NewChannel detected: object path {0}, channel_type {1}, handle_type {2}, target_handle {3}",
                                   object_path, channel_type, handle_type.ToString (), target_handle);

                    uint initiator_handle = (uint) DBusUtility.GetProperty (Connection.Bus, Connection.BusName, object_path,
                                                                     Constants.CHANNEL_IFACE, "InitiatorHandle");
                    ProcessNewChannel (object_path, initiator_handle, target_handle, c);
                    return;
                }
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
                requests.NewChannels -= OnNewChannels;
                requests = null;
            }
        }
    }
}
