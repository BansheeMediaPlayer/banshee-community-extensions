//
// TelepathyTransfer.cs
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

using Banshee.Telepathy.API;
using Banshee.Telepathy.API.Dispatchables;

using Hyena;

namespace Banshee.Telepathy.Data
{
        public class TelepathyTransferKey : IEquatable<TelepathyTransferKey>
    {
        public TelepathyTransferKey (Contact contact, string name)
        {
            Contact = contact;
            Name = name;
        }

        public Contact Contact { get; private set; }
        public string Name { get; private set; }

        public override bool Equals (object obj)
        {
            return Equals (obj as TelepathyTransferKey);
        }

        public bool Equals (TelepathyTransferKey other)
        {
            if (other == null) return false;
            return Name.Equals (other.Name) && Contact.Equals (other.Contact);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode () + Contact.GetHashCode ();
        }

        public override string ToString ()
        {
            return Name;
        }
    }

    public abstract class TelepathyTransfer<K, T> : Transfer<K> where T : FileTransfer
        where K : TelepathyTransferKey, IEquatable<K>
    {
		protected readonly object sync = new object ();
		
        public TelepathyTransfer (K key) : base (key)
        {
            Initialize ();
        }

        public Contact Contact {
            get { return Key.Contact; }
        }

        public string Name {
            get { return Key.Name; }
        }

        private bool cancel_pending = false;
        public bool CancelPending {
            get { return cancel_pending; }
            protected set { cancel_pending = value; }
        }

        private T file_transfer = null;
        public T FileTransfer {
            get {
                if (file_transfer == null) {
                    file_transfer = Contact.DispatchManager.Get <T> (Contact, Name);
                }
                return file_transfer;
            }
        }

        protected virtual void Initialize ()
        {
            if (Contact != null) {
                Contact.DispatchManager.Dispatched += OnDispatched;
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                if (Contact != null) {
                    Contact.DispatchManager.Dispatched -= OnDispatched;
                }
                UnregisterHandlers ();
            }
        }

        public override void Cancel ()
        {
            if (FileTransfer != null) {
                cancel_pending = false;
                FileTransfer.Cancel ();
            } else {
                cancel_pending = true;
            }

            base.Cancel ();
        }

        private void UnregisterHandlers ()
        {
            if (file_transfer != null) {
                file_transfer.Ready -= OnTransferReady;
                file_transfer.ResponseRequired -= OnTransferResponseRequired;
                file_transfer.TransferInitialized -= OnTransferInitialized;
                file_transfer.Closed -= OnTransferClosed;
                file_transfer = null;
            }
        }

        protected virtual void OnTransferInitialized (object sender, EventArgs args)
        {
            T transfer = sender as T;
            transfer.ProgressChanged += delegate(object o, BytesTransferredEventArgs e) {
                T ft = o as T;
                if (ft != null) {
                    Log.DebugFormat ("OnProgressChanged: {0}", e.Bytes);
                    BytesTransferred += e.Bytes;
                }
            };

            ExpectedBytes += transfer.ExpectedBytes;
        }

        protected virtual void OnTransferReady (object sender, EventArgs args)
        {
            State = TransferState.Ready;
        }

        protected virtual void OnTransferClosed (object sender, EventArgs args)
        {
            TransferClosedEventArgs transfer_args = args as TransferClosedEventArgs;

            Log.Debug (transfer_args.State.ToString ());

            switch (transfer_args.State) {
            case API.Dispatchables.TransferState.Completed:
                State = TransferState.Completed;
                break;
            case API.Dispatchables.TransferState.Cancelled:
                if (State == TransferState.Cancelled) {
                    OnStateChanged ();
                } else {
                    State = TransferState.Cancelled;
                }
                break;
            case API.Dispatchables.TransferState.Failed:
                State = TransferState.Failed;
                break;
            }

            UnregisterHandlers ();
        }

        protected abstract void OnTransferResponseRequired (object sender, EventArgs args);

        private void OnDispatched (object sender, EventArgs args)
        {
            T transfer = sender as T;
            if (transfer != null && transfer.Contact.Equals (Contact) && transfer.OriginalFilename.Equals (Name)) {
                transfer.ResponseRequired += OnTransferResponseRequired;
                transfer.TransferInitialized += OnTransferInitialized;
                transfer.Ready += OnTransferReady;
                transfer.Closed += OnTransferClosed;
				if (Contact != null) {
                    Contact.DispatchManager.Dispatched -= OnDispatched;
                }
            }
        }
    }
}