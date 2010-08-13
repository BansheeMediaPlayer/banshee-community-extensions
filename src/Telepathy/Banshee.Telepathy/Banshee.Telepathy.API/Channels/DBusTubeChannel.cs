//
// DBusTubeChannel.cs
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

using Telepathy;

namespace Banshee.Telepathy.API.Channels
{
    internal sealed class DBusTubeChannel : RequestedChannel, Banshee.Telepathy.API.Channels.ITube
    {
        public event EventHandler <EventArgs> TubeOffered;

        public DBusTubeChannel (Connection conn,
                                string object_path,
                                uint initiator_handle,
                                uint target_handle,
                                string service_name) : base (conn, object_path, initiator_handle, target_handle)
        {
            Service = service_name;

            Initialize ();
        }

        private string address;
        public string Address {
            get { return address; }
        }

        private IDBusTube tube = null;
        internal IDBusTube ITube {
            get { return tube; }
        }

        private string service;
        public string Service {
            get { return service; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("service");
                }
                service = value;
            }
        }

        private void Initialize ()
        {
            tube.TubeChannelStateChanged += OnTubeChannelStateChanged;
            tube.Closed += OnTubeClosed;
        }

        protected override void SetProxyObject ()
        {
            tube = DBusUtility.GetProxy <IDBusTube> (Connection.BusName, ObjectPath);
        }

        public void Process ()
        {
            if (InitiatorHandle != Connection.SelfHandle) {
                //Log.Debug ("Raising event as tube has been offered");
                OnTubeOffered (EventArgs.Empty);
            }
            else {
                Offer ();
            }
        }
        public void Offer ()
        {
            address = tube.Offer (new Dictionary <string, object>(), SocketAccessControl.Localhost);
            Console.WriteLine ("Tube from {0} offered", address);
        }

        public void Accept ()
        {
            address = tube.Accept (SocketAccessControl.Localhost);
            Console.WriteLine ("Tube from {0} accepted", address);
        }

        private void OnTubeChannelStateChanged (TubeChannelState state)
        {
            Console.WriteLine ("OnTubeStateChanged: state {0}",
                               state);

            switch (state) {
                case TubeChannelState.Open:
                    OnChannelReady (EventArgs.Empty);
                    break;

                case TubeChannelState.NotOffered:
                    break;
            }

        }

        private void OnTubeClosed ()
        {
            IsClosed = true;
            OnClosed (EventArgs.Empty);
        }

        private void OnTubeOffered (EventArgs args)
        {
            EventHandler <EventArgs> handler = TubeOffered;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (tube != null) {
                    try {
                        tube.TubeChannelStateChanged -= OnTubeChannelStateChanged;
                        tube.Closed -= OnTubeClosed;
                    }
                    catch (Exception) {}
                }
            }

            base.Dispose (disposing);
        }

        public override void Close ()
        {
            try {
                tube.Close ();
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString());
            }
        }
    }
}