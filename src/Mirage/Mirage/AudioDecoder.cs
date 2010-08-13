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
using System.Collections;
using System.Runtime.InteropServices;

namespace Mirage
{
    public class AudioDecoderErrorException : Exception
    {
    }

    public class AudioDecoderCanceledException : Exception
    {
    }

    public class AudioDecoder
    {
        [DllImport("libmirageaudio")]
        static extern IntPtr mirageaudio_initialize (int rate, int seconds, int winsize);

        [DllImport("libmirageaudio")]
        static extern IntPtr mirageaudio_decode (IntPtr ma, string file, ref int frames, ref int size, ref int ret);

        [DllImport("libmirageaudio")]
        static extern IntPtr mirageaudio_destroy (IntPtr ma);

        [DllImport("libmirageaudio")]
        static extern void mirageaudio_canceldecode (IntPtr ma);

        IntPtr ma;

        public AudioDecoder (int rate, int seconds, int winsize)
        {
            ma = mirageaudio_initialize (rate, seconds, winsize);
        }

        public Matrix Decode (string file)
        {
            int frames = 0;
            int size = 0;
            int ret = 0;

            IntPtr data = mirageaudio_decode (ma, file, ref frames, ref size, ref ret);
            if (ret == -1) {
                throw new AudioDecoderErrorException ();
            } else if (ret == -2) {
                throw new AudioDecoderCanceledException ();
            } else if ((frames <= 0) || (size <= 0)) {
                throw new AudioDecoderErrorException (); // No data
            }

            Dbg.WriteLine ("Mirage - decoded frames={0},size={1}", frames, size);

            // Sort the frames by total energy (frame selection)
            float [] frameselection = new float[frames];
            int [] framepos = new int[frames];
            unsafe {
                float* stft_unsafe = (float*)data;
                for (int j = 0; j < frames; j++) {
                    frameselection[j] = 0;
                    framepos[j] = j;
                    for (int i = 0; i < size; i++) {
                        frameselection[j] += stft_unsafe[i * frames + j];
                    }
                }
            }

            Array.Sort (frameselection, framepos);

            // Save the high energy frames to the Matrix
            int copyframes = frames / 2;
            Matrix stft = new Matrix (size, copyframes);

            unsafe {
                float* stft_unsafe = (float*)data;
                fixed (float* stftd = stft.d) {
                    for (int j = 0; j < copyframes; j++) {
                        for (int i = 0; i < size; i++) {
                            stftd[i * copyframes + j] = stft_unsafe[i * frames + framepos[copyframes + j]];
                        }
                    }
                }
            }

            return stft;
        }

        ~AudioDecoder ()
        {
            mirageaudio_destroy (ma);
            ma = IntPtr.Zero;
        }

        public void CancelDecode ()
        {
            mirageaudio_canceldecode (ma);
        }
    }
}