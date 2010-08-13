//
// Announcer.cs
//
// Authors:
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

using Telepathy;

using Banshee.Telepathy.API.DBus;

namespace Banshee.Telepathy.API
{
    public class Announcer : IDisposable
    {
        private Announcer ()
        {
        }

        public Announcer (ConnectionLocator locator)
        {
            ConnectionLocator = locator;
        }

        private ConnectionLocator locator;
        private ConnectionLocator ConnectionLocator {
            set {
                if (value == null) {
                    throw new ArgumentNullException ("locator");
                }

                locator = value;
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                try {
                    if (locator != null) {
                        Announce (String.Empty);
                    }
                }
                catch {}
                locator = null;
            }
        }

        public virtual void Announce (string message)
        {
            if (message == null) {
                throw new ArgumentNullException ("message");
            }


            try {
                foreach (Account account in locator.GetConnections ()) {
                    object obj = DBusUtility.GetProperty (account.BusType,
                                                          Constants.ACCOUNTMANAGER_IFACE,
                                                          account.AccountObjectPath,
                                                          Constants.ACCOUNT_IFACE,
                                                          "CurrentPresence");
                    SimplePresence presence = (SimplePresence) System.Convert.ChangeType (obj, typeof (SimplePresence));

                    presence.StatusMessage = message;
                    DBusUtility.SetProperty (account.BusType,
                                             Constants.ACCOUNTMANAGER_IFACE,
                                             account.AccountObjectPath,
                                             Constants.ACCOUNT_IFACE,
                                             "RequestedPresence",
                                             presence);
                }
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());
            }
        }
    }
}
