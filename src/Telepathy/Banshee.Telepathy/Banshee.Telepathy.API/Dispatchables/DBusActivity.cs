//
// DBusActivity.cs
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
using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API.Dispatchables
{
    public class DBusActivity : Activity
    {
        private DBusTubeChannel tube;
        private DBusConnection dbus_conn;

        internal DBusActivity (Contact c, DBusTubeChannel tube) : base (c, tube)
        {
            this.tube = tube;
        }

        private static bool auto_accept = false;
        public static bool AutoAccept {
            get { return auto_accept; }
            set { auto_accept = value; }
        }

        public void RegisterDBusObject (object o, string object_path)
        {
            if (State == ActivityState.Connected) {
                dbus_conn.Register (object_path, o);
            }
            else {
                throw new InvalidOperationException ("Activity is not connected.");
            }
        }

        public void UnRegisterDBusObject (string object_path)
        {
            if (State == ActivityState.Connected) {
                dbus_conn.Unregister (object_path);
            }
            else {
                throw new InvalidOperationException ("Activity is not connected.");
            }
        }

        public T GetDBusObject <T> (string bus_name, string object_path)
        {
            if (State == ActivityState.Connected) {
                return dbus_conn.GetProxy <T> (bus_name, object_path);
            }

            return default (T);
        }

        protected override void OnChannelReady (object sender, EventArgs args)
        {
            Console.WriteLine ("{0} Connection to address {1}", Contact.Name, tube.Address);

            try {
                dbus_conn = new DBusConnection (tube.Address, true);
                State = ActivityState.Connected;
                OnReady (EventArgs.Empty);
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (dbus_conn != null) {
                    dbus_conn.Close ();
                    dbus_conn = null;
                }
            }

            base.Dispose (disposing);
        }

        protected override void OnTubeOffered (object sender, EventArgs args)
        {
            base.OnTubeOffered (sender, args);

            if (auto_accept) {
                this.Accept ();
            } else {
                OnResponseRequired (EventArgs.Empty);
            }
        }
    }
}