//
// ColumnCellAlbum.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Frank Ziegler <funtastix@googlemail.com>
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
// Copyright (C) 2011 Frank Ziegler
// Copyright 2013 Tomasz Maczyński
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
using System;
using Gtk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Data.Gui;
using Hyena.Data.Gui.Accessibility;

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.I18n;
using Banshee.Collection.Database;
using Banshee.Database;
using Hyena.Data.Sqlite;
using System.Collections.Generic;
using Banshee.Playlist;
using Banshee.SmartPlaylist;

namespace Banshee.Collection.Gui
{
    public class CoverArtArtistColumnCell : ColumnCell
    {
        private static int image_spacing = 4;
        private static int album_spacing_small = 4;
        private static int album_spacing_normal = 6;
        private static bool use_small_images;
        private static int image_size;

        private const int small_image_size = 20;
        private const int normal_image_size = 44;

        public int ImageSize {
            get { return image_size; }
        }

        private static ImageSurface default_cover_image
            = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (image_size, "media-optical", "browser-album-cover"));

        private static BansheeModelProvider<DatabaseAlbumInfo> provider = new BansheeModelProvider<DatabaseAlbumInfo> (
            ServiceManager.DbConnection, "CoreAlbums"
        );

        private static HyenaSqliteCommand default_select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreAlbums.AlbumID IN " +
                "(SELECT AlbumID FROM CoreTracks WHERE ArtistID = ? AND PrimarySourceID = ?)",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static HyenaSqliteCommand sm_playlist_select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreAlbums.AlbumID IN " +
               "(SELECT AlbumID FROM CoreTracks, CoreSmartPlaylistEntries WHERE ArtistID = ? " +
                "AND CoreSmartPlaylistEntries.TrackID = CoreTracks.TrackID " +
                "AND CoreTracks.PrimarySourceID = ? " +
                "AND CoreSmartPlaylistEntries.SmartPlaylistID = ? )",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static HyenaSqliteCommand playlist_select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreAlbums.AlbumID IN " +
                "(SELECT AlbumID FROM CoreTracks, CorePlaylistEntries WHERE ArtistID = ? " +
                 "AND CorePlaylistEntries.TrackID = CoreTracks.TrackID " +
                 "AND CoreTracks.PrimarySourceID = ? " +
                 "AND CorePlaylistEntries.PlaylistID = ? )",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static HyenaSqliteCommand countall_select_command = new HyenaSqliteCommand (
            "SELECT COUNT(DISTINCT CoreAlbums.AlbumID) FROM CoreTracks, CoreAlbums " +
            "WHERE CoreTracks.AlbumID = CoreAlbums.AlbumID AND CoreTracks.PrimarySourceID = ?")
        ;

        private ArtworkManager artwork_manager;

        public CoverArtArtistColumnCell (bool small_images_used) : base (null, true)
        {
            artwork_manager = ServiceManager.Get<ArtworkManager> ();

            use_small_images = small_images_used;

            if (use_small_images) {
                image_size = small_image_size;
            } else {
                image_size = normal_image_size;
            }

            default_cover_image
                = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (image_size, "media-optical", "browser-album-cover"));

