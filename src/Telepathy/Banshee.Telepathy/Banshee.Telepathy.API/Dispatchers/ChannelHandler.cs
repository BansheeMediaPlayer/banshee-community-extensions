//
// ChannelHandler.cs
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
using org.freedesktop.DBus;

using Telepathy;
using Telepathy.Client;

namespace Banshee.Telepathy.API.DBus
{
    // see http://telepathy.freedesktop.org/spec/org.freedesktop.Telepathy.Client.Handler.html
    internal class ChannelHandler : IHandler, Properties
    {
        private const string BASE_BUS = "org.freedesktop.Telepathy.Client";
        private static ChannelHandler instance = null;
        private static int connection_count = 0;

        private ChannelHandler ()
        {
        }

        private ChannelHandler (string client_name, IDictionary<string, object>[] channels)
        {
            ClientName = client_name;
            HandlerChannelFilter = channels;
        }

        private static void Register (ChannelHandler instance)
        {
            DBusUtility.Register (BusType.Session, instance.BusName, instance.ObjectPath, instance);
        }

        private static void Unregister ()
        {
            DBusUtility.Unregister (BusType.Session, instance.ObjectPath);
        }

        public static ChannelHandler Create (string client_name, IDictionary<string, object>[] channels)
        {
            if (instance == null) {
                instance = new ChannelHandler (client_name, channels);
                Register (instance);
            }

            connection_count++;
            return instance;
        }

        public static void Destroy ()
        {
            connection_count--;

            if (connection_count == 0) {
                Unregister ();
                instance = null;
            }
        }

        private string client_name;
        public string ClientName {
            get { return client_name; }
            set {
                if (value == null) {
                    throw new ArgumentNullException ("client_name");
                }
                client_name = value;
            }
        }

        public string BusName {
            get { return BASE_BUS + "." + ClientName; }
        }

        public string ObjectPath {
            get { return "/" + BusName.Replace (".", "/"); }
        }

        public string[] Interfaces {
            get { return new string[] { BASE_BUS + ".Handler" }; }
        }

        private IDictionary<string, object>[] channel_filter;
        public IDictionary<string,object>[] HandlerChannelFilter {
            get { return channel_filter; }
            private set { channel_filter = value; }
        }

        public bool BypassApproval {
            get { return true; }
        }

        //IDictionary<string, object>[] channels;
        public ObjectPath[] HandledChannels {
            get { return new ObjectPath[] {}; }
        }

        public string[] Capabilities {
            get { return new string[] {}; }
        }

        public IDictionary <string, object> GetAll (string iface)
        {
            IDictionary <string, object> all = new Dictionary <string, object> ();

            if (iface.Equals (BASE_BUS)) {
                all.Add ("Interfaces", Interfaces);
            }
            else if (iface.Equals (BASE_BUS + ".Handler")) {
                all.Add ("HandlerChannelFilter", HandlerChannelFilter);
                all.Add ("BypassApproval", BypassApproval);
                all.Add ("HandledChannels", HandledChannels);
                all.Add ("Capabilities", Capabilities);
            }

            return all;
        }

        public void Set (string iface, string property, object value)
        {
        }

        public object Get (string iface, string property)
        {
            if (property.Equals ("Interfaces")) {
                return Interfaces;
            }
            else if (property.Equals ("HandlerChannelFilter")) {
                return HandlerChannelFilter;
            }
            else if (property.Equals ("BypassApproval")) {
                return BypassApproval;
            }
            else if (property.Equals ("HandledChannels")) {
                return HandledChannels;
            }
            else if (property.Equals ("Capabilities")) {
                return Capabilities;
            }
            else {
                return null;
            }
        }

        public void HandleChannels (ObjectPath account,
                                    ObjectPath connection,
                                    ChannelDetails[] channels,
                                    ObjectPath[] requests_satisfied,
                                    ulong user_action_time,
                                    IDictionary<string,object> handler_info)
        {

        }
    }
}
