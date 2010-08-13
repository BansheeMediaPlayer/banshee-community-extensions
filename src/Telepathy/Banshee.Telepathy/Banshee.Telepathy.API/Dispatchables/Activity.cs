//
// Activity.cs
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
using Banshee.Telepathy.API.Data;
using Data = Banshee.Telepathy.API.Data;

namespace Banshee.Telepathy.API.Dispatchables
{
    public enum ActivityState {
        Idle,
        RemotePending,
        LocalPending,
        Connected
    }

    public abstract class Activity : Dispatchable
    {
        //private Data.ChannelInfo channel;
        private Banshee.Telepathy.API.Channels.ITube tube;

        internal Activity (Contact c,  Banshee.Telepathy.API.Channels.ITube tube) : base (c, tube)
        {
            string service = tube.Service;
            Key = service;

//            if (c.HasService (service)) {
//                ContactService s = c.GetService (service);
//                this.service = s;
//            } else {
//                throw new InvalidOperationException (String.Format ("Contact does not support service {0}",
//                                                                    service));
//            }

            this.tube = tube;
        }

        public string Service {
            get { return tube.Service; }
        }

        private ActivityState state = ActivityState.Idle;
        public ActivityState State {
            get { return state; }
            protected set {
                if (value < ActivityState.Idle || value > ActivityState.Connected) {
                    throw new ArgumentOutOfRangeException ("state", "Not of type ActivityState");
                }
                state = value;
            }
        }

        public override string ToString ()
        {
            //return string.Format("[Activity: Type={0}, TargetHandleType={1}, Service={2}, Contact={3}, State={4}]", Type, TargetHandleType, Service, Contact, State);
            return Service;
        }

        internal protected override void Initialize ()
        {
            tube.ChannelReady += OnChannelReady;
            tube.Closed += OnTubeClosed;
            tube.TubeOffered += OnTubeOffered;
            state = ActivityState.RemotePending;
            tube.Process ();
        }

        protected override void Dispose (bool disposing)
        {
            if (IsDisposed) {
                return;
            }

            if (disposing) {
                Close ();

                if (tube != null) {
                    tube.ChannelReady -= OnChannelReady;
                    tube.Closed -= OnTubeClosed;
                    tube.TubeOffered -= OnTubeOffered;
                    tube.Dispose ();
                    tube = null;
                }
            }

            base.Dispose (disposing);
        }

        public void Start ()
        {
            if (state != ActivityState.Idle) {
                return;
            }

//            if (!Contact.HasService (service)) {
//                throw new InvalidOperationException (String.Format ("{0} does not support service {1}",
//                                                                    Contact.Name, service));
//            }

            tube.Offer ();
            state = ActivityState.RemotePending;
        }

        public void Accept ()
        {
            if (state != ActivityState.LocalPending) {
                throw new InvalidOperationException (String.Format ("Activity state is not in expected state of {0}",
                                                                    ActivityState.LocalPending));
            }

            tube.Accept ();
            state = ActivityState.RemotePending;
        }

        public void Close ()
        {
            Reject ();
        }

        public void Reject ()
        {
            if (tube != null) {
                tube.Close ();
            }
        }

        protected virtual void OnTubeClosed (object sender, EventArgs args)
        {
            Console.WriteLine ("{0} detected Tube closing", Contact.Name);
            state = ActivityState.Idle;
            OnClosed (EventArgs.Empty);
        }

        protected virtual void OnTubeOffered (object sender, EventArgs args)
        {
            state = ActivityState.LocalPending;
        }

    }
}