            if (!artwork_manager.IsCachedSize (image_size))
                artwork_manager.AddCachedSize (image_size);
        }

        private class ColumnCellArtistAccessible : ColumnCellAccessible
        {
            public ColumnCellArtistAccessible (object bound_object, CoverArtArtistColumnCell cell, ICellAccessibleParent parent)
                : base (bound_object, cell as ColumnCell, parent)
            {
                var bound_artist_info = bound_object as ArtistInfo;
                if (bound_artist_info != null) {
                    Name = bound_artist_info.DisplayName;
                }
            }
        }

        public override Atk.Object GetAccessible (ICellAccessibleParent parent)
        {
            return new ColumnCellArtistAccessible (BoundObject, this, parent);
        }

        private AlbumInfo[] GetAlbums (long artist_id)
        {
            Source source = ServiceManager.SourceManager.ActiveSource as Source;

            PlaylistSource playlist_source = null;
            if (source is PlaylistSource)
                playlist_source = source as PlaylistSource;

            SmartPlaylistSource sm_playlist_source = null;
            if (source is SmartPlaylistSource)
                sm_playlist_source = source as SmartPlaylistSource;

            while (!(source is PrimarySource))
                source = source.Parent;

            IDataReader reader;
            if (playlist_source != null)
                reader = ServiceManager.DbConnection.Query (playlist_select_command, artist_id,
                    ((PrimarySource)source).DbId, playlist_source.DbId);
            else if (sm_playlist_source != null)
                reader = ServiceManager.DbConnection.Query (sm_playlist_select_command, artist_id,
                    ((PrimarySource)source).DbId, sm_playlist_source.DbId);
            else
                reader = ServiceManager.DbConnection.Query (default_select_command,
                    artist_id, ((PrimarySource)source).DbId);

            List<AlbumInfo> albums = new List<AlbumInfo> ();
            while (reader.Read ()) {
                AlbumInfo album = (provider.Load (reader)) as AlbumInfo;
                if (album != null)
                       albums.Add (album);
            }
            return albums.ToArray ();
        }

        private int GetAllAlbumsCount ()
        {
            PrimarySource source = ServiceManager.SourceManager.ActiveSource as PrimarySource;
            if (source != null) {
                IDataReader reader = ServiceManager.DbConnection.Query (countall_select_command, source.DbId);
                if (reader.Read ())
                    return reader.Get<int> (0);
            }
            return 0;
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            bool is_queryalble_source = ServiceManager.SourceManager.ActiveSource is PrimarySource;
            is_queryalble_source = is_queryalble_source || ServiceManager.SourceManager.ActiveSource is Playlist.PlaylistSource;
            is_queryalble_source = is_queryalble_source || ServiceManager.SourceManager.ActiveSource is SmartPlaylist.SmartPlaylistSource;

            if (BoundObject == null) {
                return;
            }

            if (!(BoundObject is ArtistInfo)) {
                throw new InvalidCastException ("ColumnCellArtist can only bind to ArtistInfo objects");
            }
            ArtistInfo artist = (ArtistInfo)BoundObject;

            DatabaseArtistInfo db_artist = DatabaseArtistInfo.FindOrCreate(artist.Name, artist.NameSort);

            AlbumInfo[] albums = GetAlbums (db_artist.DbId);
            int album_count = albums.Length;

            string pattern = Catalog.GetString ("All Artists ({0})")
                .Replace ("(", "\\(")
                .Replace (")", "\\)")
                .Replace ("{0}", "[0-9]+");

            if (!String.IsNullOrEmpty (artist.Name) && System.Text.RegularExpressions.Regex.IsMatch (artist.Name, pattern))
                album_count = GetAllAlbumsCount ();

            ImageSurface image = null;
            List<ImageSurface> images = new List<ImageSurface> ();

            int non_empty = 0;
            for (int i = 0; i < albums.Length && non_empty < 3; i++)
                if (artwork_manager != null) {
                    ImageSurface sur = artwork_manager.LookupScaleSurface (albums[i].ArtworkId, image_size, true);
                    images.Add (sur);
                    if (sur != null)
                        non_empty++;
                }

            //bringing non-empty images to the front
            images.Sort (delegate (ImageSurface a, ImageSurface b) {
                if (a == null && b != null) return 1;
                if (a != null && b == null) return -1;
                return 0;
            });

            if (images.Count > 3)
                images.RemoveRange (3, images.Count - 3);

            if (images.Count == 3 && images[2] == null) {
                images[2] = images[1];
                images[1] = null;
            }

            bool is_default = false;
            if (images.Count == 0) {
                image = default_cover_image;
                is_default = true;
            } else {
                image = images[0];
            }

            int image_render_size = image_size;
            int x = image_spacing;
            int y = image_spacing;

            if (use_small_images) {
                x = image_spacing / 2;
                y = image_spacing / 2;
            }

            int y_image_spacing = 1;
            int x_offset = (use_small_images ? album_spacing_small : album_spacing_normal);
            if (images.Count > 1)
                x_offset *= 2;
            int y_offset = (images.Count > 1) ? y_image_spacing * 2 : y_image_spacing;

            for (int i = 1; i < images.Count; i++) {
                int move_x = x + ((i - 1) * (use_small_images ? album_spacing_small : album_spacing_normal));
                int move_y = y + ((i - 1) * y_image_spacing);
                ArtworkRenderer.RenderThumbnail (context.Context, images[i], false, move_x, move_y,
                        image_render_size, image_render_size, !is_default, context.Theme.Context.Radius, true, new Color (1.0, 1.0, 1.0, 1.0));
            }

            if (images.Count > 0)
                ArtworkRenderer.RenderThumbnail (context.Context, image, false, x + x_offset, y + y_offset,
                        image_render_size, image_render_size, !is_default, context.Theme.Context.Radius, true, new Color (1.0, 1.0, 1.0, 1.0));
            else
                ArtworkRenderer.RenderThumbnail (context.Context, image, false, x + x_offset, y + y_offset,
                        image_render_size, image_render_size, !is_default, context.Theme.Context.Radius);

            int fl_width = 0, fl_height = 0, sl_width = 0, sl_height = 0;
            Cairo.Color text_color = context.Theme.Colors.GetWidgetColor (GtkColorClass.Text, state);
            text_color.A = 0.75;

            Pango.Layout layout = context.Layout;
            layout.Width = (int)((cellWidth - cellHeight - x - 10) * Pango.Scale.PangoScale);
            layout.Ellipsize = Pango.EllipsizeMode.End;
            layout.FontDescription.Weight = Pango.Weight.Bold;

            // Compute the layout sizes for both lines for centering on the cell
            int old_size = layout.FontDescription.Size;

            layout.SetText (artist.DisplayName);
            layout.GetPixelSize (out fl_width, out fl_height);

            layout.FontDescription.Weight = Pango.Weight.Normal;
            layout.FontDescription.Size = (int)(old_size * Pango.Scale.Small);
            layout.FontDescription.Style = Pango.Style.Italic;

            string album_string_singular = Catalog.GetString ("Album");
            string album_string_plural = Catalog.GetString ("Albums");

            layout.SetText (album_count + " " + ((album_count == 1) ? album_string_singular : album_string_plural));
            layout.GetPixelSize (out sl_width, out sl_height);

            // Calculate the layout positioning
            x = ((int)cellHeight - x) + (use_small_images ? album_spacing_small : album_spacing_normal) + 8;
            if (use_small_images)
                y = (int)((cellHeight - (fl_height)) / 2);
            else
                y = (int)((cellHeight - (fl_height + sl_height)) / 2);

            // Render the second line first since we have that state already
            if (album_count > 0 && is_queryalble_source) {
                if (use_small_images)
                    context.Context.MoveTo (cellWidth - sl_width - image_spacing, y + image_spacing / 2);
                else
                    context.Context.MoveTo (x, y + fl_height);
                context.Context.Color = text_color;
                if (!use_small_images || fl_width + x + sl_width <= cellWidth)
                    PangoCairoHelper.ShowLayout (context.Context, layout);
            }

            // Render the first line, resetting the state
            layout.SetText (artist.DisplayName);
            layout.FontDescription.Weight = Pango.Weight.Bold;
            layout.FontDescription.Size = old_size;
            layout.FontDescription.Style = Pango.Style.Normal;

            layout.SetText (artist.DisplayName);

            context.Context.MoveTo (x, y);
            text_color.A = 1;
            context.Context.Color = text_color;
            PangoCairoHelper.ShowLayout (context.Context, layout);
        }

        public virtual int ComputeRowHeight (Widget widget)
        {
            int height;
            int text_w, text_h;

            Pango.Layout layout = new Pango.Layout (widget.PangoContext);
            layout.FontDescription = widget.PangoContext.FontDescription;

            layout.FontDescription.Weight = Pango.Weight.Bold;
            layout.SetText ("W");
            layout.GetPixelSize (out text_w, out text_h);
            height = text_h;

            layout.FontDescription.Weight = Pango.Weight.Normal;
            layout.FontDescription.Size = (int)(layout.FontDescription.Size * Pango.Scale.Small);
            layout.FontDescription.Style = Pango.Style.Italic;
            layout.SetText ("W");
            layout.GetPixelSize (out text_w, out text_h);
            if (!use_small_images)
                height += text_h;

            layout.Dispose ();

            if (use_small_images)
                return (height < image_size ? image_size : height) + 6;
            else
                return (height < image_size ? image_size : height) + 6 + image_spacing;
        }
    }
}