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
        private bool active;
        private AudioDecoder ad;
        private LastfmAccount account;
        //in a perfect word 400ms is enough but still get timeout and banned so increase time between last fm request
        //private TimeSpan lastFmTOSMinTimeout = TimeSpan.FromMilliseconds (1000);


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
            active = true;
            Source source = ServiceManager.SourceManager.ActiveSource;

            UserJob job = new UserJob (AddinManager.CurrentLocalizer.GetString ("Getting sound fingerprint"));
            job.SetResources (Resource.Cpu, Resource.Disk, Resource.Database);
            job.PriorityHints = PriorityHints.SpeedSensitive;
            job.Status = AddinManager.CurrentLocalizer.GetString ("Scanning...");
            job.IconNames = new string [] { "system-search", "gtk-find" };
            job.CanCancel = true;
            job.CancelRequested += HandleJobCancelRequested;
            job.Register ();

            if (account == null) {
                account = new LastfmAccount ();
                LoginDialog dialog = new LoginDialog (account, true);
                dialog.Run ();
                dialog.Destroy ();
            }

            //comment the timeout system for TOS because still have issue and not seems to be linked...
            //System.DateTime start = System.DateTime.MinValue;
            ThreadPool.QueueUserWorkItem (delegate {
                try {
    
                    var selection = ((ITrackModelSource)source).TrackModel.Selection;
                    int total = selection.Count;
                    int count = 0;
    
                    foreach (TrackInfo track in ((ITrackModelSource)source).TrackModel.SelectedItems) {
                        if (!active)
                            break;
                        if (String.IsNullOrEmpty (track.Uri.AbsolutePath))
                            continue;
                        ad = new AudioDecoder((int)track.Duration.TotalSeconds);
                        //respect last fm term of service :
                        //You will not make more than 5 requests per originating IP address per second, averaged over a 5 minute period
                        // 2 requests are done on each loop ==> time allowed by loop : 400ms
                        /*if (start != System.DateTime.MinValue) {
                            TimeSpan span = System.DateTime.Now - start;
                            if (lastFmTOSMinTimeout > span)
                                Thread.Sleep (lastFmTOSMinTimeout - span);
                        }
                        start = DateTime.Now;
                        */
                        byte[] fingerprint = ad.Decode (track.Uri.AbsolutePath);
                        FingerprintRequest request = new FingerprintRequest();
                        request.Send (track, fingerprint, account);

                        int fpid = request.GetFpId ();
                        //force GC to dispose
                        ad = null;

                        Log.DebugFormat ("Last.fm fingerprint id for {0} is {1}", track.TrackTitle, fpid);

                        if (fpid > 0) {
                            FetchMetadata (track, fpid);
                        } else {
                            Log.WarningFormat ("Could not find fingerprint id for the track {0} !", track.TrackTitle);
                        }
    
                        job.Progress = (double)++count / (double)total;
                    }

                } catch (Exception e) {
                    account = null;
                    Log.Exception (e);
                } finally {
                    job.Finish ();
                }
            });
        }

        void HandleJobCancelRequested (object sender, EventArgs e)
        {
            active = false;
            if (ad != null)
                ad.CancelDecode ();
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

                GetMoreInfo (track);

                track.Update ();

                if (track == ServiceManager.PlayerEngine.CurrentTrack)
                    ServiceManager.PlayerEngine.TrackInfoUpdated ();
            }
        }

        void GetMoreInfo (TrackInfo track)
        {
            var request = new LastfmRequest ("track.getInfo");
            request.AddParameter ("artist", track.ArtistName);
            request.AddParameter ("track", track.TrackTitle);
            request.AddParameter ("mbid", track.MusicBrainzId);

            request.Send ();

            var response = request.GetResponseObject ();
            if (response == null)
                return;
            try
            {
                var json_track = (JsonObject)response["track"];
                //track.Duration = TimeSpan.FromMilliseconds (double.Parse ((string)json_track["duration"]));
                if (!json_track.ContainsKey("album"))
                    return;

                var json_album = (JsonObject)json_track["album"];
                if (json_album != null)
                {
                    var attr = (JsonObject)json_album["@attr"];
                    int pos = 0;
                    if (int.TryParse ((string)attr["position"], out pos)) {
                        track.TrackNumber = pos;
                    }
                    track.AlbumTitle = (string)json_album["title"];
                    track.AlbumMusicBrainzId = (string)json_album["mbid"];
                    track.AlbumArtist = (string)json_album["artist"];
                }
            } catch (Exception e)
            {
                Log.DebugException (e);
                response.Dump ();
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

