//
// DispatchManager.cs
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

using Banshee.Telepathy.API.Dispatchables;
using Banshee.Telepathy.API.Dispatchers;

namespace Banshee.Telepathy.API
{
    // TODO this thing is a bit messy
    public sealed class DispatchManager : IDisposable
    {
        private Connection conn;
        private readonly IDictionary <string, Dispatcher> dispatchers = new Dictionary <string, Dispatcher> ();
        private readonly IDictionary <Contact, IDictionary <string, IDictionary <object, Dispatchable>>> dispatchables = new Dictionary <Contact, IDictionary <string, IDictionary <object, Dispatchable>>> ();

        public event EventHandler<EventArgs> Dispatched;
        
        private DispatchManager ()
        {        
        }
        
        internal DispatchManager (Connection conn)
        {
            if (conn == null) {
                throw new ArgumentNullException ("conn");
            }
            
            this.conn = conn;
            Initialize ();
        }

        private void Initialize ()
        {
            if (conn.CapabilitiesSupported (ConnectionCapabilities.DBusTube)) {
                RegisterDispatcher (new DBusActivityDispatcher (conn));
            }

            if (conn.CapabilitiesSupported (ConnectionCapabilities.FileTransfer)) {
                RegisterDispatcher (new FileTransferDispatcher (conn));
            }

            if (conn.CapabilitiesSupported (ConnectionCapabilities.StreamTube)) {
                RegisterDispatcher (new StreamActivityDispatcher (conn));
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        private void Dispose (bool disposing)
        {
            if (disposing) {
                UnloadRegistered ();
                UnregisterDispatchers ();

                lock (dispatchables) {
                    foreach (Contact c in new List<Contact> (dispatchables.Keys)) {
                        RemoveAll (c);
                    }
                }
            }
        }
        
        private void RegisterDispatcher (Dispatcher d)
        {
            if (d == null) {
                throw new ArgumentNullException ("d");
            }
            
            lock (dispatchers) {
                dispatchers.Add (d.DispatchObject.FullName, d);
            }
        }

        private void UnloadRegistered ()
        {
            lock (dispatchers) {
                foreach (Dispatcher d in dispatchers.Values) {
                    d.Dispose ();
                }
            }
        }

        private void UnregisterDispatchers ()
        {
            lock (dispatchers) {
                dispatchers.Clear ();
            }
        }

        public void Request <T> (Contact contact, IDictionary <string, object> properties)
            where T : Dispatchable
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            } else if (properties == null) {
                throw new ArgumentNullException ("properties");
            }
            
            string type_name = typeof (T).FullName;
            if (dispatchers.ContainsKey (type_name)) {
                dispatchers[type_name].Request (contact.Handle, HandleType.Contact, properties);
            } else {
                throw new InvalidOperationException (String.Format ("Dispatcher for {0} is not registered.",
                                                                    type_name));
            }
        }

        internal void Add (Contact contact, object key, Dispatchable d)
        {
            Add (contact, key, d, true);
        }
        
        internal void Add (Contact contact, object key, Dispatchable d, bool replace)
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            } else if (key == null) {
                throw new ArgumentNullException ("key");
            } else if (d == null) {
                throw new ArgumentNullException ("d");
            } else if (!conn.Equals (contact.Connection)) {
                throw new InvalidOperationException (String.Format ("Contact does not belong to connection {0}",
                                                     conn.AccountId));
            }
            
            string type = d.GetType ().FullName;
            //Hyena.Log.DebugFormat ("DispatchManager.Add dispatchable type {0}", type);
            
