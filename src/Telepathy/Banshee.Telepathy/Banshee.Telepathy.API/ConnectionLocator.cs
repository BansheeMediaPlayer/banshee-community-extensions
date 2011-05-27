//
// ConnectionLocator.cs
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
    public enum AccountConnectionStatus {
        Connected,
        Disconnected
    };

    public class ConnectionStatusEventArgs : EventArgs
    {
        private AccountConnectionStatus action;

        public ConnectionStatusEventArgs (AccountConnectionStatus action,
                                           string account_id,
                                           string bus_name,
                                           string object_path,
                                           string account_object_path)
        {
            this.action = action;
            this.account_id = account_id;
            this.bus_name = bus_name;
            this.object_path = object_path;
            this.account_object_path = account_object_path;
        }

        public AccountConnectionStatus Action {
            get { return action; }
        }

        private string account_id;
        public string AccountId {
            get { return account_id; }
        }

        private string bus_name;
        public string BusName {
            get { return bus_name; }
        }

        private string object_path;
        public string ObjectPath {
            get { return object_path; }
        }

        private string account_object_path;
        public string AccountObjectPath {
            get { return account_object_path; }
        }
    }

    public class ConnectionLocator : IDisposable
    {

        protected IAccountManager account_manager;
        protected readonly IDictionary <string, Account> connections = new Dictionary<string, Account> ();

        public event EventHandler <ConnectionStatusEventArgs> ConnectionStatusChanged;

        public ConnectionLocator ()
        {
            account_manager = DBusUtility.GetProxy <IAccountManager> (bus, Constants.ACCOUNTMANAGER_IFACE,
                Constants.ACCOUNTMANAGER_PATH);

            Initialize ();
        }

        private BusType bus = BusType.Session;
        public BusType Bus {
            get { return bus; }
        }

        protected void Initialize ()
        {
            AddConnections ();
        }

        private void AddConnections ()
        {
            ObjectPath[] paths = (ObjectPath[]) DBusUtility.GetProperty(bus, Constants.ACCOUNTMANAGER_IFACE, Constants.ACCOUNTMANAGER_PATH, Constants.ACCOUNTMANAGER_IFACE, "ValidAccounts");

            foreach (ObjectPath p in paths) {
                Account account = new Account (p.ToString ());

                account.ConnectionStatusChanged += delegate(object sender, ConnectionStatusEventArgs args) {
                    OnConnectionStatusChanged (args);
                };

                connections.Add (p.ToString (), account);
            }
        }

        public IEnumerable <Account> GetConnections ()
        {
            foreach (Account account in connections.Values) {
                if (account.Connected) {
                    yield return account;
                }
            }
        }

        protected virtual void OnConnectionStatusChanged (ConnectionStatusEventArgs args)
        {
            EventHandler <ConnectionStatusEventArgs> handler = ConnectionStatusChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                if (account_manager != null) {
                    try {
                        //mission_control.AccountStatusChanged -= OnAccountStatusChanged;
                    }
                    catch (Exception) {}
                    account_manager = null;
                }

                connections.Clear ();
            }
        }


    }
}
