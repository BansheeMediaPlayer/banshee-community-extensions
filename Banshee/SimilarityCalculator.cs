/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
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

using Mirage;

namespace Banshee.Plugins.Mirage
{
    public class SimilarityCalculator
    {
        int[] trackId;
        int[] excludeTrackId;
        PlaylistGeneratorSource.UpdatePlaylistDelegate play;
        int length;
        Db db;
        
        public SimilarityCalculator(int[] trackId, int[] excludeTrackId,
                Db db, PlaylistGeneratorSource.UpdatePlaylistDelegate playlist, int length)
        {
            this.trackId = trackId;
            this.play = playlist;
            this.length = length;
            this.excludeTrackId = excludeTrackId;
            this.db = db;
        }
        
        public void Compute()
        {
            int[] playlist;
            try {
                playlist = Mir.SimilarTracks(trackId, excludeTrackId, db);
                play(playlist, length);
            } catch (MirAnalysisImpossibleException) {
                Dbg.WriteLine("Mirage: ERROR. Impossible to compute playlist");
            }
        }
    }
}
