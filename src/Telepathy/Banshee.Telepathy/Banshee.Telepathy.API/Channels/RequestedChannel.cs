//
// RequestedChannel.cs
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

namespace Banshee.Telepathy.API.Channels
{
    public abstract class RequestedChannel : Banshee.Telepathy.API.Channels.IChannel, IDisposable
    {
        public event EventHandler <EventArgs> Closed;
        public event EventHandler <EventArgs> ChannelReady;

        private RequestedChannel ()
        {
        }

        public RequestedChannel (Connection conn,
                                 string object_path,
                                 uint initiator_handle,
                                 uint target_handle)
        {
            this.conn = conn;
            ObjectPath = object_path;
            InitiatorHandle = initiator_handle;
            TargetHandle = target_handle;

            SetProxyObject ();
        }

        private Connection conn;
        protected Connection Connection {
            get { return conn; }
        }

        private uint initiator_handle;
        public uint InitiatorHandle {
            get { return initiator_handle; }
            protected set {
                if (value == 0) {
                    throw new ArgumentException ("InitiatorHandle must be > 0.");
                }

                initiator_handle = value;
            }
        }

        private uint target_handle;
        public uint TargetHandle {
            get { return target_handle; }
            protected set {
                if (value == 0) {
                    throw new ArgumentException ("TargetHandle must be > 0.");
                }

                target_handle = value;
            }
        }

        private string object_path;
        public string ObjectPath {
            get { return object_path; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("object_path");
                }
                object_path = value;
            }
        }

        private bool is_closed = false;
        public bool IsClosed {
            get { return is_closed; }
            protected set { is_closed = value; }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                if (!is_closed) {
                    Close ();
                }
            }
        }

        protected abstract void SetProxyObject ();
        public abstract void Close ();

        protected virtual void OnClosed (EventArgs args)
        {
            EventHandler <EventArgs> handler = Closed;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnChannelReady (EventArgs args)
        {
            EventHandler <EventArgs> handler = ChannelReady;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}
