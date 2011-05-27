//
// ContactListChannel.cs
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

using Banshee.Telepathy.API.DBus;

using DBus;

using Telepathy;

namespace Banshee.Telepathy.API.Channels
{
    public class MembersAddedEventArgs : EventArgs
    {
        public MembersAddedEventArgs (uint [] added)
        {
            this.added = added;
        }

        private uint [] added;
        public uint [] Added {
            get { return added; }
        }

    }

    public class MembersRemovedEventArgs : EventArgs
    {
        public MembersRemovedEventArgs (uint [] removed)
        {
            this.removed = removed;
        }

        private uint [] removed;
        public uint [] Removed {
            get { return removed; }
        }

    }

    public class ContactListChannel : Channel
    {
        public event EventHandler <MembersAddedEventArgs>  MembersAdded;
        public event EventHandler <MembersRemovedEventArgs> MembersRemoved;

        public ContactListChannel (Connection conn, string target_id) : base (conn)
        {
            if (target_id == null) {
                throw new ArgumentNullException ("target_id");
            }

            this.target_id = target_id;
            ChannelType = Constants.CHANNEL_TYPE_CONTACTLIST;
        }

        private string target_id;
        public string TargetId {
            get { return target_id; }
            private set { target_id = value; }
        }

        private IGroup group = null;
        protected IGroup Group {
            get { return group; }
        }

        //private delegate void EnsureChannelCaller (IDictionary <string, object> specs, out bool yours, out ObjectPath path, out IDictionary <string, object> properties);
        public override void Request ()
        {
            IDictionary <string, object> channel_specs = new Dictionary <string, object> ();
            channel_specs.Add (Constants.CHANNEL_IFACE + ".ChannelType",
                               ChannelType);
            channel_specs.Add (Constants.CHANNEL_IFACE + ".TargetHandleType",
                               HandleType.List);
            channel_specs.Add (Constants.CHANNEL_IFACE + ".TargetID",
                               this.target_id);

            bool yours = false;
            ObjectPath object_path = null;

            Requests.EnsureChannel (channel_specs, out yours, out object_path, out channel_properties);
            this.ChannPath = object_path.ToString ();

            group = DBusUtility.GetProxy <IGroup> (Connection.Bus, Connection.BusName, ChannPath);
            group.MembersChanged += OnMembersChanged;

            OnChannelReady (EventArgs.Empty);

        }

        protected override void ProcessNewChannel (ChannelDetails c)
        {
        }

        public uint [] GetContacts ()
        {
            uint[] contacts; //, local_pending, remote_pending;
            contacts = group.Members;
            return contacts;
        }

        //TODO when a member is renamed, added[] and removed[] will contain one
        // handle each, as per spec. This may cause issues when contacts are
        // communitcating via channels. Wizardry may be needed.
        protected virtual void OnMembersChanged (string message, uint[] added,
                                       uint[] removed, uint[] local_pending,
                                       uint[] remote_pending, uint actor,
                                       ChannelGroupChangeReason reason)
        {
            if (added.Length > 0 && MembersAdded != null) {
                OnMembersAdded (new MembersAddedEventArgs (added));
            }

            if (removed.Length > 0 && MembersRemoved != null) {
                OnMembersRemoved (new MembersRemovedEventArgs (removed));
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (group != null) {
                    try {
                        group.MembersChanged -= OnMembersChanged;
                    }
                    catch (Exception) {}
                    group = null;
                }
            }

            base.Dispose (disposing);
        }

        protected virtual void OnMembersAdded (MembersAddedEventArgs args)
        {
            EventHandler <MembersAddedEventArgs> handler = MembersAdded;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnMembersRemoved (MembersRemovedEventArgs args)
        {
            EventHandler <MembersRemovedEventArgs> handler = MembersRemoved;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}
