//
// FanartArtistColumnCell.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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

using Banshee.Collection.Gui;
using Banshee.Gui;
using Gtk;
using Hyena.Data.Gui;
using Hyena.Gui;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Fanart.UI
{
    public class FanartArtistColumnCell : ColumnCell
    {


        public FanartArtistColumnCell () : base (null, true)
        {
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            if (BoundObject == null) {
                return;
            } 

            // TODO: check DatabaseArtistInfo
            var artistInfo = BoundObject as ArtistInfo;
            if (artistInfo == null) {
                throw new InvalidCastException ("FanartArtistColumnCell can only bind ArtistInfo objects");
            }

            int spacing = 2;

            int thumb_size = 22;
            int image_size = thumb_size - (2 * spacing);

            // TODO: improve image
            var defaultImage = PixbufImageSurface.Create (IconThemeUtils.LoadIcon (image_size, "applications-multimedia"));
            var image = defaultImage;

            // TODO: get MBDI using costum queries
            // currently musicBrainzID is always null
            string musicBrainzID = null;

            var dbAlbumArtistInfo = artistInfo as DatabaseAlbumArtistInfo;
            if (dbAlbumArtistInfo != null) {
                Hyena.Log.Debug (String.Format ("artistInfo is a DatabaseArtistInfo ({0})", dbAlbumArtistInfo));

                // TODO: check id dbAlbumArtistInfo.MusicBrainzId is artist's or album's MBID
                musicBrainzID = FanartMusicBrainz.MBIDByArtistID (dbAlbumArtistInfo.DbId);
            }

            if (musicBrainzID == null) {
                musicBrainzID = FanartMusicBrainz.MBIDByArtistName (artistInfo.Name);
            }

            //TODO: improve code below:
            if (musicBrainzID != null) {
                try {
                    string imagePath = FanartArtistImageSpec.GetPath (
                            FanartArtistImageSpec.CreateArtistImageFileName(musicBrainzID)
                        );
                    var artistPixbuf = new Gdk.Pixbuf (imagePath);
                    artistPixbuf = artistPixbuf.ScaleSimple (image_size, 2 * image_size, Gdk.InterpType.Bilinear);
                    var artistImage = PixbufImageSurface.Create (artistPixbuf);

                    image = artistImage;
                } catch (Exception e) {
                    Hyena.Log.Debug (String.Format (
                        "Could not get artist image for artist '{0}' with MBDI {1}, because exception {2} was thrown", 
                        artistInfo.Name ?? "", musicBrainzID ?? "", e));
                }
            }


            bool has_border = false;

            ArtworkRenderer.RenderThumbnail (context.Context, image, false, 
                                            spacing, spacing,
                                            thumb_size, thumb_size, 
                                            has_border, context.Theme.Context.Radius);
        }
    }
}

