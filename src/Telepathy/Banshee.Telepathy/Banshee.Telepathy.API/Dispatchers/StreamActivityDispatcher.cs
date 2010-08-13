//
// StreamActivityDispatcher.cs
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

using Banshee.Telepathy.API.Data;
//using Data = Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.Channels;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.API.Dispatchers
{
    internal sealed class StreamActivityDispatcher : Dispatcher
    {
        internal StreamActivityDispatcher (Connection conn) : base (conn,
                                                                    Constants.CHANNEL_TYPE_STREAMTUBE,
                                                                    new string [] { "Service" })
        {
            DispatchObject = typeof (StreamActivityListener);
            PropertyKeysForDispatching = new string [] { "Service" };
        }

        protected override bool VerifyRequest (uint target_handle, IDictionary <string, object> properties)
        {
            string service_name = null;

            if (base.VerifyRequest (target_handle, properties)) {
                service_name = (string) properties["Service"];

                Contact contact = Connection.Roster.GetContact (target_handle);
                if (contact.SupportedChannels.GetChannelInfo <StreamTubeChannelInfo> (service_name) == null) {
                    throw new InvalidOperationException (String.Format ("Contact does not support service {0}",
                                                                        service_name));
                }

                return true;
            }

            return false;
        }

        protected override bool CanProcess (ChannelDetails details)
        {
            foreach (StreamTubeChannelInfo info in Connection.SupportedChannels.GetAll<StreamTubeChannelInfo> ()) {
                if (info.Service.Equals (details.Properties[Constants.CHANNEL_TYPE_STREAMTUBE + ".Service"])) {
                    return true;
                }
            }

            return false;
        }

        protected override void ProcessNewChannel (string object_path,
                                                   uint initiator_handle,
                                                   uint target_handle,
                                                   ChannelDetails c)
        {
            string service_name = (string) c.Properties[Constants.CHANNEL_TYPE_STREAMTUBE + ".Service"];
            Contact contact = Connection.Roster.GetContact (target_handle);

            StreamTubeChannel tube = null;
            Activity activity = null;

            try {
                tube = new StreamTubeChannel (this.Connection,
                                          object_path,
                                          initiator_handle,
                                          target_handle,
                                          service_name);

                if (initiator_handle == Connection.SelfHandle) {
                    tube.ServerAddress = Connection.SupportedChannels.GetChannelInfo <StreamTubeChannelInfo> (service_name).Address;
                    activity = new StreamActivityListener (contact, tube);
                }
                else {
                    activity = new StreamActivity (contact, tube);
                }

                if (activity != null) {
                    DispatchManager dm = Connection.DispatchManager;
                    dm.Add (contact, activity.Service, activity);
                }
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());

                if (activity != null) {
                    activity.Dispose ();
                } else if (tube != null) {
                    tube.Dispose ();
                }
            }
        }
    }
}