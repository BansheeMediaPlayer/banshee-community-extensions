//
// Dispatchable.cs
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

using Banshee.Telepathy.API.Channels;

namespace Banshee.Telepathy.API.Dispatchables
{
    public enum ErrorReason {
        Duplicate,
        Unsupported,
        Unknown
    };

    public class ErrorEventArgs : EventArgs
    {
        private ErrorReason reason;

        public ErrorEventArgs (ErrorReason reason)
        {
            this.reason = reason;
        }

        public ErrorReason Reason {
            get { return reason; }
        }
    }

    public abstract class Dispatchable : IDisposable
    {
        public event EventHandler <EventArgs> ResponseRequired;
        public event EventHandler <EventArgs> Ready;
        public event EventHandler <EventArgs> Closed;
        public event EventHandler <ErrorEventArgs> Error;

        public event EventHandler <EventArgs> Disposing;

        private Dispatchable ()
        {
        }

        internal Dispatchable (Contact contact, IChannel channel)
        {
            Contact = contact;
            this.channel = channel;
        }

        private Contact contact;
        public Contact Contact {
            get { return contact; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("contact");
                }
                contact = value;
            }
        }

        private IChannel channel;
        internal IChannel Channel {
            get { return channel; }
            set { channel = value; }
        }

        private bool disposed = false;
        protected bool IsDisposed {
            get { return disposed; }
        }

        private bool is_closed = false;
        public bool IsClosed {
            get { return is_closed; }
            protected set { is_closed = value; }
        }

        private object key = null;
        internal protected object Key {
            get { return key; }
            protected set { key = value; }
        }

        public uint InitiatorHandle {
            get {
                if (channel != null) {
                    return channel.InitiatorHandle;
                }
                return 0;
            }
        }

        public bool IsSelfInitiated {
            get {
                if (channel != null && Contact != null) {
                    return channel.InitiatorHandle == Contact.Connection.SelfHandle;
                }
                return false;
            }
        }

        private static bool auto_remove = true;
        public bool AutoRemoveOnClose {
            get { return auto_remove; }
            set { auto_remove = value; }
        }

        public static int Count <T> (Connection conn) where T : Dispatchable
        {
            if (conn == null) {
                throw new ArgumentNullException ("conn");
            }

            int count = 0;

            foreach (Contact contact in conn.Roster.GetAllContacts ()) {
                DispatchManager dm = contact.DispatchManager;
                foreach (T obj in dm.GetAll <T> (contact)) {
                    if (obj != null) {
                        count++;
                    }
                }
            }

            return count;
        }

        public override bool Equals (object obj)
        {
            Dispatchable d = obj as Dispatchable;
            if (d == null) {
                return false;
            }
            else if (d.Contact == null) {
                return false;
            }

            //Console.WriteLine (String.Format ("{0} {1} {2}", d.Contact.Handle, d.Key.ToString (), d.GetType ().ToString ()));
            //Console.WriteLine (String.Format ("{0} {1} {2}", Contact.Handle, Key.ToString (), GetType ().ToString ()));
            return d.Contact.Equals (Contact) && d.Key.Equals (Key) && d.GetType ().Equals (this.GetType ());
        }

        public override int GetHashCode ()
        {
            if (Contact == null) {
                return base.GetHashCode ();
            }

            return Contact.GetHashCode () + Key.GetHashCode () + GetType ().GetHashCode ();
        }

        internal protected abstract void Initialize ();

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposed) {
                return;
            }

            if (disposing) {
                OnDisposing (EventArgs.Empty);

                disposed = true;
            }
        }

        protected abstract void OnChannelReady (object sender, EventArgs args);

        protected virtual void OnResponseRequired (EventArgs args)
        {
            EventHandler <EventArgs> handler = ResponseRequired;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnReady (EventArgs args)
        {
            EventHandler <EventArgs> handler = Ready;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnDisposing (EventArgs args)
        {
            EventHandler <EventArgs> handler = Disposing;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnClosed (EventArgs args)
        {
            IsClosed = true;

            EventHandler <EventArgs> handler = Closed;
            if (handler != null) {
                handler (this, args);
            }

            if (!disposed && key != null && AutoRemoveOnClose && Contact != null) {
                DispatchManager dm = contact.DispatchManager;
                dm.Remove (contact, key, this.GetType ());
            }
        }

        protected virtual void OnError (ErrorEventArgs args)
        {
            EventHandler <ErrorEventArgs> handler = Error;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}