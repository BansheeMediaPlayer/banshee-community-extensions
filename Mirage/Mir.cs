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
using System.Data;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Mirage
{


public class Mir
{
    private static int samplingrate = 11025;
    private static int windowsize = 512;
    
    public static Mfcc mfcc = new Mfcc(windowsize, samplingrate, 36, 20);
    public static AudioDecoder ad = new AudioDecoder(samplingrate, 135, windowsize);
    
    public static void CancelAnalyze()
    {
        ad.CancelDecode();
    }

    public static Scms Analyze(string file)
    {
        Timer t = new Timer();
        t.Start();

        Matrix stft1 = ad.Decode(file);
        if (stft1 == null)
            return null;
        Matrix mfcc1 = mfcc.Apply(stft1);
        Scms scms = Scms.GetScms(mfcc1);
        
        Dbg.WriteLine("Mirage: Total Execution Time: " + t.Stop() + "ms");
        
        return scms;
    }

    public static int[] SimilarTracks(int[] id, int[] exclude, Db db)
    {
        // Get Seed-Song SCMS
        Scms[] seedScms = new Scms[id.Length];
        for (int i = 0; i < seedScms.Length; i++) {
            seedScms[i] = db.GetTrack(id[i]);
        }
        
        // Get all tracks from the DB except the seedSongs
        IDataReader r = db.GetTracks(exclude);
        Hashtable ht = new Hashtable();
        Scms[] scmss = new Scms[100];
        int[] mapping = new int[100];
        int read = 1;
        float d;
        float dcur;
        float count;
        
        Timer t = new Timer();
        t.Start();
        
        while (read > 0) {
            
            read = db.GetNextTracks(ref r, ref scmss, ref mapping, 100);
            for (int i = 0; i < read; i++) {
                
                d = 0;
                count = 0;
                for (int j = 0; j < seedScms.Length; j++) {
                    dcur = seedScms[j].Distance(scmss[i]);
                    
                    // FIXME: Negative numbers indicate faulty scms models..
                    if (dcur > 0) {
                        d += dcur;
                        count++;
                    } else {
                        Dbg.WriteLine("Mirage: Faulty SCMS id=" + mapping[i] + "d=" + d);
                        d = 0;
                        break;
                    }
                }
                
                if (d > 0) {
                    ht.Add(mapping[i], d/count);
                }
            }
            
        }
        
        float[] items = new float[ht.Count];
        int[] keys = new int[ht.Keys.Count];
        
        ht.Keys.CopyTo(keys, 0);
        ht.Values.CopyTo(items, 0);
        
        Array.Sort(items, keys);
        
        Dbg.WriteLine("Mirage: playlist in: " + t.Stop() + "ms");
        
        return keys;
    }
}

}
