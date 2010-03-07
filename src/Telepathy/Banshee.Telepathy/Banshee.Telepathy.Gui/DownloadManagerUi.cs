//
// DownloadManagerUi.cs
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

using Mono.Unix;

using Hyena;
using Hyena.Jobs;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Telepathy.Data;

namespace Banshee.Telepathy.Gui
{
    public class DownloadManagerUi : TransferManagerUi
    {
        //private TelepathyService service = null;
        private readonly TelepathyDownloadManager download_manager = new TelepathyDownloadManager ();
		private Banshee.Library.LibraryImportManager import_manager = null;


        public DownloadManagerUi () : base ()
        {
            //this.service = service;
            Title = Catalog.GetString ("Download(s) from Contacts");
            CancelMessage = Catalog.GetString ("Downloads are in progress. Would you like to cancel them?");

            download_manager.Updated += OnUpdated;
            download_manager.Completed += OnCompleted;
            download_manager.TransferCompleted += OnTransferCompleted;
			
			import_manager = new Banshee.Library.LibraryImportManager (true);
        }

        public TelepathyDownloadManager DownloadManager {
            get { return download_manager; }
        }
        
        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                download_manager.TransferCompleted -= OnTransferCompleted;
                download_manager.Completed -= OnCompleted;
                download_manager.Updated -= OnUpdated;
                download_manager.Dispose ();
            }
        }

        private void ImportTrack (string path)
        {
            //Banshee.Library.LibraryImportManager import_manager = ServiceManager.Get <Banshee.Library.LibraryImportManager> ();

            if (import_manager.ImportTrack (path) != null) {
                import_manager.NotifyAllSources ();
            }
        }
        
        public override void CancelAll ()
        {
            download_manager.CancelAll ();
        }
        
        private void OnTransferCompleted (object sender, EventArgs args)
        {
            TelepathyDownload transfer = sender as TelepathyDownload;
            if (transfer == null) {
                return;
            }
            
            ImportTrack (transfer.FileTransfer.Filename);
        }
    }
}
