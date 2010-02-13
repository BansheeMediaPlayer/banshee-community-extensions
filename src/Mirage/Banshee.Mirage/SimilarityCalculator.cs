/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
 *           (C) 2008 Bertrand Lorentz <bertrand.lorentz@gmail.com>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

using System;

using Hyena;
using Mirage;

namespace Banshee.Mirage
{
    public class SimilarityCalculator
    {
        int[] seek_track_ids;
        int[] exclude_track_ids;
        PlaylistGeneratorSource.UpdatePlaylistDelegate update_playlist_delegate;
        Db db;
        bool append;

        public SimilarityCalculator(int [] seed_track_ids, int [] exclude_track_ids,
                Db db, PlaylistGeneratorSource.UpdatePlaylistDelegate update_playlist_delegate,
                bool append)
        {
            this.seek_track_ids = seed_track_ids;
            this.exclude_track_ids = exclude_track_ids;
            this.update_playlist_delegate = update_playlist_delegate;
            this.db = db;
            this.append = append;
        }

        public void Compute()
        {
            int[] playlist_track_ids;
            try {
                // We generate a longer playlist because some tracks might be thrown away later
                int generated_length = 4 * MirageConfiguration.PlaylistLength.Get ();
                float distceiling = (float)MirageConfiguration.DistanceCeiling.Get ();

                //Log.DebugFormat ("Distance ceiling is {0}", distceiling);

                playlist_track_ids = Analyzer.SimilarTracks (seek_track_ids, exclude_track_ids, db, generated_length, distceiling);

                update_playlist_delegate (playlist_track_ids, append);
            } catch (DbTrackNotFoundException) {
                //Log.Error ("Mirage: ERROR. Track not found in Mirage DB");
                update_playlist_delegate (null, false);
            }
        }
    }
}