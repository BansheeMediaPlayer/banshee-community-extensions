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
using Gtk;
using System.IO;
using Mono.Unix;

using System.Threading;

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

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

        private SimpleAsyncJob job;

        public static String LyricsDir {
            get { return lyrics_dir; }
        }

        void IExtensionService.Initialize ()
        {
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEngineEventChanged,
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream |
                PlayerEvent.TrackInfoUpdated);

            ServiceManager.PlayerEngine.ConnectEvent (window.OnPlayerEngineEventChanged, 
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream |
                PlayerEvent.TrackInfoUpdated);

            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;

            InstallInterfaceActions ();

            /* create the lyrics dir if needed */
            if (!Directory.Exists (lyrics_dir)) {
                Directory.CreateDirectory (lyrics_dir);
            }

            /*get the lyric of the current played song and update the window */
            if (ServiceManager.PlayerEngine.CurrentTrack != null) {
                Thread t = new Thread (new ThreadStart (LyricsManager.Instance.GetLyrics));
                t.Start ();
                return;
            }
        }
        
        public void Dispose ()
        {
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEngineEventChanged);
            ServiceManager.PlayerEngine.DisconnectEvent (window.OnPlayerEngineEventChanged);

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;

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
            /*
            if (args.Event != PlayerEvent.StartOfStream && args.Event != PlayerEvent.TrackInfoUpdated) {
                return;
            }*/

            lyrics_action_group.GetAction ("ShowLyricsAction").Sensitive = true;

            //Get the lyrics for the current track
            Thread t = new Thread (new ThreadStart (LyricsManager.Instance.GetLyrics));
            t.Start ();
        }
        
        private void InstallInterfaceActions ()
        {
            InterfaceActionService action_service = ServiceManager.Get<InterfaceActionService> ();
            
            lyrics_action_group = new ActionGroup ("Lyrics");

            lyrics_action_group.Add (new ActionEntry[] {
                new ActionEntry ("LyricsAction", null, 
                    Catalog.GetString ("L_yrics"), null, 
                    Catalog.GetString ("Manage Lyrics"), null),
                new ActionEntry ("FetchLyricsAction", null, 
                    Catalog.GetString ("_Download Lyrics"), null, 
                    Catalog.GetString ("Download lyrics for all tracks"), OnFetchLyrics)
            });

            lyrics_action_group.Add (new ToggleActionEntry[] {
                new ToggleActionEntry ("ShowLyricsAction", null, 
                            Catalog.GetString ("Show Lyrics"), "<control>T",
                            Catalog.GetString ("Show Lyrics in a separate window"), null, false) });

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
            /*do not force all lyrics to be refreshed.*/
            FetchAllLyrics (true);
        }

        private void FetchAllLyrics (bool force_refresh)
        {
            /*check if the netowrk is up */            
            if (job != null || !ServiceManager.Get<Banshee.Networking.Network> ().Connected) {
                return;
            }
            
            job = new LyricsDownloadJob (force_refresh);
            job.Finished += delegate {
                job = null;
            };

            ServiceManager.JobScheduler.Add (job);
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
    }
}