//
// StreamActivityListener.cs
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

using Banshee.Telepathy.API.Channels;

namespace Banshee.Telepathy.API.Dispatchables
{
    public class StreamActivityListener : Activity
    {
        private StreamTubeChannel tube;


        internal StreamActivityListener (Contact c, StreamTubeChannel tube) : base (c, tube)
        {
            this.tube = tube;
        }

        public object Address {
            get { return tube.ServerAddress; }
            set {
                if (State == ActivityState.Connected) {
                    throw new InvalidOperationException ("Address is already connected.");
                }
                tube.ServerAddress = value;
            }
        }

        private static bool AutoAccept {
            get; set;
        }

        protected new void Accept () {}
        protected new void Reject () {}

        protected override void OnChannelReady (object sender, EventArgs args)
        {
            Console.WriteLine ("{0} Connection to address {1}", Contact.Name, Address);

            State = ActivityState.Connected;
            OnReady (EventArgs.Empty);
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
        }
    }
}