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
using System.Collections.Generic;

using Mono.Addins;

using Gtk;

using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Gui;
using Banshee.Configuration;
using Banshee.MediaEngine;
using Banshee.Streaming;

using Hyena;
using Banshee.Sources;

namespace Banshee.Streamrecorder
{

    /// <summary>
    /// Extension Service class that adds the functionality to Banshee Media Player to record live (non-local) streams to files
    /// </summary>
    public class StreamrecorderService : IExtensionService, IDisposable, IDelayedInitializeService
    {
        private Recorder recorder;
        private ActionGroup actions;
        private InterfaceActionService action_service;
        private uint ui_menu_id;
        private uint ui_button_id;
        private bool recording = false;
        private string output_directory;
        private bool is_importing_enabled = true;
        private bool is_splitting_enabled = false;
        private TrackInfo track = null;
        private string active_encoder;

        /// <summary>
        /// Constructor -- loads previous configuration
        /// </summary>
        public StreamrecorderService ()
        {
            recording = IsRecordingEnabledEntry.Get ().Equals ("True") ? true : false;
            output_directory = OutputDirectoryEntry.Get ();
            is_importing_enabled = IsImportingEnabledEntry.Get ().Equals ("True") ? true : false;
            is_splitting_enabled = IsFileSplittingEnabledEntry.Get ().Equals ("True") ? true : false;
            active_encoder = ActiveEncoderEntry.Get ();
            ui_button_id = 0;
        }

        #region IDelayedInitializeService implementation
        public void DelayedInitialize ()
        {
            recorder = new Recorder ();
            active_encoder = recorder.SetActiveEncoder (active_encoder);
            recorder.Create ();

            ServiceManager.PlayerEngine.ConnectEvent (OnStateChange, PlayerEvent.StateChange);
            ServiceManager.PlayerEngine.ConnectEvent (OnMetadata, PlayerEvent.TrackInfoUpdated);
        }
        #endregion

        /// <summary>
        /// Initialize the service, creating the Recorder object, connecting events and adding GUI elements
        /// </summary>
        void IExtensionService.Initialize ()
        {
            ServiceManager.SourceManager.ActiveSourceChanged += OnSourceChanged;

            action_service = ServiceManager.Get<InterfaceActionService> ();
            actions = new ActionGroup ("Streamrecorder");


            actions.Add (new ActionEntry[] { new ActionEntry ("StreamrecorderAction", null,
                             AddinManager.CurrentLocalizer.GetString ("_Streamrecorder"), null, null, null),
                             new ActionEntry ("StreamrecorderConfigureAction", Stock.Properties,
                                 AddinManager.CurrentLocalizer.GetString ("_Configure"), null,
                                 AddinManager.CurrentLocalizer.GetString ("Configure the Streamrecorder plugin"), OnConfigure) });

            actions.Add (new ToggleActionEntry[] { new ToggleActionEntry ("StreamrecorderEnableAction", Stock.MediaRecord,
                             AddinManager.CurrentLocalizer.GetString ("_Activate streamrecorder"), null,
                             AddinManager.CurrentLocalizer.GetString ("Activate streamrecorder process"),
                             OnActivateStreamrecorder, recording) });

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_menu_id = action_service.UIManager.AddUiFromResource ("StreamrecorderMenu.xml");

        }

        /// <summary>
        /// Watches source changes and dynamically adds/removes the record button in the toolbar
        /// </summary>
        /// <param name="args">
        /// A <see cref="Sources.SourceEventArgs"/> -- not used
        /// </param>
        void OnSourceChanged (Sources.SourceEventArgs args)
        {
            PrimarySource primary_source = action_service.GlobalActions.ActivePrimarySource;

            if (primary_source == null) return;

            if (!primary_source.IsLocal && ui_button_id == 0)
            {
                ui_button_id = action_service.UIManager.AddUiFromResource ("StreamrecorderButton.xml");
            }
            if (primary_source.IsLocal && ui_button_id > 0)
            {
                action_service.UIManager.RemoveUi(ui_button_id);
                ui_button_id = 0;
            }
        }

