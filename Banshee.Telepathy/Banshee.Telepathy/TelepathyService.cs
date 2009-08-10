//
// TelepathyService.cs
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

using Banshee.Base;
using Banshee.Collection;
using Banshee.Sources;
using Banshee.ServiceStack;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.DBus;
using Banshee.Telepathy.API.Dispatchables;

using Banshee.Telepathy.Data;
using Banshee.Telepathy.DBus;
using Banshee.Telepathy.Gui;
using Banshee.Telepathy.Net;

using Hyena;

using Telepathy;
using Telepathy.MissionControl;

namespace Banshee.Telepathy
{
    public class TelepathyService : IExtensionService, IDisposable, IDelayedInitializeService
    {
        private static StreamingHTTPProxyServer proxy_server;
        private static StreamingServer streaming_server;
        
        private ContactContainerSource container;
        private ConnectionLocator locator;
        private DownloadManager download_manager;
        private UploadManager upload_manager;
        
        // track ConnectionManagers we're using and ensure only one per connection
        private IDictionary <string, Connection> conn_map;
        
        // track ContactSources for each Connection. Needed since all contacts for all connections
        // are lumped under one container source
        private IDictionary <string, IDictionary <uint,ContactSource>> source_map;
                
        public TelepathyService()
        {
        }

        internal static StreamingHTTPProxyServer ProxyServer {
            get { return proxy_server; }
        }

        internal static StreamingServer StreamingServer {
            get { return streaming_server; }
        }

        private static readonly string cache_dir = Paths.Combine (Paths.ExtensionCacheRoot, "contacts");
        public static string CacheDirectory {
            get { return cache_dir; }
        }
        
        void IExtensionService.Initialize ()
        {
        }

        public void Dispose ()
        {
            if (locator != null) {
                locator.ConnectionStatusChanged -= OnConnectionStatusChanged;
                locator.Dispose ();
                locator = null;
            }

            if (streaming_server != null) {
                streaming_server.Stop ();
                streaming_server = null;
            }
            
            if (proxy_server != null) {
                proxy_server.Stop ();
                proxy_server = null;
            }
            
            if (download_manager != null) {
                download_manager.Dispose ();
            }

            if (upload_manager != null) {
                upload_manager.Dispose ();
            }
            
            if (container != null) {
                foreach (ContactSource source in container.Children) {
                    Log.DebugFormat ("Disposing of ContactSource named {0}", source.Name);
                    source.Contact.ContactServicesChanged -= OnContactServicesChanged;
                    source.Dispose ();
                }
                ServiceManager.SourceManager.RemoveSource (container, true);
                container = null;
            }

            foreach (KeyValuePair <string, Connection> kv in conn_map) {
                if (kv.Value != null) {
                    kv.Value.Roster.RosterStateChanged -= OnRosterStateChanged;
                    kv.Value.Disconnected -= OnDisconnected;
                    kv.Value.Dispose ();
                }
            }

            UnregisterDBusServiceWithEmpathy (MetadataProviderService.BusName);
            UnregisterStreamServiceWithEmpathy (StreamingServer.ServiceName);
            
            conn_map.Clear ();
            source_map.Clear ();
        }

        public void DelayedInitialize ()
        {
            ConnectionParms [] parms = null;
            
            conn_map = new Dictionary <string, Connection> ();
            source_map = new Dictionary <string, IDictionary <uint, ContactSource>> ();
            
            container = new ContactContainerSource (this);
            ServiceManager.SourceManager.AddSource (container);

            download_manager = new DownloadManager (this);
            upload_manager = new UploadManager (this);

            try {
                locator = new ConnectionLocator ();
                parms = locator.GetConnections ();
            }
            catch (DBusProxyObjectNotFound e) {
                Log.Error (e.ToString ());
                return;
            }

            RegisterDBusServiceWithEmpathy (MetadataProviderService.BusName);
            RegisterStreamServiceWithEmpathy (StreamingServer.ServiceName);
            
            foreach (ConnectionParms parm in parms) {
                CreateConnection (parm.account_id, parm.bus_name, parm.object_path);
            }

            locator.ConnectionStatusChanged += OnConnectionStatusChanged;

            try {
                proxy_server = new StreamingHTTPProxyServer ();
                proxy_server.Start ();
            }
            catch (Exception e) {
                Log.Error ("Failed to start Banshee.Telepathy.StreamingHTTPProxyServer");
                Log.Exception (e);
            }

            try {
                streaming_server = new StreamingServer ();
                streaming_server.Start ();
            }
            catch (Exception e) {
                Log.Error ("Failed to start Banshee.Telepathy.StreamingHTTPProxyServer");
                Log.Exception (e);
            }
        }

