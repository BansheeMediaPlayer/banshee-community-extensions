//
// JamendoDownloadManager.cs
//
// Author:
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
//
// Copyright (c) 2010 Bertrand Lorentz
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;
using Mono.Addins;

using Hyena;
using Hyena.Downloader;

using Banshee.Library;
using Banshee.ServiceStack;

namespace Banshee.Jamendo
{
    public class JamendoDownloadManager : DownloadManager
    {
        private string mimetype;
        private DownloadManagerJob job;
        private LibraryImportManager import_manager;

        public static void Download (string remote_uri, string mimetype)
        {
            try {
                new JamendoDownloadManager (remote_uri, mimetype);
                Log.Information ("Downloading from Jamendo", remote_uri);
            } catch (Exception e) {
                Log.Exception ("Invalid Jamendo file: " + remote_uri, e);
                Log.Error ("Invalid Jamendo downloader file", remote_uri, true);
            }
        }

        public JamendoDownloadManager (string remote_uri, string mimetype)
        {
            this.mimetype = mimetype;
            job = new DownloadManagerJob (this) {
                Title = AddinManager.CurrentLocalizer.GetString ("Jamendo Downloads"),
                Status = AddinManager.CurrentLocalizer.GetString ("Contacting..."),
                IconNames = new string [] { "jamendo" },
                CanCancel = true
            };
            job.Finished += delegate { ServiceManager.SourceManager.MusicLibrary.NotifyUser (); };

            ServiceManager.Get<JobScheduler> ().Add (job);

            import_manager = new LibraryImportManager (true) {
                KeepUserJobHidden = true,
                Debug = true,
                Threaded = false
            };

            var downloader = new HttpFileDownloader () {
                Uri = new Uri (remote_uri),
                TempPathRoot = Path.Combine (Path.GetTempPath (), "banshee-jamendo-downloader"),
                FileExtension = mimetype == "application/zip" ? "zip" : "mp3"
            };
            job.CancelRequested += delegate { downloader.Abort (); };
            QueueDownloader (downloader);
        }

        protected override void OnDownloaderStarted (HttpDownloader downloader)
        {
            base.OnDownloaderStarted (downloader);
            Log.InformationFormat ("Starting to download {0}", downloader.Name);
        }

        protected override void OnDownloaderFinished (HttpDownloader downloader)
        {
            var file_downloader = (HttpFileDownloader)downloader;
            if (job.IsCancelRequested) {
                Log.InformationFormat ("Cancelled download {0}", file_downloader.LocalPath);
                File.Delete (file_downloader.LocalPath);
                base.OnDownloaderFinished (downloader);
                return;
            }

            Log.InformationFormat ("Finished downloading {0}", file_downloader.LocalPath);
            job.Status = AddinManager.CurrentLocalizer.GetString ("Importing...");
            job.Progress = 0.0;

            if (mimetype == "application/zip") {
                string unzip_dir = String.Concat (file_downloader.LocalPath, Path.GetRandomFileName ());
                Directory.CreateDirectory (unzip_dir);

                using (ZipInputStream s = new ZipInputStream (File.OpenRead (file_downloader.LocalPath))) {
                    ZipEntry zip_entry;
                    while ((zip_entry = s.GetNextEntry ()) != null) {
                        string filename = Path.GetFileName (zip_entry.Name);
                        string extracted_file = Paths.Combine (unzip_dir, filename);

                        if (filename != String.Empty) {
                            using (FileStream streamWriter = File.Create (extracted_file)) {
                                int size = 2048;
                                byte[] data = new byte[size];
                                while (true) {
                                    size = s.Read (data, 0, data.Length);
                                    if (size > 0) {
                                        streamWriter.Write (data, 0, size);
                                    } else {
                                        break;
                                    }
                                }
                            }
                            Log.DebugFormat ("Unzipped {0}", extracted_file);
                            import_manager.Enqueue (extracted_file);
                        }
                    }
                }
                Directory.Delete (unzip_dir, true);
            } else {
                if (import_manager.ImportTrack (file_downloader.LocalPath) != null) {
                    import_manager.NotifyAllSources ();
                }
            }
            File.Delete (file_downloader.LocalPath);

            base.OnDownloaderFinished (downloader);
        }
    }
}

