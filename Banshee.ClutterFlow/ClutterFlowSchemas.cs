// 
// ClutterFlowSchemas.cs
//  
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
// 
// Copyright (c) 2010 Mathijs Dumon
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

using Banshee.I18n;
using Banshee.Configuration;
using Banshee.Preferences;

namespace Banshee.ClutterFlow
{

   /// <summary>
   /// Static class providing ClutterFlow with setting schema's
   /// </summary>
	public static class ClutterFlowSchemas {
        internal static void AddToSection<T> (Section section, SchemaEntry<T> entry, SchemaPreferenceUpdatedHandler func)
        {
            section.Add (new SchemaPreference<T> (entry, entry.ShortDescription, entry.LongDescription, func));
        }
        
        internal static readonly SchemaEntry<bool> ThreadedArtwork = new SchemaEntry<bool>(
            "clutterflow", "threaded_artwork",
            true,
            Catalog.GetString ("Enable threaded loading of artwork"),
            Catalog.GetString ("If enabled ClutterFlow will use threading to load it's artwork")
        );
        
        internal static readonly SchemaEntry<bool> ExpandTrackList = new SchemaEntry<bool>(
            "clutterflow", "expand_track_list",
            true,
            Catalog.GetString ("Unfolds the track list"),
            Catalog.GetString ("If checked it will display the track list when not in fullscreen mode")
        );
        
        internal static readonly SchemaEntry<bool> ShowClutterFlow = new SchemaEntry<bool>(
            "clutterflow", "show_clutterflow",
            false,
            Catalog.GetString ("Display ClutterFlow"),
            Catalog.GetString ("If checked displays the ClutterFlow browser instead of the default one")
        );
		
        internal static readonly SchemaEntry<bool> InstantPlayback = new SchemaEntry<bool>(
            "clutterflow", "instant_playback",
            true,
            Catalog.GetString ("Immediately apply playback mode changes"),
            Catalog.GetString ("Starts playing a new song immediately after the playback mode changed (Party Mode or Album Mode)")
        );
		
        internal static readonly SchemaEntry<bool> DisplayLabel = new SchemaEntry<bool>(
            "clutterflow", "display_album_label",
            true,
            Catalog.GetString ("Display album _label"),
            Catalog.GetString ("Wether or not the album label needs to be shown above the artwork")
        );

        internal static readonly SchemaEntry<bool> DisplayTitle = new SchemaEntry<bool>(
            "clutterflow", "display_track_title",
            true,
            Catalog.GetString ("Display track _title"),
            Catalog.GetString ("Wether or not the album title needs to be shown above the artwork in fullscreen mode")
        );

        internal static readonly SchemaEntry<int> TextureSize = new SchemaEntry<int>(
            "clutterflow", "texture_size",
            128, 32, 512,
            Catalog.GetString ("Texture size in pixels"),
            Catalog.GetString ("The in-memory size of the cover textures in pixels")
        );

        internal static readonly SchemaEntry<int> MinCoverSize = new SchemaEntry<int>(
            "clutterflow", "min_cover_size",
            64, 64, 128,
            Catalog.GetString ("Minimal cover size in pixels"),
            Catalog.GetString ("The on-stage minimal cover size in pixels")
        );

        internal static readonly SchemaEntry<int> MaxCoverSize = new SchemaEntry<int>(
            "clutterflow", "max_cover_size",
            256, 128, 512,
            Catalog.GetString ("Maximal cover size in pixels"),
            Catalog.GetString ("The on-stage minimal cover size in pixels")
        );

        internal static readonly SchemaEntry<int> VisibleCovers = new SchemaEntry<int>(
            "clutterflow", "visbible_covers",
            7, 1, 15,
            Catalog.GetString ("Number of visible covers at the side"),
            Catalog.GetString ("The number of covers that need to be displayed on the stage (at one side)")
        );
	}
	
    /*public class ClutterFlowSource : Source, IDisposable//, ITrackModelSource
    {
	
        private ClutterFlowInterface clutter_flow_interface;
        
		public ClutterFlowSource () : base("ClutterFlow", "ClutterFlow", 0)
        {
			
			TypeUniqueId = "ClutterFlow";
			Properties.SetString ("Icon.Name", "clutterflow-icon");
			Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", true);
			
            clutter_flow_interface = new ClutterFlowInterface ();

			
			
            //Properties.Set<ISourceContents> ("Nereid.SourceContents", clutter_flow_interface);
			//SetParentSource(ServiceManager.SourceManager.MusicLibrary);
			//Parent.AddChildSource(this);
			//ServiceManager.SourceManager.AddSource (this);
			
			InstallPreferences ();
			//CreateAlbumListModel ();

			//Reload();
        }
		
        public void Dispose ()
        {		
            if (clutter_flow_interface != null) {
                clutter_flow_interface.Destroy ();
                clutter_flow_interface.Dispose ();
                clutter_flow_interface = null;
            }
			UninstallPreferences ();
        }

		#region FullScreen Overrides
        public override void Activate ()
        {
            if (clutter_flow_interface != null) {
                clutter_flow_interface.OverrideFullscreen ();
            }
        }

        public override void Deactivate ()
        {
            if (clutter_flow_interface != null) {
                clutter_flow_interface.RelinquishFullscreen ();
            }
        }
		#endregion

    }*/
}
