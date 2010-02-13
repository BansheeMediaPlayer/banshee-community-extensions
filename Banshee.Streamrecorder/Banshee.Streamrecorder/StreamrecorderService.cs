//
// StreamrecorderService.cs
//
// Author:
//   Frank Ziegler
//   based on Banshee-Streamripper by Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
// Copyright (C) 2009 Frank Ziegler
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
using System.Text.RegularExpressions;

using Mono.Unix;
using Mono.Addins;

using Gtk;

using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Gui;
using Banshee.Configuration;
using Banshee.MediaEngine;

using Hyena;

namespace Banshee.Streamrecorder
{
    public class StreamrecorderService : IExtensionService, IDisposable
    {
        private StreamrecorderProcessControl streamrecorder_process;
        private ActionGroup actions;
        private InterfaceActionService action_service;
        private uint ui_manager_id;
        private bool recording = false;
        private string output_directory;
        private bool is_importing_enabled = true;
		private TrackInfo track = null;

        
        public StreamrecorderService ()
        {
            Hyena.Log.Debug ("[StreamrecorderService] <StreamrecorderService> START");
            
            recording = IsRecordingEnabledEntry.Get ().Equals ("True") ? true : false;
            output_directory = OutputDirectoryEntry.Get ();
            is_importing_enabled = IsImportingEnabledEntry.Get ().Equals ("True") ? true : false;
            streamrecorder_process = new StreamrecorderProcessControl();
        }
        
        void IExtensionService.Initialize ()
        {
            Hyena.Log.Debug ("[StreamrecorderService] <Initialize> START");

            ServiceManager.PlaybackController.TrackStarted += delegate {
                if (recording) {
                    StartRecording ();
                }
            };
            
            ServiceManager.PlaybackController.Stopped += delegate {
                if (recording) {
                    StopRecording ();
                }
            };
        
            ServiceManager.PlayerEngine.ConnectEvent ( OnEndOfStream , PlayerEvent.EndOfStream) ;
            ServiceManager.PlayerEngine.ConnectEvent ( OnStateChange , PlayerEvent.StateChange) ;
			ServiceManager.PlayerEngine.ConnectEvent ( OnMetadata, PlayerEvent.TrackInfoUpdated );
        
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            actions = new ActionGroup ("Streamrecorder");
            
            actions.Add (new ActionEntry [] {
                new ActionEntry ("StreamrecorderAction", null,
                    AddinManager.CurrentLocalizer.GetString ("_Streamrecorder"), null,
                    null, null),

                new ActionEntry ("StreamrecorderConfigureAction", Stock.Properties,
                    AddinManager.CurrentLocalizer.GetString ("_Configure"), null,
                    AddinManager.CurrentLocalizer.GetString ("Configure the Streamrecorder plugin"), OnConfigure)
            });
                
            actions.Add (new ToggleActionEntry [] { 
                new ToggleActionEntry ("StreamrecorderEnableAction", Stock.MediaRecord,
                    AddinManager.CurrentLocalizer.GetString ("_Activate streamrecorder"), null,
                    AddinManager.CurrentLocalizer.GetString ("Activate streamrecorder process"), OnActivateStreamrecorder, recording)
            });

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("Resources.StreamrecorderMenu.xml");

            Hyena.Log.Debug ("[StreamrecorderService] <Initialize> END");
        }

        public void OnActivateStreamrecorder (object o, EventArgs ea) 
        {
            Hyena.Log.Debug ("[StreamrecorderService] <OnActivateStreamrecorder> START");
                    
            if (!recording) { 
				StartRecording ();
            }
            else {
                StopRecording ();        
            }

            recording = !recording;
            IsRecordingEnabledEntry.Set (recording.ToString ());

            Hyena.Log.Debug ("[StreamrecorderService] <OnActivateStreamrecorder> END");
        }
        
        public void OnConfigure (object o, EventArgs ea)
        {
            new StreamrecorderConfigDialog (this, output_directory, is_importing_enabled);
        }
            
        public void Dispose ()
        {
            Log.Debug ("Disposing Streamrecorder plugin");

            StopRecording ();
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            ServiceManager.PlayerEngine.DisconnectEvent ( OnEndOfStream ) ;
            ServiceManager.PlayerEngine.DisconnectEvent ( OnStateChange ) ;
            actions = null;
        }
        
        string IService.ServiceName {
            get { return "StreamrecorderService"; }
        }