            lock (dispatchables) {
                if (!dispatchables.ContainsKey (contact)) {
                    dispatchables.Add (contact, new Dictionary <string, IDictionary <object, Dispatchable>> ());
                }
                if (!dispatchables[contact].ContainsKey (type)) {
                    dispatchables[contact].Add (type, new Dictionary <object, Dispatchable> ());
                }
                if (!dispatchables[contact][type].ContainsKey (key)) {
                    dispatchables[contact][type].Add (key, d);
                } else if (replace) {
                    Remove (contact, key, d.GetType ());
                    Add (contact, key, d, false);
                } else {
					throw new InvalidOperationException ("Dispatchable could not be added to dispatch manager.");
				}
                
                OnDispatched (d, EventArgs.Empty);
                d.Initialize ();
            }
        }

        internal void Remove <T> (Contact contact, object key) where T : Dispatchable
        {
            Remove (contact, key, typeof (T));
        }

        internal void Remove (Contact contact, object key, Type obj_type)
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            } else if (key == null) {
                throw new ArgumentNullException ("key");
            }
            
            string type = obj_type.FullName;

            lock (dispatchables) {
                if (dispatchables.ContainsKey (contact)) {
                    if (dispatchables[contact].ContainsKey (type)) {
                        if (dispatchables[contact][type].ContainsKey (key)) {
                            dispatchables[contact][type][key].Dispose ();
                            // if it's gone, then great. don't care about an exception here
                            try {
                                dispatchables[contact][type].Remove (key);
                                Console.WriteLine ("{0} with key {1} removed from DM", type, key.ToString ());
                            } catch (KeyNotFoundException) {}
                        }
                    }
                }
            }
        }

        public void RemoveAll (Contact contact)
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            }
            
            lock (dispatchables) {
                if (dispatchables.ContainsKey (contact)) {
                    foreach (Dispatchable d in GetAll <Dispatchable> (contact)) {
                        if (d != null) {
                            d.Dispose ();
                        }
                    }
                    dispatchables.Remove (contact);
                }
            }
        }

        public T Get <T> (Contact contact, object key) where T : Dispatchable
        {
            return (T) Get (contact, key, typeof (T));
        }
        
        internal Dispatchable Get (Contact contact, object key, Type obj_type)
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            } else if (key == null) {
                throw new ArgumentNullException ("key");
            }
            
            string type = obj_type.FullName;
            
            //Console.WriteLine (String.Format ("{0} {1} {2}", type, contact.Handle, key.ToString ()));
            lock (dispatchables) {
                if (dispatchables.ContainsKey (contact)) {
                    if (dispatchables[contact].ContainsKey (type)) {
                        if (dispatchables[contact][type].ContainsKey (key)) {
                            return dispatchables[contact][type][key];
                        }
                    }
                }
            }

            return null;
        }
        
        public IEnumerable <T> GetAll <T> (Contact contact) where T : Dispatchable
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            }
            
            string type  = typeof (T).FullName;
            
            bool everything = false;
            if (typeof (T).Name.Equals (typeof (Dispatchable).Name)) {
                everything = true;
            }

            lock (dispatchables) { 
                if (dispatchables.ContainsKey (contact)) {
                    if (!everything && dispatchables[contact].ContainsKey (type)) {
                        foreach (object key in new List<object> (dispatchables[contact][type].Keys)) {
                            yield return (T) dispatchables[contact][type][key];
                        }
                    } else if (everything) {
                        foreach (KeyValuePair <string, IDictionary <object, Dispatchable>> kv in dispatchables[contact]) {
                            foreach (object key in new List<object> (kv.Value.Keys)) {
                                yield return (T) dispatchables[contact][kv.Key][key];
                            }
                        }
                    }
                }
            }
        }

        public bool Exists <T> (Contact contact, object key) where T : Dispatchable
        {
            if (contact == null) {
                throw new ArgumentNullException ("contact");
            } else if (key == null) {
                throw new ArgumentNullException ("key");
            }
            
            string type  = typeof (T).FullName;
            
            lock (dispatchables) {
                if (dispatchables.ContainsKey (contact)) {
                    if (dispatchables[contact].ContainsKey (type)) {
                        if (dispatchables[contact][type].ContainsKey (key)) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        
        private void OnDispatched (Dispatchable d, EventArgs args)
        {
            var handler = Dispatched;
            if (handler != null) {
                handler (d, args);
            }
        }
    }

}
