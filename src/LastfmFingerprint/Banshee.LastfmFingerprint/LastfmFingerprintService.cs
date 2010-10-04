//
// LastfmFingerprintService.cs
//
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright (c) 2010 Olivier Dufour
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
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Gui.TrackEditor;
using Gtk;
using Mono.Unix;
using System.Threading;
using Hyena.Jobs;
using Hyena;
using System.Runtime.InteropServices;
using Banshee.Collection;
using System.Net;
using System.IO;
using System.Xml;

namespace Banshee.LastfmFingerprint
{
    public class LastfmFingerprintService : IExtensionService
    {
        private bool disposed;

        string IService.ServiceName {
            get { return "LastfmFingerprintService"; }
        }

        public LastfmFingerprintService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            InterfaceInitialize ();
        }

        public void InterfaceInitialize ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            uia_service.TrackActions.Add (new ActionEntry [] {
                new ActionEntry ("FingerprintAction", null,
                    Catalog.GetString ("Get tag from song fingerprint"), null,
                    Catalog.GetString ("Get tag from lastfm acoustic fingerprint"),
                    OnGetTagFromFingerprint)
            });


            //TODO icon of fingerprint
            //Gtk.Action action = uia_service.TrackActions["FingerprintAction"];
            //action.IconName = "fingerprint";

            uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");

            UpdateActions ();
            uia_service.TrackActions.SelectionChanged += delegate { ThreadAssist.ProxyToMain (UpdateActions); };
            ServiceManager.SourceManager.ActiveSourceChanged += delegate { ThreadAssist.ProxyToMain (UpdateActions); };
        }

        private void OnGetTagFromFingerprint (object sender, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;

            UserJob job = new UserJob (Catalog.GetString ("Getting sound fingerprint"));
            job.SetResources (Resource.Cpu, Resource.Disk, Resource.Database);
            job.PriorityHints = PriorityHints.SpeedSensitive;
            job.Status = Catalog.GetString ("Scanning...");
            job.IconNames = new string [] { "system-search", "gtk-find" };
            job.Register ();

            //ThreadPool.QueueUserWorkItem (delegate {
            try {

                var selection = ((ITrackModelSource)source).TrackModel.Selection;
                int total = selection.Count;
                int count = 0;

                foreach (TrackInfo track in ((ITrackModelSource)source).TrackModel.SelectedItems) {

                    //TODO : hardcoded nchannels (stereo only )
                    AudioDecoder ad = new AudioDecoder(track.SampleRate, (int)track.Duration.TotalSeconds, 2, track.ArtistName, track.AlbumTitle,
                                                      track.TrackTitle, track.TrackNumber, track.Year, track.Genre);

                    int fpid = ad.Decode (track.Uri.AbsolutePath);
                    Console.WriteLine("fpid:{0}", fpid);
                    //TODO get metadata from id
                    FetchMetadata (track, fpid);

                    job.Progress = (double)++count / (double)total;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine (e);
            }
            finally
            {
                job.Finish ();
            }
            //});
        }

        void FetchMetadata (TrackInfo track, int fpid)
        {
            string query = string.Format("http://ws.audioscrobbler.com/2.0/?method=track.getfingerprintmetadata&fingerprintid={0}&api_key=2bfed60da64b96c16ea77adbf5fe1a82", fpid);
            WebRequest request = WebRequest.Create (query);
            WebResponse rsp =  request.GetResponse ();
            XmlDocument doc = new XmlDocument();
            /*StreamReader reader = new StreamReader(rsp.GetResponseStream ());
            string str = reader.ReadLine();
            while(str != null)
            {
                Console.WriteLine(str);
                str = reader.ReadLine();
            }*/

            //editor_track = new EditorTrackInfo (sourceTrack);
            using (Stream stream = rsp.GetResponseStream ())
            {
                //XmlTextReader reader = new XmlTextReader(Stream);
                //reader.re
                doc.Load (stream);
                //get the first track (higher rank)
                XmlNode node = doc.SelectSingleNode("/lfm/tracks/track");

                if (node.HasChildNodes) {
                    track.TrackTitle = node["name"].Value;
                    Log.DebugFormat ("title={0}", track.TrackTitle);
                    track.MusicBrainzId = node["mbid"].Value;
                    track.MoreInfoUri = new SafeUri (node["url"].Value);
    
                    //node["streamable"].Value;
                    XmlNode anode = node["artist"];
                    if (anode.HasChildNodes) {
                        track.ArtistName = anode["name"].Value;
                        track.ArtistMusicBrainzId = anode["mbid"].Value;
                        //anode["url"].Value;//more infor artiste
                    }
    
                    track.Save ();
                    ServiceManager.PlayerEngine.TrackInfoUpdated ();
                    //TODO Get cover
                    //best is to use code of MetadataServiceJob.SaveHttpStreamCover
                    //but hard to get it because protected... TODO find a way to reuse this code
                    // size = small, medium, large or extralarge
                    //node["image"].Value;//depend of size
                }
            }

        }


        void UpdateActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.Action action = uia_service.TrackActions["FingerprintAction"];

            /*Source source = ServiceManager.SourceManager.ActiveSource;
            bool sensitive = false;
            bool visible = false;

            if (source != null) {
                //TODO test MusicLibrarySource ?
                var track_source = source as ITrackModelSource;
                if (track_source != null) {
                    var selection = track_source.TrackModel.Selection;
                    visible = source.HasEditableTrackProperties;
                    sensitive = selection.Count > 0;
                }
            }

            action.Sensitive = sensitive;
            action.Visible = visible;
            */

            action.Sensitive = true;
            action.Visible = true;
        }


        void IDisposable.Dispose ()
        {
            if (disposed) {
                return;
            }

            //TODO dispose resource here

            disposed = true;
        }
    }
}

