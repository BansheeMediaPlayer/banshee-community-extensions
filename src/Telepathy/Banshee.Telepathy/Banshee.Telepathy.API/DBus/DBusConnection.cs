//
// DBusConnection.cs
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

namespace Banshee.Telepathy.API.DBus
{
    public sealed class DBusConnection
    {
        private string address;
        private global::DBus.Connection conn = null;
        private IDictionary <string, object> registered;

        private DBusConnection ()
        {
            registered = new Dictionary <string, object> ();
        }

        public DBusConnection (string address) : this ()
        {
            Address = address;
            conn = global::DBus.Connection.Open (address);
        }

        public DBusConnection (string address, bool mainloop) : this (address)
        {
            if (mainloop) {
                ConnectToMainLoop ();
            }
        }

        public string Address {
            get { return address; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("address");
                }
                address = value;
            }
        }

        public void Register (string object_path, object o)
        {
            lock (registered) {
                if (registered.ContainsKey (object_path)) {
                    throw new InvalidOperationException (String.Format ("{0} already registered.", object_path));
                }

                //no bus name, as it really does not matter for a peer-to-peer connection
                //actually, when doing a GetObject, the busName is totally ignored.
                conn.Register (new ObjectPath (object_path), o);
                registered.Add (object_path, o);
            }
        }

        public void Unregister (string object_path)
        {
            lock (registered) {
                conn.Unregister (new ObjectPath (object_path));
                registered.Remove (object_path);
            }
        }

        public void UnregisterAll ()
        {
            lock (registered) {
                foreach (string s in registered.Keys) {
                    conn.Unregister (new ObjectPath (s));
                }
            registered.Clear ();
            }
        }

        public void Close ()
        {
            UnregisterAll ();
            // other clean up here?
        }

        public T GetProxy <T> (string bus_name, string object_path)
        {
            if (bus_name == null) {
                throw new ArgumentNullException ("bus_name");
            }
            else if (object_path == null) {
                throw new ArgumentNullException ("object_path");
            }

            T proxy = conn.GetObject <T> (bus_name, new ObjectPath (object_path));
            if (proxy == null) {
                throw new DBusProxyObjectNotFound (typeof(T).FullName);

            }

            return proxy;
        }

        public void ConnectToMainLoop ()
        {
            if (conn != null) {
                global::DBus.BusG.Init (conn);
            }
        }
    }
}
