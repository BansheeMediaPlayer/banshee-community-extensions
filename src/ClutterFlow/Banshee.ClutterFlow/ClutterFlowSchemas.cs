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

using Mono.Addins;

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
            SchemaPreference<T> pref = new SchemaPreference<T> (entry, entry.ShortDescription, entry.LongDescription, func);
            section.Add (pref);
        }

        internal static readonly SchemaEntry<int> DragSensitivity = new SchemaEntry<int>(
            "clutterflow", "drag_sensitivity",
            3, 1, 20,
            AddinManager.CurrentLocalizer.GetString ("Sensitivity for album dragging"),
            AddinManager.CurrentLocalizer.GetString ("Sets the sensitivity with which albums scroll when dragged, higher values mean faster scrolling")
        );

        internal static readonly SchemaEntry<bool> ExpandTrackList = new SchemaEntry<bool>(
            "clutterflow", "expand_track_list",
            true,
            AddinManager.CurrentLocalizer.GetString ("Unfolds the track list"),
            AddinManager.CurrentLocalizer.GetString ("If checked it will display the track list when not in fullscreen mode")
        );

        internal static readonly SchemaEntry<bool> OldShowBrowser = new SchemaEntry<bool>(
            "clutterflow", "old_show_browser",
            false,
            "Saved value for Show Browser",
            "If checked the Browser will be displayed when ClutterFlow is made invisible"
        );

        internal static readonly SchemaEntry<bool> ShowClutterFlow = new SchemaEntry<bool>(
            "clutterflow", "show_clutterflow",
            false,
            "Display ClutterFlow",
            "If checked displays the ClutterFlow browser instead of the default one"
        );

        internal static readonly SchemaEntry<bool> InstantPlayback = new SchemaEntry<bool>(
            "clutterflow", "instant_playback",
            true,
            AddinManager.CurrentLocalizer.GetString ("Immediately apply playback mode changes"),
            AddinManager.CurrentLocalizer.GetString ("Starts playing a new song immediately after the playback mode changed (Party Mode or Album Mode)")
        );

        internal static readonly SchemaEntry<bool> DisplayLabel = new SchemaEntry<bool>(
            "clutterflow", "display_album_label",
            true,
            AddinManager.CurrentLocalizer.GetString ("Display album _label"),
            AddinManager.CurrentLocalizer.GetString ("Wether or not the album label needs to be shown above the artwork")
        );

        internal static readonly SchemaEntry<bool> DisplayTitle = new SchemaEntry<bool>(
            "clutterflow", "display_track_title",
            true,
            AddinManager.CurrentLocalizer.GetString ("Display track _title"),
            AddinManager.CurrentLocalizer.GetString ("Wether or not the album title needs to be shown above the artwork in fullscreen mode")
        );

        internal static readonly SchemaEntry<int> TextureSize = new SchemaEntry<int>(
            "clutterflow", "texture_size",
            128, 32, 512,
            AddinManager.CurrentLocalizer.GetString ("Texture size in pixels"),
            AddinManager.CurrentLocalizer.GetString ("The in-memory size of the cover textures in pixels")
        );

        internal static readonly SchemaEntry<int> MinCoverSize = new SchemaEntry<int>(
            "clutterflow", "min_cover_size",
            64, 64, 128,
            AddinManager.CurrentLocalizer.GetString ("Minimal cover size in pixels"),
            AddinManager.CurrentLocalizer.GetString ("The on-stage minimal cover size in pixels")
        );

        internal static readonly SchemaEntry<int> MaxCoverSize = new SchemaEntry<int>(
            "clutterflow", "max_cover_size",
            256, 128, 512,
            AddinManager.CurrentLocalizer.GetString ("Maximal cover size in pixels"),
            AddinManager.CurrentLocalizer.GetString ("The on-stage minimal cover size in pixels")
        );

        internal static readonly SchemaEntry<int> VisibleCovers = new SchemaEntry<int>(
            "clutterflow", "visbible_covers",
            7, 1, 15,
            AddinManager.CurrentLocalizer.GetString ("Number of visible covers at the side"),
            AddinManager.CurrentLocalizer.GetString ("The number of covers that need to be displayed on the stage (at one side)")
        );

        internal static readonly SchemaEntry<string> SortBy = new SchemaEntry<string>(
            "clutterflow", "sort_by",
            (string) Enum.GetName (typeof(SortOptions), SortOptions.Artist),
            AddinManager.CurrentLocalizer.GetString ("Sort covers by"),
            AddinManager.CurrentLocalizer.GetString ("Selects on what basis covers should be sorted")
        );
    }
}
