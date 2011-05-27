//
// Account.cs
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

using Telepathy;

using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API
{
    public class Account
    {
        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        private Account ()
        {
        }

        public Account (String object_path)
        {
            AccountObjectPath = object_path;
            Initialize ();
        }

        private bool connected = false;
        public bool Connected {
            get { return connected; }
            private set {
                if (connected != value) {

                    OnConnectionStatusChanged (new ConnectionStatusEventArgs (value ? AccountConnectionStatus.Connected : AccountConnectionStatus.Disconnected,
                                                                              AccountId,
                                                                              BusName,
                                                                              ObjectPath,
                                                                              AccountObjectPath));
                    connected = value;
                }
            }
        }

        private string object_path;
        public string ObjectPath {
            get { return object_path; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("object_path");
                }
                object_path = value;
            }
        }

        private string account_path;
        public string AccountObjectPath {
            get { return account_path; }
            private set {
                if (value == null) {
                   throw new ArgumentNullException ("account_path");
                }

                account_path = value;
            }
        }

        public string BusName {
            get {
                if (ObjectPath != null) {
                    return ObjectPath.Substring (1).Replace ("/", ".");
                }

                return null;
            }
        }

        private string account_id;
        public string AccountId {
            get { return account_id; }
            private set {
                if (value == null) {
                    throw new ArgumentNullException ("account_id");
                }

                account_id = value;
            }
        }

        private BusType bus = BusType.Session;
        public BusType BusType {
            get { return bus; }
        }

        private IAccount iaccount;
        internal IAccount IAccount {
            get { return iaccount; }
        }

        private void Initialize ()
        {
            iaccount = DBusUtility.GetProxy <IAccount> (BusType.Session,
                                                        Constants.ACCOUNTMANAGER_IFACE,
                                                        AccountObjectPath);

            iaccount.AccountPropertyChanged += OnAccountPropertyChanged;

            ObjectPath = GetConnectionObjectPath ();
            connected = !ObjectPath.Equals ("/");

            AccountId = (string) DBusUtility.GetProperty (BusType,
                                                          Constants.ACCOUNTMANAGER_IFACE,
                                                          AccountObjectPath,
                                                          Constants.ACCOUNT_IFACE,
                                                          "NormalizedName");
        }

        private string GetConnectionObjectPath ()
        {
            ObjectPath path = (ObjectPath) DBusUtility.GetProperty (BusType,
                                                                    Constants.ACCOUNTMANAGER_IFACE,
                                                                    AccountObjectPath,
                                                                    Constants.ACCOUNT_IFACE,
                                                                    "Connection");
            return path.ToString ();
        }

        private void OnAccountPropertyChanged (IDictionary <string, object> properties)
        {
            if (properties.ContainsKey ("ConnectionStatus")) {
                Connected = (ConnectionStatus) properties["ConnectionStatus"] == ConnectionStatus.Connected;
            }
            if (properties.ContainsKey ("Connection")) {
                ObjectPath = GetConnectionObjectPath ();
            }
        }

        protected virtual void OnConnectionStatusChanged (ConnectionStatusEventArgs args)
        {
            EventHandler <ConnectionStatusEventArgs> handler = ConnectionStatusChanged;
            if (handler != null) {
                handler (this, args);
            }
        }
    }
}