        /// <summary>
        /// Activates stream recording
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="ea">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        public void OnActivateStreamrecorder (object o, EventArgs ea)
        {
            recording = !recording;

            if (recording) {
                StartRecording ();
            } else {
                StopRecording ();
            }

            IsRecordingEnabledEntry.Set (recording.ToString ());
        }

        /// <summary>
        /// Triggers configuration, shows the configuration dialog
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="ea">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        public void OnConfigure (object o, EventArgs ea)
        {
            new StreamrecorderConfigDialog (this, output_directory, active_encoder, is_importing_enabled, is_splitting_enabled);
        }

        /// <summary>
        /// Disposes the StreamRecorder service, stops recording, disconnects events, and removes GUI elements
        /// </summary>
        public void Dispose ()
        {
            StopRecording ();
            recorder.Dispose ();
            action_service.UIManager.RemoveUi (ui_menu_id);
            if (ui_button_id > 0)
                action_service.UIManager.RemoveUi (ui_button_id);
            action_service.UIManager.RemoveActionGroup (actions);
            ServiceManager.PlayerEngine.DisconnectEvent (OnStateChange);
            actions = null;
        }

        /// <summary>
        /// The service name
        /// </summary>
        string IService.ServiceName {
            get { return "StreamrecorderService"; }
        }

        /// <summary>
        /// Helper function to indicate if the current track can be recorded safely
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/> indicating whether it is safe to record the current track or not
        /// </returns>
        private bool IsCurrentTrackRecordable ()
        {
            if (Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack != null
                && Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.IsLive
                && Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.IsPlaying)
                return true;

            return false;
        }

        /// <summary>
        /// Handles Metadata changes initiating tagging and file spliting
        /// </summary>
        /// <param name="args">
        /// A <see cref="PlayerEventArgs"/> -- not used
        /// </param>
        private void OnMetadata (PlayerEventArgs args)
        {
            if (recording) {
                TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
                recorder.AddStreamTags (track, is_splitting_enabled);
            }
        }

