//
// TelepathyDownloadManager.cs
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

namespace Banshee.Telepathy.Data
{
    public class TelepathyDownloadManager : TransferManager<TelepathyDownloadKey, TelepathyDownload>
    {
		private readonly IDictionary<TelepathyDownloadKey, TelepathyDownload> cancelled = new Dictionary<TelepathyDownloadKey, TelepathyDownload> ();
		
		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (TelepathyDownload d in new List<TelepathyDownload> (cancelled.Values)) {
					if (d != null) {
						d.Dispose ();
					}
				}
				cancelled.Clear ();
			}
			
			base.Dispose (disposing);
		}
		
        protected override void CleanUpTransfer (TelepathyDownload t, bool dispose)
        {
            if (!t.CancelPending) {
				cancelled.Remove (t.Key);
                base.CleanUpTransfer (t, dispose);
            } else {
				// if cancel_pending, the file transfer channel was not yet
				// available to cancel, so continue to keep track so when it
				// comes across it can be cancelled
				base.CleanUpTransfer (t, false);
				cancelled[t.Key] =  t;
			}
        }
		
		public override void Queue (TelepathyDownload t)
		{
			if (cancelled.ContainsKey (t.Key)) {
				TelepathyDownload old = cancelled[t.Key];
				if (old != null) {
					old.Dispose ();
				}
				cancelled.Remove (t.Key);
			}
			base.Queue (t);
		}
    }
}