        private void RegisterDBusServiceWithEmpathy (string service)
        {
            // HACK - suppress error messages from Empathy
            string empathy_handler_bus = EmpathyConstants.DTUBE_HANDLER_IFACE + "." +
                service.Replace (".", "_");
            string empathy_handler_path = EmpathyConstants.DTUBE_HANDLER_PATH + "/" +
                service.Replace (".", "_");
            
            DBusUtility.Register (BusType.Session, empathy_handler_bus, 
                                  empathy_handler_path, new EmpathyHandler ());
        }

        private void UnregisterDBusServiceWithEmpathy (string service)
        {
            string empathy_handler_path = EmpathyConstants.DTUBE_HANDLER_PATH + "/" +
                service.Replace (".", "_");
            
            DBusUtility.Unregister (BusType.Session, empathy_handler_path);
        }

        private void RegisterStreamServiceWithEmpathy (string service)
        {
            // HACK - suppress error messages from Empathy
            string empathy_handler_bus = EmpathyConstants.STREAMTUBE_HANDLER_IFACE + "." +
                service.Replace (".", "_");
            string empathy_handler_path = EmpathyConstants.STREAMTUBE_HANDLER_PATH + "/" +
                service.Replace (".", "_");
            
            DBusUtility.Register (BusType.Session, empathy_handler_bus, 
                                  empathy_handler_path, new EmpathyHandler ());
        }

        private void UnregisterStreamServiceWithEmpathy (string service)
        {
            string empathy_handler_path = EmpathyConstants.STREAMTUBE_HANDLER_PATH + "/" +
                service.Replace (".", "_");
            
            DBusUtility.Unregister (BusType.Session, empathy_handler_path);
        }
        
        public IEnumerable <Connection> GetActiveConnections ()
        {
            foreach (Connection c in conn_map.Values) {
                yield return c;
            }
        }
        
        private void CreateConnection (string account_id, string bus_name, string object_path)
        {
            if (!conn_map.ContainsKey (account_id)) {
                //Log.DebugFormat ("{0} not found in map", account_id);
                try {
                    AddConnection (new Connection (bus_name, object_path, 
                                               account_id, ConnectionCapabilities.DBusTransport |
                                                ConnectionCapabilities.FileTransfer |
                                                ConnectionCapabilities.SocketTransport));
                }
                catch (DBusProxyObjectNotFound e) {
                    Log.Error (e.ToString ());
                }
                catch (NotSupportedException e) {
                    Log.Debug (e.ToString ());
                }
            }
        }


        private void AddConnection (Connection conn)
        {
            conn_map.Add (conn.AccountId, conn);
            conn.CacheDirectory = Paths.Combine (TelepathyService.CacheDirectory, conn.AccountId);
            
            try {
                //Log.DebugFormat ("Connection object for {0} created successfully", conn.AccountId);
                source_map.Add (conn.AccountId, new Dictionary <uint, ContactSource> ());
                conn.Disconnected += OnDisconnected;

                conn.Roster.RosterStateChanged += OnRosterStateChanged;
                conn.Roster.Load ();
            }
            catch (DBusProxyObjectNotFound e) {
                Log.Error (e.ToString ());
            }
        }

        
        private void RemoveConnection (string account_id)
        {
            if (conn_map.ContainsKey (account_id)) {
                Log.DebugFormat ("Removing connection {0}", account_id);
                RemoveContactSources (conn_map[account_id].Roster);
                conn_map[account_id].Roster.RosterStateChanged -= OnRosterStateChanged;
                conn_map[account_id].Dispose ();
                conn_map.Remove (account_id);
                source_map.Remove (account_id);
            }
        }
        
