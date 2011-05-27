//
// DBusUtility.cs
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

namespace Banshee.Telepathy.API.DBus
{
    public class DBusProxyObjectNotFound : Exception
    {
        private const string message = "Proxy for {0} unavailable. Is application providing the proxy running?";

        public DBusProxyObjectNotFound () : base ()
        {
        }
        public DBusProxyObjectNotFound (string proxy) : base (String.Format (message, proxy))
        {
        }
        public DBusProxyObjectNotFound (string proxy, Exception e) : base (String.Format (message, proxy), e)
        {
        }
    }

    public enum BusType {
        Session = 0,
        System = 1
    };

    public static class DBusUtility
    {
        public static T GetProxy <T> (string bus_name, string object_path)
        {
            return GetProxy <T> (BusType.Session, bus_name, object_path, true);
        }

        public static T GetProxy <T> (string bus_name, string object_path, bool start)
        {
            return GetProxy <T> (BusType.Session, bus_name, object_path, start);
        }

        public static T GetProxy <T> (BusType bus_type, string bus_name, string object_path)
        {
            return GetProxy <T> (BusType.Session, bus_name, object_path, true);
        }

        public static T GetProxy <T> (BusType bus_type, string bus_name, string object_path, bool start)
        {
            if (bus_name == null) {
                throw new ArgumentNullException ("bus_name");
            }
            else if (object_path == null) {
                throw new ArgumentNullException ("object_path");
            }
            else if (bus_type < BusType.Session || bus_type > BusType.System) {
                throw new ArgumentOutOfRangeException ("bus_type");
            }

            Bus bus = null;
            switch (bus_type) {
            case BusType.Session:
                bus = Bus.Session;
                break;
            case BusType.System:
                bus = Bus.System;
                break;
            }

            if (!bus.NameHasOwner (bus_name)) {
                StartReply reply = bus.StartServiceByName (bus_name);
                if (reply != StartReply.Success && reply != StartReply.AlreadyRunning) {
                    throw new DBusProxyObjectNotFound (typeof(T).FullName);
                }
            }

            T proxy = bus.GetObject <T> (bus_name, new ObjectPath (object_path));
            if (proxy == null) {
                throw new DBusProxyObjectNotFound (typeof(T).FullName);

            }

            return proxy;
        }

        public static object GetProperty (BusType bus, string bus_name,
                                          string object_path, string iface, string property)
        {
            if (bus_name == null) {
                throw new ArgumentNullException ("bus_name");
            }

            if (object_path == null) {
                throw new ArgumentNullException ("object_path");
            }

            if (iface == null) {
                throw new ArgumentNullException ("iface");
            }

            if (property == null) {
                throw new ArgumentNullException ("property");
            }

            Properties p = GetProxy <Properties> (bus, bus_name, object_path);
            return p.Get (iface, property);
        }

        public static void SetProperty (BusType bus, string bus_name,
                                        string object_path, string iface, string property, object value)
        {
            if (bus_name == null) {
                throw new ArgumentNullException ("bus_name");
            }

            if (object_path == null) {
                throw new ArgumentNullException ("object_path");
            }

            if (iface == null) {
                throw new ArgumentNullException ("iface");
            }

            if (property == null) {
                throw new ArgumentNullException ("property");
            }

            Properties p = GetProxy <Properties> (bus, bus_name, object_path);
            p.Set (iface, property, value);
        }

        public static void Register (BusType bus_type, string bus_name, string object_path, object o)
        {
            if (bus_name == null) {
                throw new ArgumentNullException ("bus_name");
            }
            else if (object_path == null) {
                throw new ArgumentNullException ("object_path");
            }
            else if (bus_type < BusType.Session || bus_type > BusType.System) {
                throw new ArgumentOutOfRangeException ("bus_type");
            }
            else if (o == null) {
                throw new ArgumentNullException ("o");
            }

            Bus bus = null;
            switch (bus_type) {
            case BusType.Session:
                bus = Bus.Session;
                break;
            case BusType.System:
                bus = Bus.System;
                break;
            }

            if (bus.RequestName (bus_name) == RequestNameReply.PrimaryOwner) {
                bus.Register (new ObjectPath (object_path), o);
            }
        }

        public static void Unregister (BusType bus_type, string object_path)
        {
            if (object_path == null) {
                throw new ArgumentNullException ("object_path");
            }
            else if (bus_type < BusType.Session || bus_type > BusType.System) {
                throw new ArgumentOutOfRangeException ("bus_type");
            }

            Bus bus = null;
            switch (bus_type) {
            case BusType.Session:
                bus = Bus.Session;
                break;
            case BusType.System:
                bus = Bus.System;
                break;
            }

            bus.Unregister (new ObjectPath (object_path));
        }
    }
}
