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

using Hyena;
using Banshee.Base;
using Banshee.Collection;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Telepathy.Data;
using Banshee.Telepathy.DBus;
using Banshee.Telepathy.Gui;
using Banshee.Telepathy.Net;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Data;
using APIData = Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.DBus;
using Banshee.Telepathy.API.Dispatchables;

using Telepathy;
using Telepathy.MissionControl;

namespace Banshee.Telepathy
{
    public class TelepathyService : IExtensionService, IDisposable, IDelayedInitializeService
    {
        private static StreamingHTTPProxyServer proxy_server;
        private static StreamingServer streaming_server;
        
        private ContactContainerSource container;
        
        private static DownloadManagerUi download_manager;
        private static UploadManagerUi upload_manager;
        
        // track ConnectionManagers we're using and ensure only one per connection
        private IDictionary <string, Connection> conn_map;
        
        // track ContactSources for each Connection. Needed since all contacts for all connections
        // are lumped under one container source
        private IDictionary <string, IDictionary <Contact, ContactSource>> source_map;
                
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
        
        public static DownloadManagerUi DownloadManager {
            get { return download_manager; }
        }
        
        public static UploadManagerUi UploadManager {
            get { return upload_manager; }
        }
        
        void IExtensionService.Initialize ()
        {
        }

        public void Dispose ()
        {
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
                download_manager = null;
            }

            if (upload_manager != null) {
                upload_manager.Dispose ();
                upload_manager = null;
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

            if (locator != null) {
                locator.ConnectionStatusChanged -= OnConnectionStatusChanged;
                locator.Dispose ();
                locator = null;
            }

            foreach (KeyValuePair <string, Connection> kv in conn_map) {
                if (kv.Value != null) {
                    kv.Value.Roster.RosterStateChanged -= OnRosterStateChanged;
                    kv.Value.Disconnected -= OnDisconnected;
                    kv.Value.Dispose ();
                }
            }

            conn_map.Clear ();
            source_map.Clear ();
            
            TelepathyNotification notify = TelepathyNotification.Get;
            if (notify != null) {
                notify.Dispose ();
            }
        }

