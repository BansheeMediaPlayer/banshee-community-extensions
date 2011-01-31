//
// LiveRadioPluginSource.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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
using System.Collections.Generic;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;
using Banshee.Streaming;
using Banshee.PlaybackController;
using Banshee.Collection;
using Banshee.Collection.Database;

using Gtk;

using Mono.Addins;

using Hyena;

using Banshee.LiveRadio.Plugins;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// The base class for temporary LiveRadio Plugin sources, which can add station tracks and will remove all tracks upon disposal
    /// </summary>
    public class LiveRadioPluginSource : PrimarySource, IDisposable, IBasicPlaybackController
    {
        const int sort_order = 191;
        private LiveRadioPluginSourceContents source_contents;
        private bool add_track_job_cancelled = false;
        private ILiveRadioPlugin plugin;

        /// <summary>
        /// Constructor -- creates a new temporary LiveRadio Plugin source and sets itself as the plugin's source
        /// Any tracks that have remained in the source from a previous (interrupted) session are purged
        /// </summary>
        /// <param name="plugin">
        /// A <see cref="ILiveRadioPlugin"/> -- the plugin the source is created for
        /// </param>
        public LiveRadioPluginSource (ILiveRadioPlugin plugin) :
                        base(AddinManager.CurrentLocalizer.GetString ("LiveRadioPlugin") + plugin.Name,
                             plugin.Name,
                             "live-radio-plugin-" + plugin.Name.ToLower (),
                             sort_order)
        {
            TypeUniqueId = "live-radio-" + plugin.Name;
            IsLocal = false;

            Gdk.Pixbuf icon = new Gdk.Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                      .GetManifestResourceStream ("LiveRadioIcon.svg"));
            SetIcon (icon);

            plugin.SetLiveRadioPluginSource (this);
            this.plugin = plugin;

            AfterInitialized ();

            Properties.Set<bool> ("Nereid.SourceContentsPropagate", true);

            Properties.SetString ("TrackView.ColumnControllerXml", String.Format (@"
                <column-controller>
                  <!--<column modify-default=""IndicatorColumn"">
                    <renderer type=""Banshee.Podcasting.Gui.ColumnCellPodcastStatusIndicator"" />
                  </column>-->
                  <add-default column=""IndicatorColumn"" />
                  <add-default column=""GenreColumn"" />
                  <column modify-default=""GenreColumn"">
                    <visible>false</visible>
                  </column>
                  <add-default column=""TitleColumn"" />
                  <column modify-default=""TitleColumn"">
                    <title>{0}</title>
                    <long-title>{0}</long-title>
                  </column>
                  <add-default column=""ArtistColumn"" />
                  <column modify-default=""ArtistColumn"">
                    <title>{1}</title>
                    <long-title>{1}</long-title>
                  </column>
                  <add-default column=""CommentColumn"" />
                  <column modify-default=""CommentColumn"">
                    <title>{2}</title>
                    <long-title>{2}</long-title>
                  </column>
                  <add-default column=""RatingColumn"" />
                  <add-default column=""PlayCountColumn"" />
                  <add-default column=""LastPlayedColumn"" />
                  <add-default column=""LastSkippedColumn"" />
                  <add-default column=""DateAddedColumn"" />
                  <add-default column=""UriColumn"" />
                  <sort-column direction=""asc"">genre</sort-column>
                </column-controller>",
                AddinManager.CurrentLocalizer.GetString ("Station"),
                AddinManager.CurrentLocalizer.GetString ("Creator"),
                AddinManager.CurrentLocalizer.GetString ("Description")
            ));

            ServiceManager.PlayerEngine.TrackIntercept += OnPlayerEngineTrackIntercept;

            TrackEqualHandler = delegate(DatabaseTrackInfo a, TrackInfo b) {
                RadioTrackInfo radio_track = b as RadioTrackInfo;
                return radio_track != null && DatabaseTrackInfo.TrackEqual (radio_track.ParentTrack as DatabaseTrackInfo, a);
            };

            source_contents = new LiveRadioPluginSourceContents (plugin);
            source_contents.SetSource (this);

            Properties.Set<ISourceContents> ("Nereid.SourceContents", source_contents);

            this.PurgeTracks ();

        }

        public void SetIcon (Gdk.Pixbuf icon)
        {
            Properties.Set<Gdk.Pixbuf> ("Icon.Pixbuf_16", icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear));
        }


        /// <summary>
        /// The Widget of the source contents of this source
        /// </summary>
        /// <returns>
        /// A <see cref="Widget"/>
        /// </returns>
        public Widget GetWidget ()
        {
            ISourceContents source_contents = Properties.Get<ISourceContents> ("Nereid.SourceContents");
            return source_contents.Widget;
        }

        /// <summary>
        /// Purges any tracks left in the source and removes event handlers
        /// </summary>
        public override void Dispose ()
        {
            base.Dispose ();
            this.PurgeTracks ();

            ServiceManager.PlayerEngine.TrackIntercept -= OnPlayerEngineTrackIntercept;
        }

        /// <summary>
        /// Handler when a track of this source is intercepted by the player engine. Initiates the track to play
        /// if it belongs to this source.
        /// </summary>
        /// <param name="track">
        /// A <see cref="TrackInfo"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        private bool OnPlayerEngineTrackIntercept (TrackInfo track)
        {
            DatabaseTrackInfo station = track as DatabaseTrackInfo;
            if (station == null || station.PrimarySource != this) {
                return false;
            }

            new RadioTrackInfo (station).Play ();

            return true;
        }

        /// <summary>
        /// Handles the (user) request to cancel a batch job adding new stations to the source
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        private void OnAddTrackJobCancelRequested (object o, EventArgs e)
        {
            add_track_job_cancelled = true;
        }

        /// <summary>
        /// Sets the tracks of this source. All tracks previously contained in this source are purged.
        /// </summary>
        /// <param name="tracks">
        /// A <see cref="List<DatabaseTrackInfo>"/> -- the list of tracks to be contained in the source
        /// </param>
        public void SetStations (List<DatabaseTrackInfo> tracks)
        {
            this.PurgeTracks ();
            BatchUserJob add_track_job = AddTrackJob;
            add_track_job.Total = tracks.Count;
            add_track_job.CancelRequested += OnAddTrackJobCancelRequested;
            this.PauseSorting ();
            foreach (DatabaseTrackInfo track in tracks) {
                AddStation (track);
                this.IncrementAddedTracks ();
                if (add_track_job_cancelled) {
                    add_track_job_cancelled = false;
                    Hyena.Log.Debug ("[LiveRadioPluginSource]<AddStations> job cancelled");
                    add_track_job.Completed = add_track_job.Total;
                    return;
                }
            }
            add_track_job.Finish ();
            this.ResumeSorting ();
        }

        /// <summary>
        /// Adds a track to is source. Does not actually set the tracks source.
        /// </summary>
        /// <param name="track">
        /// A <see cref="DatabaseTrackInfo"/> -- the track to be added to its set source
        /// </param>
        protected override void AddTrack (DatabaseTrackInfo track)
        {
            track.CanSaveToDatabase = false;
            track.PrimarySource = this;
            track.Save ();
        }

        /// <summary>
        /// Adds a station track to this source.
        /// </summary>
        /// <param name="track">
        /// A <see cref="DatabaseTrackInfo"/> -- the track to be added to this source
        /// </param>
        private void AddStation (DatabaseTrackInfo track)
        {
            DatabaseTrackInfo station = track ?? new DatabaseTrackInfo ();
            if (track.Copyright != null)
            {
                SafeUri url = plugin.RetrieveUrl (track.Copyright);
                if (url != null)
                {
                    track.Uri = url;
                }
                track.Copyright = null;
            }
            station.IsLive = true;
            station.PrimarySource = this;
            if (!String.IsNullOrEmpty (station.TrackTitle) && station.Uri != null && station.Uri is SafeUri) {
                this.AddTrack (station);
            }
        }

        #region IBasicPlaybackController implementation

        /// <summary>
        /// First method for IBasicPlaybackController
        /// </summary>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool First ()
        {
            return false;
        }

        /// <summary>
        /// Next method for IBasicPlaybackController
        /// </summary>
        /// <param name="restart">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <param name="userRequested">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Next (bool restart, bool userRequested)
        {
            return Next (restart);
        }

        /// <summary>
        /// Next method for IBasicPlaybackController
        /// </summary>
        /// <param name="restart">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Next (bool restart)
        {
            RadioTrackInfo radio_track = ServiceManager.PlaybackController.CurrentTrack as RadioTrackInfo;
            if (radio_track != null && radio_track.PlayNextStream ()) {
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Previous  method for IBasicPlaybackController
        /// </summary>
        /// <param name="restart">
        /// A <see cref="System.Boolean"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// </returns>
        public bool Previous (bool restart)
        {
            RadioTrackInfo radio_track = ServiceManager.PlaybackController.CurrentTrack as RadioTrackInfo;
            if (radio_track != null && radio_track.PlayPreviousStream ()) {
                return true;
            } else {
                return false;
            }
        }

        #endregion

        public override bool AcceptsInputFromSource (Source source)
        {
            return false;
        }

        /// <summary>
        /// Set to false so the track view is not searchable and query is not interrupted
        /// </summary>
        public override bool CanSearch {
            get { return true; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool ShowBrowser {
            get { return true; }
        }

        public override bool CanRename {
            get { return false; }
        }

        protected override bool HasArtistAlbum {
            get { return false; }
        }

        public override bool HasViewableTrackProperties {
            get { return true; }
        }

        public override bool HasEditableTrackProperties {
            get { return false; }
        }

    }

}
