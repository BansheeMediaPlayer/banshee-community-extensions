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

using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;
using Banshee.Streaming;
using Banshee.PlaybackController;
using Banshee.Collection;
using Banshee.Collection.Database;

using Gtk;

using Hyena;

using Banshee.LiveRadio.Plugins;

namespace Banshee.LiveRadio
{
    // We are inheriting from Source, the top-level, most generic type of Source.
    // Other types include (inheritance indicated by indentation):
    //      DatabaseSource - generic, DB-backed Track source; used by PlaylistSource
    //        PrimarySource - 'owns' tracks, used by DaapSource, DapSource
    //          LibrarySource - used by Music, Video, Podcasts, and Audiobooks
    public class LiveRadioPluginSource : PrimarySource, IDisposable, IBasicPlaybackController
    {
        // In the sources TreeView, sets the order value for this source, small on top
        const int sort_order = 191;
        private LiveRadioPluginSourceContents source_contents;
        private bool add_track_job_cancelled = false;

        public LiveRadioPluginSource (ILiveRadioPlugin plugin) :
                        base(Catalog.GetString ("LiveRadioPlugin") + plugin.GetName (),
                             plugin.GetName (),
                             "live-radio-plugin-" + plugin.GetName ().ToLower (),
                             sort_order)
        {
            Log.DebugFormat ("[LiveRadioPluginSource\"{0}\"]<Constructor> START", plugin.GetName ());
            TypeUniqueId = "live-radio-" + plugin.GetName ();
            IsLocal = false;
            
            plugin.SetLiveRadioPluginSource (this);
            
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
                Catalog.GetString ("Station"),
                Catalog.GetString ("Creator"),
                Catalog.GetString ("Description")
            ));

            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.Set<bool> ("ActiveSourceUIResourcePropagate", true);
            Properties.Set<System.Reflection.Assembly> ("ActiveSourceUIResource.Assembly", typeof(LiveRadioPluginSource).Assembly);
            
            Properties.SetString ("GtkActionPath", "/LiveRadioContextMenu");
            
            ServiceManager.PlayerEngine.TrackIntercept += OnPlayerEngineTrackIntercept;
            
            TrackEqualHandler = delegate(DatabaseTrackInfo a, TrackInfo b) {
                RadioTrackInfo radio_track = b as RadioTrackInfo;
                return radio_track != null && DatabaseTrackInfo.TrackEqual (radio_track.ParentTrack as DatabaseTrackInfo, a);
            };
            
            source_contents = new LiveRadioPluginSourceContents (plugin);
            source_contents.SetSource (this);
            
            Properties.Set<ISourceContents> ("Nereid.SourceContents", source_contents);
            
            this.PurgeTracks ();
            
            Log.DebugFormat ("[LiveRadioPluginSource\"{0}\"]<Constructor> END", plugin.GetName ());
            
        }

        public Widget GetWidget ()
        {
            ISourceContents source_contents = Properties.Get<ISourceContents> ("Nereid.SourceContents");
            return source_contents.Widget;
        }

        public override void Dispose ()
        {
            base.Dispose ();
            this.PurgeTracks ();
            
            ServiceManager.PlayerEngine.TrackIntercept -= OnPlayerEngineTrackIntercept;
        }

        private bool OnPlayerEngineTrackIntercept (TrackInfo track)
        {
            DatabaseTrackInfo station = track as DatabaseTrackInfo;
            if (station == null || station.PrimarySource != this) {
                return false;
            }
            
            new RadioTrackInfo (station).Play ();
            
            return true;
        }

        private void OnAddTrackJobCancelRequested (object o, EventArgs e)
        {
            add_track_job_cancelled = true;
        }

        public void SetStations (List<DatabaseTrackInfo> tracks)
        {
            this.PurgeTracks ();
            BatchUserJob add_track_job = AddTrackJob;
            add_track_job.Total = tracks.Count;
            add_track_job.CancelRequested += OnAddTrackJobCancelRequested;
            //source_contents.Widget.FreezeChildNotify ();
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
            //source_contents.Widget.ThawChildNotify ();
            
        }

        protected override void AddTrack (DatabaseTrackInfo track)
        {
            track.Save ();
        }

        private void AddStation (DatabaseTrackInfo track)
        {
            DatabaseTrackInfo station = track ?? new DatabaseTrackInfo ();
            station.IsLive = true;
            station.PrimarySource = this;
            if (!String.IsNullOrEmpty (station.TrackTitle) && station.Uri != null && station.Uri is SafeUri) {
                //this.AddTrackAndIncrementCount(station);
                this.AddTrack (station);
            }
        }

        protected void OnAddToFavorites (object o, EventArgs e)
        {
            return;
        }

        protected void OnAddToInternetRadio (object o, EventArgs e)
        {
            return;
        }

        protected void OnFindSimilar (object o, EventArgs e)
        {
            return;
        }

        #region IBasicPlaybackController implementation

        public bool First ()
        {
            return false;
        }

        public bool Next (bool restart)
        {
            RadioTrackInfo radio_track = ServiceManager.PlaybackController.CurrentTrack as RadioTrackInfo;
            if (radio_track != null && radio_track.PlayNextStream ()) {
                return true;
            } else {
                return false;
            }
        }

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
            get { return false; }
        }

        protected PrimarySource GetInternetRadioSource ()
        {
            Log.Debug ("[LiveRadioPluginSource] <GetInternetRadioSource> Start");
            
            foreach (Source source in Banshee.ServiceStack.ServiceManager.SourceManager.Sources) {
                Log.DebugFormat ("[LiveRadioPluginSource] <GetInternetRadioSource> Source: {0}", source.GenericName);
                
                if (source.UniqueId.Equals ("InternetRadioSource-internet-radio")) {
                    return (PrimarySource)source;
                }
            }
            
            Log.Debug ("[LiveRadioPluginSource] <GetInternetRadioSource> Not found throwing exception");
            throw new InternetRadioExtensionNotFoundException ();
        }
        
    }

    public class InternetRadioExtensionNotFoundException : Exception
    {
    }
    
    
    
}
