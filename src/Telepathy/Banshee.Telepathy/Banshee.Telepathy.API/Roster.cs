//
// Roster.cs
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

using Banshee.Telepathy.API.Channels;
using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API
{
    public enum RosterState {
        Unloaded = 0,
        Loading = 1,
        Loaded = 2
    };

    public enum ContactMembership {
        Added = 0,
        Removed = 1
    };

    public class RosterEventArgs : EventArgs
    {
        private RosterState action;

        public RosterEventArgs (RosterState action)
        {
            this.action = action;
        }

        public RosterState Action {
            get { return action; }
        }
    }

    public class ContactStatusEventArgs : EventArgs
    {
        private ConnectionPresenceType action;

        public ContactStatusEventArgs (ConnectionPresenceType action)
        {
            this.action = action;
        }

        public ConnectionPresenceType Action {
            get { return action; }
        }
    }

    public class ContactMembershipEventArgs : EventArgs
    {
        private ContactMembership action;

        public ContactMembershipEventArgs (ContactMembership action)
        {
            this.action = action;
        }

        public ContactMembership Action {
            get { return action; }
        }
     }

    public class Roster : IDisposable
    {
        private readonly IDictionary <uint, Contact> roster = new Dictionary <uint, Contact> ();
        private IConnection iconn_proxy;

        public event EventHandler <ContactStatusEventArgs> ContactStatusChanged;
        public event EventHandler <ContactMembershipEventArgs> ContactMembershipChanged;
        public event EventHandler <RosterEventArgs> RosterStateChanged;

        protected Roster ()
        {
            state = RosterState.Unloaded;
        }

        protected internal Roster (Connection conn) : this ()
        {
            this.Connection = conn;
            iconn_proxy = conn.DBusProxy;
        }

        private Connection conn = null;
        public Connection Connection {
            get { return conn; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("conn");
                }
                conn = value;
            }
        }

        public int Count {
            get { return roster.Count; }
        }

        private RosterState state;
        public RosterState State {
            get {
                return state;
                }
            protected set {
                if (value < RosterState.Unloaded || value > RosterState.Loaded) {
                    throw new ArgumentOutOfRangeException ("state");
                }
                state = value;
            }
        }

        private ISimplePresence presence = null;
        protected internal ISimplePresence ISimplePresence {
            get { return presence; }
        }

        private ContactListChannel contact_list = null;
        protected internal ContactListChannel ContactListChannel {
            get { return contact_list; }
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

            if (contact_list != null) {
                contact_list.ChannelReady -= OnContactListChannelReady;
                contact_list.MembersAdded -= OnMembersAdded;
                contact_list.MembersRemoved -= OnMembersRemoved;
                contact_list.Dispose ();
                contact_list = null;
            }

            if (presence != null) {
                try {
                    presence.PresencesChanged -= OnPresencesChanged;
                }
                catch (Exception) {}
                presence = null;
            }

            lock (roster) {
                foreach (KeyValuePair <uint, Contact> kv in roster) {
                    kv.Value.Dispose ();
                }
            }

            roster.Clear ();
            conn = null;
        }

        public IEnumerable <Contact> GetAllContacts ()
        {
            lock (roster) {
                foreach (KeyValuePair <uint, Contact> kv in roster) {
                    yield return kv.Value;
                }
            }
        }

        public void Load ()
        {
            if (state != RosterState.Unloaded) {
                return;
            }

            state = RosterState.Loading;

            contact_list = new ContactListChannel (this.Connection, "subscribe");
            contact_list.ChannelReady += OnContactListChannelReady;
            contact_list.MembersAdded += OnMembersAdded;
            contact_list.MembersRemoved += OnMembersRemoved;
            contact_list.Request ();
        }

        public Contact GetContact (uint key)
        {
            lock (roster) {
                if (roster.ContainsKey (key)) {
                    return roster[key];
                }
            }

            return null;

        }

        protected Contact CreateContact (string member_name,
                                                 uint handle,
                                                 ConnectionPresenceType status)
        {
            return CreateContact (member_name, handle, status, null);
        }

        protected virtual Contact CreateContact (string member_name,
                                                 uint handle,
                                                 ConnectionPresenceType status,
                                                 string status_message)
        {
            Contact c = new Contact (this, member_name,
                                                 handle,
                                                 status);
            if (status_message != null) {
                c.StatusMessage = status_message;
            }

            return c;
        }

        private void GetPresenceInfo (uint[] contacts)
        {
            IDictionary<uint,SimplePresence> presence_info = new Dictionary<uint,SimplePresence>();
            presence_info = presence.GetPresences (contacts);

            GetPresenceInfo (contacts, presence_info, false);
        }

        private void GetPresenceInfo (uint [] contacts, IDictionary <uint, SimplePresence> presence_info, bool raise_events)
        {
            string[] member_names;

            try {
                member_names = iconn_proxy.InspectHandles (HandleType.Contact, contacts);
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());
                return;
            }

            for (int i = 0; i < contacts.Length; i++) {
                uint handle = contacts[i];
                if (presence_info.ContainsKey(handle)) {
                    Console.WriteLine ("Presence change for {0} with handle {1} is {2}",
                                     member_names[i],
                                     handle,
                                     presence_info[handle].Status);

                    lock (roster) {
                        if (!roster.ContainsKey (handle)) {
                            Contact c = CreateContact (member_names[i],
                                        handle,
                                        presence_info[handle].Type,
                                        presence_info[handle].StatusMessage);
                            roster.Add (handle, c);
                            c.Initialize ();
                        }
                        else {
                            roster[handle].Update (member_names[i],
                                                        handle,
                                                        presence_info[handle].Type);
                            roster[handle].StatusMessage = presence_info[handle].StatusMessage;
                        }
                    }

                    if (raise_events) {
                        OnContactStatusChanged (roster[handle],
                                                new ContactStatusEventArgs (presence_info[handle].Type));
                    }
                }
            }

        }

        private void OnPresencesChanged (IDictionary <uint,SimplePresence> presences_changed)
        {
            uint[] handles = new uint[presences_changed.Keys.Count];
            presences_changed.Keys.CopyTo(handles, 0);

            GetPresenceInfo (handles, presences_changed, true);
        }

        protected virtual void OnContactStatusChanged (object o, ContactStatusEventArgs args)
        {
            EventHandler <ContactStatusEventArgs> handler = ContactStatusChanged;
            if (handler != null) {
                handler (o, args);
            }
        }

        protected virtual void OnRosterStateChanged (RosterEventArgs args)
        {
            EventHandler <RosterEventArgs> handler = RosterStateChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnContactListChannelReady (object sender, EventArgs args)
        {
            uint [] contacts = contact_list.GetContacts ();
            //Log.DebugFormat ("Account {0} has {1} contacts", conn.AccountId, contacts.Length);

            presence = DBusUtility.GetProxy <ISimplePresence> (conn.Bus, conn.BusName, conn.ObjectPath);
            GetPresenceInfo (contacts);
            presence.PresencesChanged += OnPresencesChanged;

            state = RosterState.Loaded;
            OnRosterStateChanged (new RosterEventArgs (state));
        }

        protected virtual void OnContactMembershipChanged (object o, ContactMembershipEventArgs args)
        {
            EventHandler <ContactMembershipEventArgs> handler = ContactMembershipChanged;
            if (handler != null) {
                handler (o, args);
            }
        }

        private void OnMembersAdded (object sender, MembersAddedEventArgs args)
        {
            GetPresenceInfo (args.Added);

            foreach (uint handle in args.Added) {
               OnContactMembershipChanged (roster[handle],
                                          new ContactMembershipEventArgs (ContactMembership.Added));
            }
        }

        private void OnMembersRemoved (object sender, MembersRemovedEventArgs args)
        {
            foreach (uint handle in args.Removed) {
                OnContactMembershipChanged (roster[handle],
                                          new ContactMembershipEventArgs (ContactMembership.Removed));

                lock (roster) {
                    roster.Remove (handle);
                }
            }
        }

    }
}
