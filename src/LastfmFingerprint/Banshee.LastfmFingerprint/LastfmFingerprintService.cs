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
using Gtk;
using Mono.Addins;

using Hyena;
using Hyena.Jobs;
using Hyena.Json;
using Lastfm;
using Banshee.Collection;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;
using System.Threading;

namespace Banshee.LastfmFingerprint
{
    public class LastfmFingerprintService : IExtensionService, IDisposable
    {
        private InterfaceActionService uia_service;
        private ActionGroup actions;
        private uint ui_manager_id;

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
            uia_service = ServiceManager.Get<InterfaceActionService> ();

            actions = new ActionGroup ("LastfmFingerprint");

            actions.Add (new ActionEntry [] {
                new ActionEntry ("FingerprintAction", null,
                    AddinManager.CurrentLocalizer.GetString ("Get Information From Track Fingerprint"), null,
                    AddinManager.CurrentLocalizer.GetString ("Get track information from last.fm acoustic fingerprint"),
                    OnGetTagFromFingerprint)
            });


            //TODO icon of fingerprint
            //Gtk.Action action = uia_service.TrackActions["FingerprintAction"];
            //action.IconName = "fingerprint";

            uia_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");

            UpdateActions ();
            ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
        }

        public void Dispose ()
        {
            ServiceManager.SourceManager.ActiveSourceChanged -= HandleActiveSourceChanged;

            uia_service.UIManager.RemoveUi (ui_manager_id);
            uia_service.UIManager.RemoveActionGroup (actions);
        }

        private void OnGetTagFromFingerprint (object sender, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;

            UserJob job = new UserJob (AddinManager.CurrentLocalizer.GetString ("Getting sound fingerprint"));
            job.SetResources (Resource.Cpu, Resource.Disk, Resource.Database);
            job.PriorityHints = PriorityHints.SpeedSensitive;
            job.Status = AddinManager.CurrentLocalizer.GetString ("Scanning...");
            job.IconNames = new string [] { "system-search", "gtk-find" };
            job.Register ();

            ThreadPool.QueueUserWorkItem (delegate {
            try {

                var selection = ((ITrackModelSource)source).TrackModel.Selection;
                int total = selection.Count;
                int count = 0;

                foreach (TrackInfo track in ((ITrackModelSource)source).TrackModel.SelectedItems) {

                    AudioDecoder ad = new AudioDecoder(track.SampleRate, (int)track.Duration.TotalSeconds, track.ArtistName, track.AlbumTitle,
                                                      track.TrackTitle, track.TrackNumber, track.Year, track.Genre);

                    int fpid = ad.Decode (track.Uri.AbsolutePath);
                    Log.DebugFormat ("Last.fm fingerprint id for {0} is {1}", track.TrackTitle, fpid);

                    if (fpid != 0)
                        FetchMetadata (track, fpid);
                    else
                    {
                        Log.WarningFormat ("Could not find fingerprint id for the track {0} !", track.TrackTitle);
                    }

                    job.Progress = (double)++count / (double)total;
                }

            } catch (Exception e) {
                Log.Exception (e);
            } finally {
                job.Finish ();
            }
            });
        }

        void FetchMetadata (TrackInfo track, int fpid)
        {


            var request = new LastfmRequest ("track.getFingerprintMetadata");
            request.AddParameter ("fingerprintid", fpid.ToString ());
            request.Send ();

            var response = request.GetResponseObject ();
            if (response == null)
                return;
            var json_tracks = (JsonObject)response["tracks"];
            var obj_track = json_tracks["track"];
            JsonObject json_track = null;

            Log.Debug ("obj_track:" + obj_track.ToString ());

            if (obj_track is JsonArray)
                json_track = (JsonObject) (((JsonArray)obj_track)[0]);
            else if (obj_track is JsonObject)
                json_track = (JsonObject)obj_track;

            if (json_track !=null) {
                track.TrackTitle = (string)json_track["name"];
                track.MusicBrainzId = (string)json_track["mbid"];
                track.MoreInfoUri = new SafeUri ((string)json_track["url"]);

                var json_artist = (JsonObject)json_track["artist"];
                if (json_artist != null) {
                    track.ArtistName = (string)json_artist["name"];
                    track.ArtistMusicBrainzId = (string)json_artist["mbid"];
                }
                // TODO Get cover from URL in json_track["image"] array
                // Maybe reuse MetadataServiceJob.SaveHttpStreamCover to fetch it
                // but it's protected...

                track.Save ();

                if (track == ServiceManager.PlayerEngine.CurrentTrack)
                    ServiceManager.PlayerEngine.TrackInfoUpdated ();
            }
        }


        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            UpdateActions ();
        }

        void UpdateActions ()
        {
            ThreadAssist.ProxyToMain (delegate {
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

                actions.Sensitive = true;
                actions.Visible = true;
            });
        }
    }
}

