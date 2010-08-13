//
// TransferManagerUi.cs
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

using Mono.Addins;

using Hyena.Jobs;
using Banshee.ServiceStack;

using Banshee.Telepathy.Data;

namespace Banshee.Telepathy.Gui
{
    public abstract class TransferManagerUi : IDisposable
    {
        private UserJob user_job = null;
        private readonly object sync = new object ();

        protected TransferManagerUi ()
        {
            Initialize ();
        }

        private string title = AddinManager.CurrentLocalizer.GetString ("Transfer(s) to Contacts");
        public string Title {
            get { return title; }
            protected set { title = value; }
        }

        private string cancel_message = AddinManager.CurrentLocalizer.GetString (
            "File transfers are in progress. Would you like to cancel them?");
        public string CancelMessage {
            get { return cancel_message; }
            protected set { cancel_message = value; }
        }

        private string progress_message = AddinManager.CurrentLocalizer.GetString ("Transferring {0} of {1}");
        public string ProgressMessage {
            get { return progress_message; }
            set { progress_message = value; }
        }

        private int total = 0;
        public int Total {
            get { return total; }
            protected set { total = value; }
        }

        private int in_progress = 0;
        public int InProgress {
            get { return in_progress; }
            protected set { in_progress = value; }
        }

        private long bytes_transferred = 0;
        public long BytesTransferred {
            get { return bytes_transferred; }
            protected set { bytes_transferred = value < 0 ? 0 : value; }
        }

        private long bytes_expected = 0;
        public long BytesExpected {
            get { return bytes_expected; }
            protected set { bytes_expected = value < 0 ? 0 : value; }
        }

        private bool cancelling = false;
        public bool Cancelling {
            get { return cancelling; }
            protected set { cancelling = value; }
        }

        protected virtual void Initialize ()
        {
            ResetCounters ();
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                DestroyUserJob ();
            }
        }

        protected void ResetCounters ()
        {
            BytesExpected = 0;
            BytesTransferred = 0;
            InProgress = 0;
            Total = 0;
        }

        protected void CreateUserJob ()
        {
            lock (sync) {
                if (user_job != null) {
                    return;
                }

                user_job = new UserJob (Title, AddinManager.CurrentLocalizer.GetString ("Initializing"));
                user_job.SetResources (Resource.Cpu, Resource.Disk);
                user_job.PriorityHints = PriorityHints.SpeedSensitive | PriorityHints.DataLossIfStopped;
                user_job.IconNames = new string [] { Gtk.Stock.Network };
                user_job.CancelMessage = CancelMessage;
                user_job.CanCancel = true;
                user_job.CancelRequested += OnCancelRequested;
                user_job.Register ();
            }
        }

        protected void DestroyUserJob ()
        {
            lock (sync) {
                if (user_job == null) {
                    return;
                }

                ResetCounters ();

                user_job.CancelRequested -= OnCancelRequested;
                user_job.Finish ();
                user_job = null;
            }
        }

        protected void Update ()
        {
            CreateUserJob ();

            lock (sync) {
                user_job.Progress = BytesExpected != 0 ? (double) BytesTransferred / (double) BytesExpected : 0;
                user_job.Status = String.Format (ProgressMessage, InProgress, Total);
            }
        }

        public abstract void CancelAll ();


        protected virtual void OnUpdated (object sender, UpdatedEventArgs args)
        {
            BytesExpected = args.BytesExpected;
            BytesTransferred = args.BytesTransferred;
            InProgress = args.InProgress;
            Total = args.Total;

            Update ();
        }

        protected virtual void OnCompleted (object sender, EventArgs args)
        {
            DestroyUserJob ();
        }

        protected virtual void OnCancelRequested (object sender, EventArgs args)
        {
            Cancelling = true;

            CancelAll ();
            DestroyUserJob ();

            Cancelling = false;
        }
    }
}