        private void AddContactSource (Contact contact)
        {
            if (contact.HasService (MetadataProviderService.BusName)) {
                ContactSource source = new ContactSource (contact);
                container.AddChildSource (source);
                source_map[contact.AccountId].Add (contact.Handle, source);
            }

            contact.ContactServicesChanged += OnContactServicesChanged;
        }

        private void RemoveContactSource (Contact contact)
        {
            if (source_map[contact.AccountId].ContainsKey (contact.Handle)) {
                ContactSource source = source_map[contact.AccountId][contact.Handle];
                //source.Contact.ContactServicesChanged -= OnContactServicesChanged;
                source.Dispose ();
                container.RemoveChildSource (source);
                source_map[contact.AccountId].Remove (contact.Handle);
            }
        }

        private void AddContactSources (Roster roster)
        {
            foreach (Contact contact in roster.GetAllContacts ()) {
                AddContactSource (contact);
            }

            container.SortChildSources ();
        }

        private void RemoveContactSources (Roster roster)
        {
            foreach (KeyValuePair <uint, ContactSource> kv in source_map[roster.Connection.AccountId]) {
                kv.Value.Dispose ();
                container.RemoveChildSource (kv.Value);
                //kv.Value.Contact.ContactServicesChanged -= OnContactServicesChanged;
            }

            source_map[roster.Connection.AccountId].Clear ();
        }

        private void OnContactServicesChanged (object o, EventArgs args)
        {
            Contact contact = o as Contact;
            bool has_service = contact.HasService (MetadataProviderService.BusName);

            Log.DebugFormat ("{0} in OnContactServicesChanged", contact.Name);
            
            if (source_map[contact.AccountId].ContainsKey (contact.Handle)) {
                if (!has_service) {
                    RemoveContactSource (contact);
                }
            }
            else {
                if (has_service) {
                    AddContactSource (contact);
                }
            }

            container.SortChildSources ();
            
        }
        
        private void OnContactMembershipChanged (object o, ContactMembershipEventArgs args)
        {
            if (args.Action == ContactMembership.Added) {
                AddContactSource ((Contact)o);
            }
            else if (args.Action == ContactMembership.Removed) {
                RemoveContactSource ((Contact)o);
            }

            container.SortChildSources ();
        }
        
        private void OnConnectionStatusChanged (object sender, ConnectionLocatorEventArgs args)
        {
            if (args.Action == McStatus.Connected) {
                CreateConnection (args.AccountId, args.BusName, args.ObjectPath);
            }
        }

        private void OnDisconnected (object sender, EventArgs args)
        {
            Connection conn = sender as Connection;
            
            if (conn != null) {
                RemoveConnection (conn.AccountId);
            }
        }
        
        private void OnRosterStateChanged (object sender, RosterEventArgs args)
        {
            if (args.Action == RosterState.Loaded) {
                Roster roster = (Roster)sender;
                AddContactSources (roster);
                roster.ContactMembershipChanged += OnContactMembershipChanged;
                System.Threading.Thread.Sleep (1000);   //FIXME
                
                roster.Connection.AddService (new ContactService (ContactServiceType.DBusTransport, 
                                              HandleType.Contact, 
                                              MetadataProviderService.BusName), false);

                //TODO move this elsewhere?
                StreamActivity.AutoAccept = true;
                roster.Connection.AddService (new ContactService (ContactServiceType.SocketTransport,
                                                                  HandleType.Contact,
                                                                  StreamingServer.ServiceName,
                                                                  StreamingServer.Address));
            }
        }

        string IService.ServiceName {
            get { return "TelepathyService"; }
        }

    }
}
