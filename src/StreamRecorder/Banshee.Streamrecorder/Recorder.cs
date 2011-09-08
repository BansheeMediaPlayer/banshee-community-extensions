//
// Recorder.cs
//
// Author:
//   Frank Ziegler
//
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

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Streamrecorder.Gst;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Streamrecorder
{

    /// <summary>
    /// A Recorder object that uses a GStreamerMiniBinding to enable recoding of the player pipeline by attaching to the player audio tee
    /// </summary>
    public class Recorder : IDisposable
    {
        private string output_directory;
        private string output_file;
        private string file_extension;
        private bool is_recording;

        private PlayerAudioTee audiotee;
        private Bin encoder_bin;
        private TagSetter tagger;
        private FileSink file_sink;
        private string empty_file = "/dev/null";
        private GhostPad ghost_pad;

        private Element outputselector;
        private Pad selector_fakepad;
        private Pad selector_filepad;

        private List<Encoder> encoders = new List<Encoder> ();
        private bool has_lame;
        private const string lame_name = "LAME MP3 Audio Encoder";
        private const string lame_pipeline = "! lamemp3enc name=audio_encoder ! id3v2mux name=tagger ";
        private const string lame_extension = ".mp3";
        private bool has_vorbis;
        private const string vorbis_name = "Ogg/Vorbis Audio Encoder";
        private const string vorbis_pipeline = "! vorbisenc name=audio_encoder ! vorbistag name=tagger ! oggmux ";
        private const string vorbis_extension = ".ogg";
        private bool has_flac;
        private const string flac_name = "FLAC Audio Encoder";
        private const string flac_pipeline = "! flacenc name=audio_encoder ! flactag name=tagger ";
        private const string flac_extension = ".flac";

        /// <summary>
        /// Constructor -- creates a new Recorder that will use the GStreamerMiniBinding to connect itself to the player tee for recording streams
        /// </summary>
        public Recorder ()
        {
            try
            {
                if (Marshaller.Initialize ()) {
                    encoders.Add (new Encoder ("None (unchanged stream)", "! identity ", null));

                    has_lame = Marshaller.CheckGstPlugin ("lame") && Marshaller.CheckGstPlugin ("id3v2mux");
                    Hyena.Log.Debug ("[Streamrecorder] GstPlugin lame" + (has_lame ? "" : " not") + " found");
                    if (has_lame) encoders.Add (new Encoder (lame_name, lame_pipeline, lame_extension));

                    has_vorbis = Marshaller.CheckGstPlugin ("vorbisenc") && Marshaller.CheckGstPlugin ("oggmux") && Marshaller.CheckGstPlugin ("oggmux");
                    Hyena.Log.Debug ("[Streamrecorder] GstPlugin vorbis" + (has_vorbis ? "" : " not") + " found");
                    if (has_vorbis) encoders.Add (new Encoder (vorbis_name, vorbis_pipeline, vorbis_extension, true));

                    has_flac = Marshaller.CheckGstPlugin ("flacenc") && Marshaller.CheckGstPlugin ("flactag");
                    Hyena.Log.Debug ("[Streamrecorder] GstPlugin flac" + (has_flac ? "" : " not") + " found");
                    if (has_flac) encoders.Add (new Encoder (flac_name, flac_pipeline, flac_extension));

                } else {
                    Hyena.Log.Debug ("[Streamrecorder] an error occurred during gstreamer initialization, aborting.");
                }
            } catch (Exception e) {
                Hyena.Log.Information ("[Streamrecorder] An exception occurred during gstreamer initialization");
                Hyena.Log.Debug (e.StackTrace);
            }

            is_recording = false;
        }

        /// <summary>
        /// Creates a new recoding pipeline with the best (by user preference) available encoder and attaches it
        /// to the audiotee
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/>, true if the pipeline was successfully created, false otherwise.
        /// </returns>
        public bool Create ()
        {
            string bin_description = BuildPipeline ();

            try {
                audiotee = new PlayerAudioTee (ServiceManager.PlayerEngine.ActiveEngine.GetBaseElements ()[2]);

                if (bin_description.Equals ("")) {
                    return false;
                }

                encoder_bin = Parse.BinFromDescription (bin_description, true);

                tagger = new TagSetter (encoder_bin.GetByInterface (TagSetter.GetType ()));
                file_sink = encoder_bin.GetByName ("file_sink").ToFileSink ();

                file_sink.Location = empty_file;
                file_sink.SetBooleanProperty ("sync", true);
                file_sink.SetBooleanProperty ("async", false);

                GLib.Object.GetObject (file_sink.ToIntPtr ()).AddNotification ("allow-overwrite", OnAllowOverwrite);

                ghost_pad = encoder_bin.GetStaticPad ("sink").ToGhostPad ();

                outputselector = encoder_bin.GetByName ("sel");

                Pad filesinkpad = file_sink.GetStaticPad ("sink");
                selector_filepad = filesinkpad.GetPeer ();

                Element fake_sink = encoder_bin.GetByName ("fake_sink");
                Pad fakesinkpad = fake_sink.GetStaticPad ("sink");
                selector_fakepad = fakesinkpad.GetPeer ();

                audiotee.AddBin (encoder_bin, ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing);
                Hyena.Log.Debug ("[Recorder] Recorder attached");
            } catch (Exception e) {
                Hyena.Log.InformationFormat ("[Streamrecorder] An exception occurred during pipeline construction: {0}", bin_description);
                Hyena.Log.Debug (e.StackTrace);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Helper function to build the actual pipeline string
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> containing the pipeline description
        /// </returns>
        private string BuildPipeline ()
        {
            string pipeline = "";
            string pipeline_start = "queue ! audioresample ! audioconvert ";
            string pipeline_end = "! output-selector name=sel sel. ! fakesink name=fake_sink async=false sel. ! filesink name=file_sink async=false";
            Encoder encoder = GetFirstAvailableEncoder ();

            if (encoder != null) {
                pipeline = pipeline_start + encoder.Pipeline + pipeline_end;
                if (String.IsNullOrEmpty (encoder.FileExtension))
                {
                    if (Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack != null)
                    {
                        string trackuri = Banshee.ServiceStack.ServiceManager.PlaybackController.CurrentTrack.Uri.ToString ();
                        int ind = trackuri.LastIndexOf ('.');
                        file_extension = trackuri.Substring (ind).Substring (0,4);
                    } else {
                        file_extension = ".audiostream";
                    }
                    foreach (char c in Path.GetInvalidFileNameChars ())
                        file_extension.Replace (c, '_');
                } else {
                    file_extension = encoder.FileExtension;
                }
            }

            return pipeline;
        }

        /// <summary>
        /// List of available encoders
        /// </summary>
        public List<Encoder> Encoders {
            get { return encoders; }
        }

        /// <summary>
        /// Sets the prefered encoder for the Recorder to use
        /// </summary>
        /// <param name="active_encoder">
        /// A <see cref="System.String"/> containing the Name of the encoder that is requested
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> containing the Name of the encoder that will be used
        /// </returns>
        public string SetActiveEncoder (string active_encoder)
        {
            string ret = null;
            if (active_encoder == null)
                return null;
            foreach (Encoder encoder in encoders) {
                if (encoder.ToString ().Equals (active_encoder)) {
                    encoder.IsPreferred = true;
                    ret = encoder.ToString ();
                } else {
                    encoder.IsPreferred = false;
                }
            }
            if (ret == null) {
                ret = GetFirstAvailableEncoder ().ToString ();
            }
            return ret;
        }

        /// <summary>
        /// Helper function returning the first preferred encoder from the list of encoders
        /// </summary>
        /// <returns>
        /// The first <see cref="Encoder"/> object that is preferred in the list of encoders or null, if there is none.
        /// </returns>
        private Encoder GetFirstPreferredEncoder ()
        {
            foreach (Encoder encoder in encoders) {
                if (encoder.IsPreferred)
                    return encoder;
            }
            return null;
        }

        /// <summary>
        /// Helper function returning the first available encoder, using preferred encoders first
        /// </summary>
        /// <returns>
        /// An <see cref="Encoder"/> object or null, if there is no encoders in the list
        /// </returns>
        public Encoder GetFirstAvailableEncoder ()
        {
            Encoder encoder = GetFirstPreferredEncoder ();
            if (encoder == null && encoders.Count >= 1) {
                encoder = encoders[0];
            }
            return encoder;
        }

        /// <summary>
        /// Function to control behaviour when files would be overwritten. Should never be called in current code conditions.
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="args">
        /// A <see cref="GLib.NotifyArgs"/> -- not used
        /// </param>
        private void OnAllowOverwrite (object o, GLib.NotifyArgs args)
        {
            return;
        }

        public bool IsRecording {
            get { return this.is_recording; }
        }

        /// <summary>
        /// Starts recording of the current stream by switching from fakesink to filesink
        /// </summary>
        public void StartRecording ()
        {
            if (outputselector != null && !outputselector.IsNull () && encoder_bin != null && !encoder_bin.IsNull ()) {
                try {
                    //switch output-selector: set file location and set active pad to filesink
                    SetNewTrackLocation (output_file + file_extension);
                    outputselector.SetProperty ("active-pad", new Element (selector_filepad.ToIntPtr ()));
                    Hyena.Log.Debug ("[Recorder] <StartRecording> Recording started");
                    is_recording = true;
                } catch (Exception e) {
                    is_recording = false;
                    Hyena.Log.Information ("[Streamrecorder] An exception occurred during gstreamer operation");
                    Hyena.Log.Debug (e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Stops recording of the current stream by switching from filesink to fakesink
        /// </summary>
        public void StopRecording ()
        {
            if (encoder_bin != null && !encoder_bin.IsNull () && outputselector != null && !outputselector.IsNull ()) {
                try {
                    //switch output-selector: set file location to /tmp and set active pad to fakesink
                    SetNewTrackLocation (empty_file);
                    outputselector.SetProperty ("active-pad", new Element (selector_fakepad.ToIntPtr ()));
                    //string outputpadparent = new Pad (outputselector.GetProperty ("active-pad")).GetPeer ().GetParent ().GetPathString ();
                    Hyena.Log.Debug ("[Recorder] <StopRecording> Recording stopped");
                    is_recording = false;
                } catch (Exception e) {
                    Hyena.Log.Information ("[Streamrecorder] An exception occurred during gstreamer operation");
                    Hyena.Log.Debug (e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Adds Metadata tags to the recorded file using GStreamer TagSetter interface and splits files if requested
        /// </summary>
        /// <param name="track">
        /// A <see cref="TrackInfo"/> containing the current stream and its metadata
        /// </param>
        /// <param name="splitfiles">
        /// A <see cref="System.Boolean"/> indicating whether the recorded files are to be split
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> indicating if tagging was successful
        /// </returns>
        public bool AddStreamTags (TrackInfo track, bool splitfiles)
        {
            if (track == null)
                return false;

            if (splitfiles && file_sink != null && track.ArtistName != null && track.ArtistName.Length > 0) {
                SetMetadataFilename (track.TrackTitle, track.ArtistName);
                SetNewTrackLocation (output_file + file_extension);
            }

            if (tagger == null || tagger.IsNull ()) {
                Hyena.Log.Debug ("[Recorder]<AddStreamTags> tagger is null, not tagging!");
                return false;
            }

            try {
                TagList taglist = new TagList ();
                if (track.TrackTitle != null)
                    taglist.AddStringValue (TagMergeMode.ReplaceAll, "title", track.TrackTitle);
                if (track.Genre != null)
                    taglist.AddStringValue (TagMergeMode.ReplaceAll, "genre", track.Genre);
                if (track.ArtistName != null)
                    taglist.AddStringValue (TagMergeMode.ReplaceAll, "artist", track.ArtistName);
                if (track.AlbumArtist != null)
                    taglist.AddStringValue (TagMergeMode.ReplaceAll, "album-artist", track.AlbumArtist);

                tagger.MergeTags (taglist, TagMergeMode.ReplaceAll);
            } catch (Exception e) {
                Hyena.Log.Information ("[Streamrecorder] An exception occurred during gstreamer operation");
                Hyena.Log.Debug (e.StackTrace);
            }

            return true;
        }

        /// <summary>
        /// Creates a new filename from title and artist
        /// </summary>
        /// <param name="title">
        /// A <see cref="System.String"/> containing the track title
        /// </param>
        /// <param name="artist">
        /// A <see cref="System.String"/> containing the track artist
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> the new filename including the directory and file extension
        /// </returns>
        public string SetMetadataFilename (string title, string artist)
        {
            string new_name = artist + "_-_" + title;
            string test_name = new_name;
            int i = 0;
            while (File.Exists (output_directory + Path.DirectorySeparatorChar + SetOutputFile (test_name) + file_extension)) {
                i++;
                test_name = new_name + "(" + i + ")";
            }
            new_name = test_name;
            SetOutputFile (new_name);
            return output_file;
        }

        /// <summary>
        /// Sets the output directory and filename for recording
        /// </summary>
        /// <param name="directory">
        /// A <see cref="System.String"/> containing the output directory
        /// </param>
        /// <param name="filename">
        /// A <see cref="System.String"/> containing the output filename that will be used if no metadata filenames are used
        /// </param>
        public void SetOutputParameters (string directory, string filename)
        {
            SetOutputDirectory (directory);
            SetOutputFile (filename);
        }

        /// <summary>
        /// Helper function to set the output directory
        /// </summary>
        /// <param name="directory">
        /// A <see cref="System.String"/> containing the output directory
        /// </param>
        private void SetOutputDirectory (string directory)
        {
            output_directory = directory;
        }

        /// <summary>
        /// Helper function to set the output filename, removing invalid characters
        /// </summary>
        /// <param name="fullfilename">
        /// A <see cref="System.String"/> containing the desired filename including extension
        /// </param>
        /// <returns>
        /// A <see cref="System.String"/> containing the cleaned filename that will be used
        /// </returns>
        private string SetOutputFile (string fullfilename)
        {
            string cleanfilename = fullfilename;
            char[] invalid_chars = Path.GetInvalidFileNameChars ();
            foreach (char invalid_char in invalid_chars) {
                cleanfilename = cleanfilename.Replace (invalid_char.ToString (), "_").Trim ('_');
            }
            output_file = output_directory + Path.DirectorySeparatorChar + cleanfilename;
            return cleanfilename;
        }

        /// <summary>
        /// Changes the location of the file being recorded while recording is in progress,
        /// splitting the file at the current stream location
        /// </summary>
        /// <param name="new_location">
        /// A <see cref="System.String"/> containing the full new filename and path
        /// </param>
        private void SetNewTrackLocation (string new_location)
        {
            try {
                Pad teepad = ghost_pad.GetPeer ();
                if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing)
                {
                    teepad.SetBlocked (true);
                    encoder_bin.SendEvent (Marshaller.NewEOSEvent ());
                    encoder_bin.SetState (State.Null);
                    file_sink.Location = new_location;
                    encoder_bin.SetState (State.Ready);
                    encoder_bin.SetState (State.Playing);
                    teepad.SetBlocked (false);
                } else {
                    encoder_bin.SendEvent (Marshaller.NewEOSEvent ());
                    encoder_bin.SetState (State.Null);
                    file_sink.Location = new_location;
                    encoder_bin.SetState (State.Ready);
                }
            } catch (Exception e) {
                Hyena.Log.Information ("[Streamrecorder] An exception occurred during gstreamer operation");
                Hyena.Log.Debug (e.StackTrace);
            }
        }

        /// <summary>
        /// Detaches the encoder bin from the audiotee
        /// </summary>
        public void Dispose ()
        {
            try {
                audiotee.RemoveBin (encoder_bin, ServiceManager.PlayerEngine.CurrentState == PlayerState.Playing);
                Hyena.Log.Debug ("[Recorder] Recorder detached");
            } catch (Exception e) {
                Hyena.Log.Information ("[Streamrecorder] An exception occurred during gstreamer operation");
                Hyena.Log.Debug (e.StackTrace);
            }
        }

    }

}
