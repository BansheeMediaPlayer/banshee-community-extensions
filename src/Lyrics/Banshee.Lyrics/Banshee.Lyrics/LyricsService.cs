//
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
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
using System.IO;
using System.Threading;

using Gtk;

using Mono.Addins;

using Banshee.Gui;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

using Banshee.Lyrics.Gui;
using Banshee.Lyrics.IO;
using Banshee.Lyrics.Sources;

using Hyena;
using Hyena.Jobs;

namespace Banshee.Lyrics
{
    public class LyricsService: IExtensionService, IDisposable
    {
        private static string lyrics_dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar +
                ".cache" + Path.DirectorySeparatorChar + "banshee-1" + Path.DirectorySeparatorChar +
                "extensions" + Path.DirectorySeparatorChar + "lyrics" + Path.DirectorySeparatorChar;

        private LyricsWindow window = new LyricsWindow ();

        private uint ui_manager_id;
        private ActionGroup lyrics_action_group;

        private LyricsDownloadJob job;

        public static String LyricsDir {
            get { return lyrics_dir; }
        }

        void IExtensionService.Initialize ()
        {
            /* check if the lyrics download table exists */
           if (!ServiceManager.DbConnection.TableExists ("LyricsDownloads")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE LyricsDownloads (
                        TrackID     INTEGER UNIQUE,
                        Downloaded  BOOLEAN
                    )");
            }

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEngineEventChanged,
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream |
                PlayerEvent.TrackInfoUpdated);

            ServiceManager.PlayerEngine.ConnectEvent (window.OnPlayerEngineEventChanged,
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream |
                PlayerEvent.TrackInfoUpdated);

            InstallInterfaceActions ();

            /* create the lyrics dir if needed */
            if (!Directory.Exists (lyrics_dir)) {
                Directory.CreateDirectory (lyrics_dir);
            }

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }

            /*get the lyric of the current played song and update the window */
            if (ServiceManager.PlayerEngine.CurrentTrack != null) {
                FetchLyrics(ServiceManager.PlayerEngine.CurrentTrack);
            }
        }

        private bool ServiceStartup ()
        {
            if (ServiceManager.SourceManager.MusicLibrary == null) {
                return false;
            }

            InitializeSource ();

            return true;
        }

        private void InitializeSource ()
        {
            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;
        }

        public void Dispose ()
        {
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEngineEventChanged);
            ServiceManager.PlayerEngine.DisconnectEvent (window.OnPlayerEngineEventChanged);

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;

            if (job != null) {
                ServiceManager.JobScheduler.Cancel (job);
            }
            InterfaceActionService actions_service = ServiceManager.Get<InterfaceActionService> ();
            actions_service.RemoveActionGroup (lyrics_action_group);
            actions_service.UIManager.RemoveUi (ui_manager_id);
            lyrics_action_group = null;

            this.window.Hide ();
        }

        string IService.ServiceName {
            get { return "LyricsService"; }
        }

        private void OnPlayerEngineEventChanged (PlayerEventArgs args)
        {
            if (args.Event == PlayerEvent.EndOfStream) {
                lyrics_action_group.GetAction ("ShowLyricsAction").Sensitive = false;
                return;
            }
            lyrics_action_group.GetAction ("ShowLyricsAction").Sensitive = true;

            FetchLyrics (ServiceManager.PlayerEngine.CurrentTrack);
        }

        public void FetchLyrics (TrackInfo track)
        {
            LyricsManager.Instance.FetchLyrics (track);
        }
        public void SaveLyrics (TrackInfo track, string lyrics)
        {
            LyricsManager.Instance.SaveLyrics (track, lyrics, true);
        }

        private void InstallInterfaceActions ()
        {
            InterfaceActionService action_service = ServiceManager.Get<InterfaceActionService> ();

            lyrics_action_group = new ActionGroup ("Lyrics");

            lyrics_action_group.Add (new ActionEntry[] {
                new ActionEntry ("LyricsAction", null,
                    AddinManager.CurrentLocalizer.GetString ("L_yrics"), null,
                    AddinManager.CurrentLocalizer.GetString ("Manage Lyrics"), null),
                new ActionEntry ("FetchLyricsAction", null,
                    AddinManager.CurrentLocalizer.GetString ("_Download Lyrics"), null,
                    AddinManager.CurrentLocalizer.GetString ("Download lyrics for all tracks"), OnFetchLyrics)
            });

            lyrics_action_group.Add (new ToggleActionEntry[] {
                new ToggleActionEntry ("ShowLyricsAction", null,
                            AddinManager.CurrentLocalizer.GetString ("Show Lyrics"), "<control>T",
                            AddinManager.CurrentLocalizer.GetString ("Show Lyrics in a separate window"), null, false) });

            lyrics_action_group.GetAction ("ShowLyricsAction").Activated += OnToggleWindow;
            lyrics_action_group.GetAction ("ShowLyricsAction").Sensitive = ServiceManager.PlayerEngine.CurrentTrack != null ? true : false;

            action_service.AddActionGroup (lyrics_action_group);

            ui_manager_id = action_service.UIManager.AddUiFromResource ("LyricsMenu.xml");
        }

        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            /*do not force all lyrics to be refreshed.*/
            FetchAllLyrics (false);
        }

        private void OnFetchLyrics (object o, EventArgs args)
        {
            /*force all lyrics to be refreshed.*/
            FetchAllLyrics (true);
        }

        private void FetchAllLyrics (bool force)
        {
            /*check that there is no job and the netowrk is up */
            if (job != null || !ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                return;
            }

            job = new LyricsDownloadJob (force);
            job.Finished += delegate {
                job = null;
            };

            job.Start ();
        }

        private void OnToggleWindow (object o, EventArgs args)
        {
            ToggleAction action = (ToggleAction)o;
            if (action.Active) {
                window.ForceUpdate ();
                window.Show ();
                window.Present ();
            } else {
                window.Hide ();
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }
    }
}