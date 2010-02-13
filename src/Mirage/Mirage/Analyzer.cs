/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007-2008 Dominik Schnitzer <dominik@schnitzer.at>
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
using System.Linq;
using System.Data;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Mirage
{
    public class MirAnalysisImpossibleException : Exception
    {
    }

    public class Analyzer
    {
        private const int samplingrate = 22050;
        private const int windowsize = 1024;
        private const int melcoefficients = 36;
        private const int mfcccoefficients = 20;
        private const int secondstoanalyze = 120;
        
        private static Mfcc mfcc = new Mfcc(windowsize, samplingrate, melcoefficients, mfcccoefficients);

        private static AudioDecoder ad = new AudioDecoder(samplingrate, secondstoanalyze, windowsize);
        
        public static void CancelAnalyze()
        {
            ad.CancelDecode();
        }

        public static Scms Analyze (string file)
        {
            
            try {
                DbgTimer t = new DbgTimer();
                t.Start();
                
                Matrix stftdata = ad.Decode(file);
                Matrix mfccdata = mfcc.Apply(ref stftdata);
                Scms scms = Scms.GetScms(ref mfccdata);
            
                long stop = 0;
                t.Stop(ref stop);
                Dbg.WriteLine("Mirage - Total Execution Time: {0}ms", stop);
                
                return scms;
                
            } catch (Exception) {
                throw new MirAnalysisImpossibleException();
            }
        }

        public static int [] SimilarTracks (int [] seed_track_ids, int [] exclude_track_ids, Db db, int length)
        {
            return SimilarTracks (seed_track_ids, exclude_track_ids, db, length, 0);
        }
        
        public static int [] SimilarTracks (int [] seed_track_ids, int [] exclude_track_ids, Db db, int length, float distceiling)
        {
            DbgTimer t = new DbgTimer();
            t.Start();

            // Get Seed-Song SCMS
            Scms [] seedScms = new Scms[seed_track_ids.Length];
            for (int i = 0; i < seedScms.Length; i++) {
                seedScms[i] = new Scms(mfcccoefficients);
                db.GetTrack (seed_track_ids[i], ref seedScms[i]);
            }

            // Get all tracks from the DB except the seedSongs
            Hashtable ht = new Hashtable ();
            Scms[] scmss = new Scms[100];
            for (int i = 0; i < 100; i++) {
                scmss[i] = new Scms (mfcccoefficients);
            }

            int [] mapping = new int[100];
            int read = 1;
            
            // Allocate the Scms Distance cache
            ScmsConfiguration c = new ScmsConfiguration (mfcccoefficients);
            
            IDataReader r = db.GetTracks (exclude_track_ids);
            while (read > 0) {
                read = db.GetNextTracks (ref r, ref scmss, ref mapping, 100);
                for (int i = 0; i < read; i++) {

                    float d = 0;
                    int count = 0;
                    for (int j = 0; j < seedScms.Length; j++) {
                        float dcur = Scms.Distance (ref seedScms[j], ref scmss[i], ref c);
                        
                        // Possible negative numbers indicate faulty scms models..
                        if (dcur >= 0) {
                            d += dcur;
                            count++;
                        } else {
                            Dbg.WriteLine ("Mirage - Faulty SCMS id={0} d={1}", mapping[i], d);
                            d = float.MaxValue;
                            break;
                        }
                    }
                    
                    // Exclude track if it's too close to the seeds
                    if (d > distceiling) {
                        ht.Add (mapping[i], d/count);
                    } else {
                        Dbg.WriteLine ("Mirage - ignoring {0}", mapping[i]);
                    }
                }
            }

            db.GetTracksFinished ();
            
            float [] items = new float[ht.Count];
            int [] keys = new int[ht.Keys.Count];
            
            ht.Keys.CopyTo (keys, 0);
            ht.Values.CopyTo (items, 0);
            
            Array.Sort (items, keys);
            Array.Resize (ref keys, length);
            
            long stop = 0;
            t.Stop (ref stop);
            Dbg.WriteLine ("Mirage - playlist in: {0}ms", stop);
            
            return keys;
        }

        public static Dictionary<int, Scms> LoadLibrary (ref Db db)
        {
            int [] mapping = new int[100];
            int [] exclude = new int[0];
            Dictionary<int, Scms> loadedDb = new Dictionary<int, Scms> ();

            IDataReader r = db.GetTracks (exclude);
            int read;
            do {
                Scms[] scmss = new Scms[100];
                for (int i = 0; i < 100; i++) {
                    scmss[i] = new Scms(mfcccoefficients);
                }

                read = db.GetNextTracks(ref r, ref scmss, ref mapping, 100);
                for (int i = 0; i < read; i++) {
                    loadedDb.Add(mapping[i], scmss[i]);
                }
            } while (read > 0);

            return loadedDb;
        }

        public static List<int> DuplicateSearch(Dictionary<int, Scms> loadedDb, int trackId, float threshold)
        {
            Scms seed = loadedDb[trackId];
            ScmsConfiguration c = new ScmsConfiguration(mfcccoefficients);
            List<int> duplicates = new List<int>();
            foreach(KeyValuePair<int, Scms> item in loadedDb) {
                Scms song = item.Value;
                int songId = item.Key;
                if (songId != trackId) {
                    float dcur = Scms.Distance(ref seed, ref song, ref c);
                    if (dcur <= threshold) {
                        duplicates.Add(item.Key);
                    }
                }
            }

            return duplicates;
        }
    }
}
