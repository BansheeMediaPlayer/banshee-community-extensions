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

using NDesk.DBus;

using Telepathy;
using Telepathy.MissionControl;

using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API
{
    public struct ConnectionParms
    {
        public string account_id;
        public string bus_name;
        public string object_path;
    }
    
    public class ConnectionLocatorEventArgs : EventArgs
    {
        private McStatus action;
        private ConnectionParms parms;

        public ConnectionLocatorEventArgs (McStatus action, string account_id, 
                           string bus_name, string object_path)
        {
            this.action = action;
            this.parms.account_id = account_id;
            this.parms.bus_name = bus_name;
            this.parms.object_path = object_path;
        }

        public McStatus Action {
            get { return action; }
        }

        public string AccountId {
            get { return parms.account_id; }
        }

        public string BusName {
            get { return parms.bus_name; }
        }

        public string ObjectPath {
            get { return parms.object_path; }
        }
    }

    public class ConnectionLocator : IDisposable
    {
        private BusType bus;
        protected IMissionControl mission_control;
        private AccountStatusChangedParms previous_parms = null;
        
        public event EventHandler <ConnectionLocatorEventArgs> ConnectionStatusChanged;
        
        public ConnectionLocator ()
        {
            bus = BusType.Session;
            mission_control = DBusUtility.GetProxy <IMissionControl> (bus, Constants.MISSIONCONTROL_IFACE,
                Constants.MISSIONCONTROL_PATH);
            
            mission_control.AccountStatusChanged += OnAccountStatusChanged;
        }

        public BusType Bus {
            get { return bus; }
        }
        
        public ConnectionParms [] GetConnections ()
        {
            string[] ids;
            ids = mission_control.GetOnlineConnections ();
            ConnectionParms [] parms = new ConnectionParms[ids.Length];

            for (int i = 0; i < ids.Length; i++) {
                string bus_name;
                ObjectPath object_path;
                mission_control.GetConnection (ids[i], out bus_name, out object_path);
                parms[i].account_id = ids[i];
                parms[i].bus_name = bus_name;
                parms[i].object_path = object_path.ToString ();
            }

            return parms;
        }

        protected virtual void OnAccountStatusChanged (McStatus status, McPresence presence, 
                                            ConnectionStatusReason reason, string account_id)
        {
            //HACK MissionControl issues duplicate events, so suppress them
            if (previous_parms == null) {
                previous_parms = new AccountStatusChangedParms (status, presence, reason, account_id);
            }
            else {
                AccountStatusChangedParms current_parms = new AccountStatusChangedParms (status, presence, reason, account_id);
                if (previous_parms.Equals (current_parms)) {
                    Console.WriteLine ("Suppressing duplicate MissionControl.AccountStatusChanged event.");
                    return;
                }
                else {
                    previous_parms = current_parms;
                }
            }
                
            Console.WriteLine ("Mission Control reporting account status changed: status {0}, presence {1}, reason {2}, account {3}",
                             status.ToString (), presence.ToString (), reason.ToString (), account_id);

            string bus_name = null;
            ObjectPath object_path = null;
            
            switch (status) {
                case McStatus.Connected:
                    if (presence != McPresence.Unset) {
                        mission_control.GetConnection (account_id, out bus_name, out object_path);
                        OnConnectionStatusChanged (new ConnectionLocatorEventArgs (status, account_id, 
                                                                bus_name, object_path.ToString ()));
                    }
                    break;
                case McStatus.Disconnected:
                    OnConnectionStatusChanged (new ConnectionLocatorEventArgs (status, account_id, 
                                                                "", ""));
                    break;
            }
        }

        protected virtual void OnConnectionStatusChanged (ConnectionLocatorEventArgs args)
        {
            EventHandler <ConnectionLocatorEventArgs> handler = ConnectionStatusChanged;
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
                if (mission_control != null) {
                    try {
                        mission_control.AccountStatusChanged -= OnAccountStatusChanged;
                    }
                    catch (Exception) {}
                    mission_control = null;
                }
            }
        }

        //HACK MissionControl issues duplicate events, so suppress them
        private class AccountStatusChangedParms
        {
            private McStatus status;
            private McPresence presence;
            private ConnectionStatusReason reason;
            private string account_id;
    
            public AccountStatusChangedParms (McStatus status, 
                                              McPresence presence, 
                                              ConnectionStatusReason reason,
                                              string account_id)
            {
                this.status = status;
                this.presence = presence;
                this.reason = reason;
                this.account_id = account_id;
            }
    
            public McStatus Status { 
                get { return status; }
            }
                
            public McPresence Presence { 
                get { return presence; }
            }
            
            public ConnectionStatusReason Reason { 
                get { return reason; }
            }
            
            public string AccountId { 
                get { return account_id; }
            }
    
            public override bool Equals (object obj)
            {
                AccountStatusChangedParms parms = obj as AccountStatusChangedParms;
                
                if (parms != null) {
                    return parms.Status == status && parms.Presence == presence &&
                        parms.Reason == reason && parms.AccountId.Equals (account_id);
                }
                
                return false;
            }
    
            public override int GetHashCode ()
            {
                return status.GetHashCode () + presence.GetHashCode () + reason.GetHashCode () +
                    account_id.GetHashCode ();
            }
    
        }
    }
}
