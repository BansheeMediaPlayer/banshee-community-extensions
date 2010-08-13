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
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Mirage
{
    public class Analyzer
    {
        private const int SAMPLING_RATE = 22050;
        private const int WINDOW_SIZE = 1024;
        private const int MEL_COEFFICIENTS = 36;
        public const int MFCC_COEFFICIENTS = 20;
        private const int SECONDS_TO_ANALYZE = 120;

        private static Mfcc mfcc = new Mfcc (WINDOW_SIZE, SAMPLING_RATE, MEL_COEFFICIENTS, MFCC_COEFFICIENTS);
        private static AudioDecoder ad = new AudioDecoder (SAMPLING_RATE, SECONDS_TO_ANALYZE, WINDOW_SIZE);

        public static void Init () {}

        public static void CancelAnalyze ()
        {
            ad.CancelDecode ();
        }

        public static Scms Analyze (string file_path)
        {
            DbgTimer t = new DbgTimer ();
            t.Start ();

            Matrix stftdata = ad.Decode (file_path);
            Matrix mfccdata = mfcc.Apply (ref stftdata);
            Scms scms = Scms.GetScms (mfccdata);

            long stop = 0;
            t.Stop (ref stop);
            Dbg.WriteLine ("Mirage - Total Execution Time: {0}ms", stop);

            return scms;
        }
    }
}
