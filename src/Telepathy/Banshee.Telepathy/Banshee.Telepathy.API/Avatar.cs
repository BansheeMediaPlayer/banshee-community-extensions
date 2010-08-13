//
// Avatar.cs
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
using System.IO;
using System.Threading;

using Banshee.Telepathy.API.DBus;

using Telepathy;

namespace Banshee.Telepathy.API
{
    public enum AvatarState {
        NotLoaded,
        Loading,
        Loaded,
        NoAvatar
    };

    public class AvatarStateEventArgs : EventArgs
    {
        public AvatarStateEventArgs (AvatarState state)
        {
            this.state = state;
        }

        private AvatarState state;
        public AvatarState State {
            get { return state; }
        }
    }

    public class Avatar : IDisposable
    {
        private IAvatars avatars;
        private Thread save_thread;
        private Thread delete_thread;

        public event EventHandler <AvatarStateEventArgs> StateChanged;
        public event EventHandler <AvatarStateEventArgs> Loaded;

        private Avatar ()
        {
            Initialize ();
        }

        internal protected Avatar (Contact contact) : this ()
        {
            this.contact = contact;

            avatars = DBusUtility.GetProxy <IAvatars> (Connection.Bus, Connection.BusName, Connection.ObjectPath);
        }

        private Contact contact;
        public Contact Contact {
            get { return contact; }
            protected set {
                if (value == null) {
                    throw new ArgumentNullException ("contact");
                }
                contact = value;
            }
        }

        public Connection Connection {
            get { return contact.Connection; }
        }

        private AvatarState state = AvatarState.NotLoaded;
        public AvatarState State {
            get { return state; }
            protected set {
                if (value < AvatarState.NotLoaded || value > AvatarState.NoAvatar) {
                    throw new ArgumentOutOfRangeException ("state");
                }

                var tmp_state = state;
                if (tmp_state != value) {
                    state = value;
                    Gtk.Application.Invoke ( delegate {
                        OnStateChanged (new AvatarStateEventArgs (value));
                    });
                }
            }
        }

        private byte [] avatar_data;
        public byte [] Image {
            get { return avatar_data; }
            protected set { avatar_data = value; }
        }

        private bool cache = true;
        public bool Cache {
            get { return cache; }
            set { cache = value; }
        }

        private bool cached = false;
        public bool IsCached {
            get { return cached; }
            protected set { cached = value; }
        }

        public string Filename {
            get { return "avatar.jpg"; }
        }

        public string CacheDirectory {
            get { return Connection.CacheDirectory != null ? Path.Combine (Path.Combine (Connection.CacheDirectory, "avatars"), Contact.Name) : null; }
        }

        protected virtual void Initialize ()
        {
        }

        private void CacheImage (byte [] data)
        {
            if (CacheDirectory == null) {
                return;
            }
            else if (data == null) {
                return;
            }

            if (!Directory.Exists (CacheDirectory)) {
                Directory.CreateDirectory (CacheDirectory);
            }

            save_thread = new Thread (new ThreadStart (delegate {
                if (delete_thread != null && delete_thread.IsAlive) {
                    delete_thread.Abort ();
                }

                try {
                    using (FileStream fs = new FileStream (Path.Combine (CacheDirectory, Filename), FileMode.Create)) {
                        fs.Write (data, 0, data.Length);
                        fs.Flush ();
                        fs.Close ();
                        cached = true;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine (e.ToString ());
                    cached = false;
                }
            }));

            save_thread.IsBackground = true;
            save_thread.Name = "Avatar.CacheImage";
            save_thread.Start ();
        }

        private bool LoadFromCache ()
        {
            if (CacheDirectory == null) {
                return false;
            }

            string path = Path.Combine (CacheDirectory, Filename);
            if (!File.Exists (path)) {
                return false;
            }
            else {
                State = AvatarState.Loading;
            }

            Thread thread = new Thread (new ThreadStart (delegate {
                try {
                    using (BinaryReader reader = new BinaryReader (new FileStream (path, FileMode.Open))) {
                        using (MemoryStream ms = new MemoryStream ()) {
                           byte [] data = new byte[8192];

                            while (reader.Read (data, 0, data.Length) > 0) {
                                ms.Write (data, 0, data.Length);
                            }

                            Image = ms.ToArray ();

                            State = AvatarState.Loaded;
                            Gtk.Application.Invoke (delegate {
                                OnLoaded (new AvatarStateEventArgs (State));
                            });
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine (e.ToString ());
                    State = AvatarState.NotLoaded;
                }
            }));

            thread.IsBackground = true;
            thread.Name = "Avatar.LoadFromCache";
            thread.Start ();

            return true;
        }

        public void Load ()
        {
            Load (true);
        }

        public void Load (bool from_cache)
        {
            bool retreiving = false;
            if (cached && from_cache) {
                 retreiving = LoadFromCache ();
            }

            try {
                if (!retreiving) {
                    uint [] handles = { Contact.Handle };

                    State = AvatarState.Loading;

                    IDictionary <uint, string> tokens = avatars.GetKnownAvatarTokens (handles);
                    if (tokens.Count == 0) {
                        State = AvatarState.NoAvatar;
                    }
                    else {
                        avatars.AvatarRetrieved += OnAvatarRetrieved;
                        avatars.RequestAvatars (handles);
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());
                State = AvatarState.NoAvatar;
            }
        }

        public void Clear ()
        {
            Clear (true);
        }

        public virtual void Clear (bool delete)
        {
            Image = null;
            State = AvatarState.NotLoaded;

            if (delete) {
                DeleteIfCached ();
            }
        }

        private void DeleteIfCached ()
        {
            if (CacheDirectory == null) {
                return;
            }

            string dir = CacheDirectory;
            delete_thread = new Thread (new ThreadStart (delegate {
                if (save_thread != null && save_thread.IsAlive) {
                    save_thread.Abort ();
                }

                try {
                    string path = Path.Combine (dir, Filename);
                    if (File.Exists (path)) {
                        File.Delete (path);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine (e.ToString());
                }
                finally {
                    cached = false;
                }
            }));

            delete_thread.IsBackground = true;
            delete_thread.Name = "Avatar.DeleteIfCached";
            delete_thread.Start ();
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                if (avatars != null) {
                    try {
                        avatars.AvatarRetrieved -= OnAvatarRetrieved;
                    }
                    catch (Exception) {}
                }

                Clear ();

                avatars = null;
                contact = null;
            }
        }

        protected virtual void OnStateChanged (AvatarStateEventArgs args)
        {
            EventHandler <AvatarStateEventArgs> handler = StateChanged;
            if (handler != null) {
                handler (this, args);
            }
        }

        protected virtual void OnLoaded (AvatarStateEventArgs args)
        {
            EventHandler <AvatarStateEventArgs> handler = Loaded;
            if (handler != null) {
                handler (this, args);
            }
        }

        private void OnAvatarRetrieved (uint handle, string token, byte [] avatar, string type)
        {
            Console.WriteLine ("OnAvatarRetrieved handle {0} token {1} byte.length {2} type {3}",
                               handle,
                               token,
                               avatar.Length,
                               type);

            if (handle == Contact.Handle) {
                Image = avatar;

                if (cache) {
                    CacheImage (avatar);
                }

                State = AvatarState.Loaded;
                OnLoaded (new AvatarStateEventArgs (State));
            }
        }
    }
}