        public void DelayedInitialize ()
        {
            // require for bundled version of NDesk.DBus
            NDesk.DBus.BusG.Init ();

            conn_map = new Dictionary <string, Connection> ();
            source_map = new Dictionary <string, IDictionary <Contact, ContactSource>> ();
            
            container = new ContactContainerSource (this);
            ServiceManager.SourceManager.AddSource (container);

            download_manager = new DownloadManagerUi ();
            upload_manager = new UploadManagerUi ();

            try {
                locator = new ConnectionLocator ();
            }
            catch (DBusProxyObjectNotFound e) {
                Log.Error (e.ToString ());
                return;
            }

            foreach (Account account in locator.GetConnections ()) {
                CreateConnection (account);
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
        
        public IEnumerable <Connection> GetActiveConnections ()
        {
            foreach (Connection c in conn_map.Values) {
                yield return c;
            }
        }

        private void CreateConnection (Account account)
        {
            CreateConnection (account.BusName, account.ObjectPath, account.AccountId, account.AccountObjectPath);
        }
        
        private void CreateConnection (string bus_name, string object_path, string account_id, string account_path)
        {
            if (!conn_map.ContainsKey (account_path)) {
                //Log.DebugFormat ("{0} not found in map", account_id);
                try {
                    AddConnection (new Connection (bus_name, 
                                                   object_path, 
                                                   account_id, 
                                                   account_path, 
                                                   ConnectionCapabilities.DBusTube |
                                                   ConnectionCapabilities.FileTransfer |
                                                   ConnectionCapabilities.StreamTube));
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
            conn_map.Add (conn.AccountObjectPath, conn);
            conn.CacheDirectory = Paths.Combine (TelepathyService.CacheDirectory, conn.AccountId);
            
            try {
                //Log.DebugFormat ("Connection object for {0} created successfully", conn.AccountId);
                source_map.Add (conn.AccountObjectPath, new Dictionary <Contact, ContactSource> ());
                conn.Disconnected += OnDisconnected;

                conn.Roster.RosterStateChanged += OnRosterStateChanged;
                conn.Roster.Load ();
            }
            catch (DBusProxyObjectNotFound e) {
                Log.Error (e.ToString ());
            }
        }

        
        private void RemoveConnection (string object_path)
        {
            if (conn_map.ContainsKey (object_path)) {
                Log.DebugFormat ("Removing connection {0}", object_path);
                RemoveContactSources (conn_map[object_path].Roster);
                conn_map[object_path].Roster.RosterStateChanged -= OnRosterStateChanged;
                conn_map[object_path].Dispose ();
                conn_map.Remove (object_path);
                source_map.Remove (object_path);
            }
        }
        
        private void AddContactSource (Contact contact)
        {
            if (contact.SupportedChannels.GetChannelInfo <DBusTubeChannelInfo> (MetadataProviderService.BusName) != null) {
                ContactSource source = new ContactSource (contact);
                container.AddChildSource (source);
                source_map[contact.Connection.AccountObjectPath].Add (contact, source);
            }

            contact.ContactServicesChanged += OnContactServicesChanged;
        }

        private void RemoveContactSource (Contact contact)
        {
            if (source_map[contact.Connection.AccountObjectPath].ContainsKey (contact)) {
                ContactSource source = source_map[contact.Connection.AccountObjectPath][contact];
                if (source.Contact.Connection.Status == ConnectionStatus.Disconnected) {
                    source.Contact.ContactServicesChanged -= OnContactServicesChanged;
                }
                
                // remove and close all channels, in case we don't get closed events
                contact.DispatchManager.RemoveAll (contact);
                
                source.Dispose ();
                container.RemoveChildSource (source);
                source_map[contact.Connection.AccountObjectPath].Remove (contact);
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
            ICollection<ContactSource> collection = source_map[roster.Connection.AccountObjectPath].Values;
            IEnumerator<ContactSource> e;
            while ((e = collection.GetEnumerator ()).MoveNext ()) {
                RemoveContactSource (e.Current.Contact);
            }

            source_map[roster.Connection.AccountObjectPath].Clear ();
        }

        private void OnContactServicesChanged (object o, EventArgs args)
        {
            Contact contact = o as Contact;
            bool has_service = contact.SupportedChannels.GetChannelInfo <DBusTubeChannelInfo> (MetadataProviderService.BusName) != null;

            Log.DebugFormat ("{0} in OnContactServicesChanged", contact.Name);
            
            if (source_map[contact.Connection.AccountObjectPath].ContainsKey (contact)) {
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
        
        private void OnConnectionStatusChanged (object sender, ConnectionStatusEventArgs args)
        {
            if (args.Action == AccountConnectionStatus.Connected) {
                if (conn_map.ContainsKey (args.AccountObjectPath)) {
                    RemoveConnection (args.AccountObjectPath);
                }
                
                CreateConnection (args.BusName, args.ObjectPath, args.AccountId, args.AccountObjectPath);
            }
        }

        private void OnDisconnected (object sender, EventArgs args)
        {
            Connection conn = sender as Connection;
            
            if (conn != null) {
                RemoveConnection (conn.AccountObjectPath);
            }
        }
        
        private void OnRosterStateChanged (object sender, RosterEventArgs args)
        {
            if (args.Action == RosterState.Loaded) {
                Roster roster = (Roster)sender;
                AddContactSources (roster);
                roster.ContactMembershipChanged += OnContactMembershipChanged;
                System.Threading.Thread.Sleep (1000);   //FIXME

                AddSupportedChannels (roster.Connection);
            }
        }

        private void AddSupportedChannels (Connection conn)
        {
            IList<APIData.ChannelInfo> channels = new List<APIData.ChannelInfo> ();
            channels.Add (new DBusTubeChannelInfo (ChannelType.DBusTube, HandleType.Contact, MetadataProviderService.BusName));
            channels.Add (new StreamTubeChannelInfo (ChannelType.StreamTube, HandleType.Contact, StreamingServer.ServiceName, StreamingServer.Address));
            channels.Add (new FileTransferChannelInfo (ChannelType.FileTransfer, HandleType.Contact, "audio/mpeg", "Telepathy extension for Banshee transfer"));
            
            //TODO move this elsewhere?
            StreamActivity.AutoAccept = true;

            conn.AdvertiseSupportedChannels ("Banshee", channels);
        }

        string IService.ServiceName {
            get { return "TelepathyService"; }
        }

        private ConnectionLocator locator;
        public ConnectionLocator ConnectionLocator {
            get { return locator; }
        }
    }
}