        private bool IsCurrentTrackRecordable () 
        {
            if (Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack != null
				&& Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.IsLive 
				&& Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.IsPlaying)
                return true;
            
            return false;
        }

        private void OnMetadata (PlayerEventArgs args)
        {
			TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
			streamrecorder_process.AddStreamTags(track);
        }

        private void OnEndOfStream (PlayerEventArgs args)
        {
            if (recording) {
                StopRecording ();
            }
        }

        private void OnStateChange (PlayerEventArgs args)
        {
            //Console.WriteLine (ServiceManager.PlayerEngine.CurrentState);
            //Console.WriteLine (ServiceManager.PlayerEngine.LastState);
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Idle && recording) {
                StopRecording ();
            }        
        }

        private void StartRecording () 
        {

            if (recording ) {
                StopRecording ();
            }
			
			if (!IsCurrentTrackRecordable ()) {
				 return;
			}
			
			track = ServiceManager.PlaybackController.CurrentTrack;

            if (InitStreamrecorderProcess (track)) {
				streamrecorder_process.StartRecording ();
				streamrecorder_process.AddStreamTags(track);
				
                if (is_importing_enabled)
                    StartFolderScanner ();
            }
        }

        private void StopRecording () 
        {
            streamrecorder_process.StopRecording ();

            StopFolderScanner ();
        }
     
        public void StartFolderScanner () 
        {
            RippedFileScanner.StartScanner ();
        }

        public void StopFolderScanner () 
        {
           RippedFileScanner.StopScanner ();
        }
                
        private bool InitStreamrecorderProcess (TrackInfo track_in) 
        {
            Hyena.Log.DebugFormat ("[StreamrecorderService] <InitStreamrecorderProcess> START dir: '{0}'", output_directory);
                    
            if (String.IsNullOrEmpty (output_directory)) {
                output_directory = Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary.BaseDirectory +
                    Path.DirectorySeparatorChar + "ripped";
            }
                    
            if (track_in == null) {
                Hyena.Log.Debug ("[StreamrecorderService] <InitStreamrecorderProcess> END. Recording not ready");
                return false;
            }
                  
            if (track_in.Uri == null || track_in.Uri.IsLocalPath) {
                Hyena.Log.Debug ("[StreamrecorderService] <InitStreamrecorderProcess> END. Not recording local files");
                return false;
            }

			DateTime dt = DateTime.Now;
			string datestr = String.Format("{0:d_M_yyyy_HH_mm_ss}", dt);
			string fileext = ".mp3";//Regex.Replace(track.Uri.ToString(), @"^.*(\.[^\.\/]*)$", "$1");
			//if (fileext.Equals(track.Uri.ToString())) fileext = ".mp3" ;
			string filename = track.TrackTitle + "_" + datestr + fileext;

            streamrecorder_process.SetOutputParameters (output_directory,filename);

            RippedFileScanner.SetScanDirectory (output_directory);
                    
            Hyena.Log.Debug ("[StreamrecorderService] <InitStreamrecorderProcess> END. Recording ready");
            return true;
        }
                
        public string OutputDirectory 
        {
            get { return output_directory; }
            set 
            {
                StopRecording ();
                StopFolderScanner ();
                    
                this.output_directory = value;
               
                Hyena.Log.DebugFormat ("[StreamrecorderService] <OutputDirectorySetter> ", value);

				if (String.IsNullOrEmpty (this.output_directory)) 
				{
					this.output_directory = Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary.BaseDirectory +
						Path.DirectorySeparatorChar + "ripped";
				}
				RippedFileScanner.SetScanDirectory (this.output_directory);

                if (is_importing_enabled)
                    StartFolderScanner ();

                if (recording)
                    StartRecording ();
            }
        }
         
        public bool IsImportingEnabled
        {
            get { return is_importing_enabled; }
            set { is_importing_enabled = value; }
        }
       
        public static readonly SchemaEntry<string> IsRecordingEnabledEntry = new SchemaEntry<string> (
            "plugins.streamrecorder", "is_recording_enabled", "", "Is ripping enabled", "Is ripping enabled"
        );
                
        public static readonly SchemaEntry<string> OutputDirectoryEntry = new SchemaEntry<string> (
            "plugins.streamrecorder", "output_directory", "", "Output directory for ripped files", 
            "Output directory for ripped files"
        );
                
        public static readonly SchemaEntry<string> IsImportingEnabledEntry = new SchemaEntry<string> (
            "plugins.streamrecorder", "is_importing_enabled", "", "Is importing enabled", "Is importing enabled"
        );
    }
}