        /// <summary>
        /// Handles Player state changes and Stops or Starts recording if appropriate
        /// </summary>
        /// <param name="args">
        /// A <see cref="PlayerEventArgs"/>
        /// </param>
        private void OnStateChange (PlayerEventArgs args)
        {
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Idle && recording) {
                StopRecording ();
            }
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing && recording) {
                StartRecording ();
            }
        }

        /// <summary>
        /// Starts recording of the current stream if track is recordable
        /// </summary>
        private void StartRecording ()
        {
            if (!IsCurrentTrackRecordable ()) {
                return;
            }

            track = ServiceManager.PlaybackController.CurrentTrack;

            if (InitStreamrecorderProcess (track)) {
                if (!recorder.IsRecording) recorder.StartRecording ();
                recorder.AddStreamTags (track, false);

                if (is_importing_enabled)
                    StartFolderScanner ();
            }
        }

        /// <summary>
        /// stops recording of the current track
        /// </summary>
        private void StopRecording ()
        {
            if (recorder.IsRecording) recorder.StopRecording ();

            StopFolderScanner ();
        }

        /// <summary>
        /// starts the folder scanner
        /// </summary>
        public void StartFolderScanner ()
        {
            RippedFileScanner.StartScanner ();
        }

        /// <summary>
        /// stops the folder scanner
        /// </summary>
        public void StopFolderScanner ()
        {
            RippedFileScanner.StopScanner ();
        }

        /// <summary>
        /// Initializes all parameters for recording a track
        /// </summary>
        /// <param name="track_in">
        /// A <see cref="TrackInfo"/> that is to be recorded
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> indicating if all parameters could successfully be initialized
        /// </returns>
        private bool InitStreamrecorderProcess (TrackInfo track_in)
        {
            if (String.IsNullOrEmpty (output_directory)) {
                output_directory = Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary.BaseDirectory
                                 + Path.DirectorySeparatorChar + "ripped";
                Hyena.Log.DebugFormat ("[StreamrecorderService] <InitStreamrecorderProcess> output directory not set, using: {0}", output_directory);
            }

            if (!Directory.Exists (output_directory)) {
                Hyena.Log.Debug ("[StreamrecorderService] <InitStreamrecorderProcess> output directory does not exist, creating.");
                Directory.CreateDirectory (output_directory);
            }

            if (track_in == null) {
                return false;
            }

            if (track_in.Uri == null || track_in.Uri.IsLocalPath) {
                Hyena.Log.Debug ("[StreamrecorderService] <InitStreamrecorderProcess> Not recording local files");
                return false;
            }

            DateTime dt = DateTime.Now;
            string datestr = String.Format ("{0:d_M_yyyy_HH_mm_ss}", dt);
            string filename;
            RadioTrackInfo radio_track = track as RadioTrackInfo;

            //split only if Artist AND Title are present, i.e. stream sends complete metadata
            //do not set extension, will be done by recorder!
            if (is_splitting_enabled && track.ArtistName != null && track.ArtistName.Length > 0) {
                filename = recorder.SetMetadataFilename (track.TrackTitle, track.ArtistName);
            } else {
                filename = (radio_track.ParentTrack == null ? track.TrackTitle : radio_track.ParentTrack.TrackTitle) + "_" + datestr;
            }

            recorder.SetOutputParameters (output_directory, filename);

            RippedFileScanner.SetScanDirectory (output_directory);

            return true;
        }

        /// <summary>
        /// Retrieves an array containing the Names of available encoders
        /// </summary>
        /// <returns>
        /// A <see cref="System.String[]"/> containing the Names of available encoders
        /// </returns>
        public string[] GetEncoders ()
        {
            List<Encoder> encoders = recorder.Encoders;
            string[] encoder_names = new string[encoders.Count];
            for (int i = 0; i < encoders.Count; i++) {
                encoder_names[i] = encoders[i].ToString ();
            }
            return encoder_names;
        }

        /// <summary>
        /// The output directory for recorded files
        /// </summary>
        public string OutputDirectory {
            get { return output_directory; }
            set {
                if (this.output_directory.Equals (value)) return;
                StopFolderScanner ();

                this.output_directory = value;

                if (String.IsNullOrEmpty (this.output_directory)) {
                    this.output_directory = Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary.BaseDirectory
                                          + Path.DirectorySeparatorChar + "ripped";
                }
                RippedFileScanner.SetScanDirectory (this.output_directory);

                if (is_importing_enabled)
                    StartFolderScanner ();
            }
        }

        /// <summary>
        /// the Name of the configured encoder
        /// </summary>
        public string ActiveEncoder {
            get { return active_encoder; }
            set {
                if (active_encoder.Equals (value)) return;
                if (ServiceManager.PlayerEngine.IsPlaying ()) ServiceManager.PlayerEngine.TogglePlaying ();
                active_encoder = value;
                recorder.Dispose ();
                recorder = new Recorder ();
                recorder.SetActiveEncoder (active_encoder);
                recorder.Create ();
            }
        }

        /// <summary>
        /// Indicator if recorded tracks are imported into the music library
        /// </summary>
        public bool IsImportingEnabled {
            get { return is_importing_enabled; }
            set { is_importing_enabled = value; }
        }

        /// <summary>
        /// Indicator if files should be split by metadata if available
        /// </summary>
        public bool IsFileSplittingEnabled {
            get { return is_splitting_enabled; }
            set { is_splitting_enabled = value; }
        }

        public static readonly SchemaEntry<string> IsRecordingEnabledEntry = new SchemaEntry<string> (
               "plugins.streamrecorder", "is_recording_enabled", "", "Is ripping enabled", "Is ripping enabled");

        public static readonly SchemaEntry<string> OutputDirectoryEntry = new SchemaEntry<string> (
               "plugins.streamrecorder", "output_directory", "", "Output directory for ripped files", "Output directory for ripped files");

        public static readonly SchemaEntry<string> IsImportingEnabledEntry = new SchemaEntry<string> (
               "plugins.streamrecorder", "is_importing_enabled", "", "Is importing enabled", "Is importing enabled");

        public static readonly SchemaEntry<string> IsFileSplittingEnabledEntry = new SchemaEntry<string> (
               "plugins.streamrecorder", "is_splitting_enabled", "", "Is splitting enabled", "Is splitting enabled");

        public static readonly SchemaEntry<string> ActiveEncoderEntry = new SchemaEntry<string> (
               "plugins.streamrecorder", "active_encoder", "", "Active Encoder", "Active Encoder");
    }
}
