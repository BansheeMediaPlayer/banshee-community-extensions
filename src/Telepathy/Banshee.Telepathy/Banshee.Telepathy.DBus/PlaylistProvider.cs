//
// PlaylistProvider.cs
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
using System.Threading;
using System.Collections.Generic;

using Banshee.ServiceStack;

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

using Hyena.Data.Sqlite;

using DBus;

namespace Banshee.Telepathy.DBus
{
    public class PlaylistProvider : BaseProvider, IPlaylistProvider
    {
        private static object class_lock = new object ();
        private static int instance_count = 0;
        private int myindex = 0;
        //private MetadataProviderService myservice;
        private DBusActivity activity;

        public PlaylistProvider (DBusActivity activity, int id) : base ()
        {
            lock (class_lock) {
                instance_count++;
                myindex = instance_count;
            }

            //myservice = service;
            this.activity = activity;
            Id = id;
        }

        public string GetName ()
        {
            string name = ServiceManager.DbConnection.Query<string> (
                "SELECT Name FROM CorePlaylists WHERE PlaylistID = ?", Id
            );

            return name ?? "";

        }

        public override void GetChunk (long timestamp, int sequence_num)
        {

            IDictionary <string, object> [] dict = {};

            lock (timestamp_lock) {
                if (timestamp == CurrentTimestamp && buffer != null) {
                    if (buffer.ContainsKey (sequence_num)) {
                        dict =  buffer[sequence_num];
                    }
                }
            }

            OnSingleChunkReady (ObjectPath, dict, timestamp, sequence_num);

        }

        public override void GetChunks (int chunk_size)
        {
            // mark as critical region to prevent consecutive calls from mucking things up
            lock (payload_lock) {

                long timestamp;

                lock (timestamp_lock) {
                    if (UseBuffer) {
                        buffer = new Dictionary <int, IDictionary <string, object> []> ();
                    }
                    timestamp = DateTime.Now.Ticks;
                    CurrentTimestamp = timestamp;
                }

                int total = ServiceManager.DbConnection.Query<int> (
                    "SELECT COUNT(*) FROM CorePlaylistEntries WHERE PlaylistID = ?", Id
                );

                if (total == 0) {
                    IDictionary <string, object> [] empty = {};         // d-bus no like nulls
                    OnChunkReady (ObjectPath, empty, timestamp, 1, 0);
                    return;
                }

                chunk_size = chunk_size < 1 ? 100 : chunk_size;         // default chunk_size

                HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query (
                    "SELECT TrackID FROM CorePlaylistEntries WHERE PlaylistID = ?", Id)
                );

                // deliver data asynchronously via signal in chunks of chunk_size
                // this should make things look like they are happening quickly over our tube
                int sequence_num = 1;

                for (int i = 0; i < total; i += chunk_size) {
                    int dict_size = (total - i) < chunk_size ? (total - i) : chunk_size;
                    IDictionary <string, object> [] dict  = new Dictionary <string, object> [dict_size];

                    for (int j = 0; j < dict.Length; j++) {
                        dict[j] = new Dictionary <string, object> ();
                        if (reader.Read ()) {
                            dict[j].Add ("TrackID", reader.Get <int> (0));
                        }
                    }

                    if (UseBuffer) {
                        buffer.Add (sequence_num, dict);
                    }

                    OnChunkReady (ObjectPath, dict, timestamp, sequence_num++, total);

                }
            }
        }

        void IPlaylistProvider.GetChunks (int chunk_size)
        {
            ThreadPool.QueueUserWorkItem ( delegate { GetChunks (chunk_size); } );
        }

        void IPlaylistProvider.Destroy ()
        {
            activity.UnRegisterDBusObject (ObjectPath);
            Dispose ();
        }

        protected override void Dispose (bool disposing)
        {
            //instance_count--;
            base.Dispose (disposing);
        }

        public static string BusName {
            get { return "org.bansheeproject.Banshee"; }
        }

        public string ObjectPath {
            get { return "/org/bansheeproject/PlaylistProvider_" + myindex; }
        }
    }
}
