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
using System.Runtime.InteropServices;

namespace Mirage
{

public class AudioDecoder
{
    [DllImport("libmirageaudio")]
    static extern IntPtr mirageaudio_initialize(int rate, int seconds, int winsize);

    [DllImport("libmirageaudio")]
    static extern IntPtr mirageaudio_decode(IntPtr ma, string file, ref int frames, ref int size);

    [DllImport("libmirageaudio")]
    static extern IntPtr mirageaudio_destroy(IntPtr ma);

    IntPtr ma;

    public AudioDecoder(int rate, int seconds, int winsize)
    {
        ma = mirageaudio_initialize(rate, seconds, winsize);
    }

    public Matrix Decode(string file)
    {
        int frames = 0;
        int size = 0;

        IntPtr data = mirageaudio_decode(ma, file, ref frames, ref size);
        Dbg.WriteLine("Mirage: decoded frames="+frames+",size="+size);
        if (frames <= 0)
            return null;

        Matrix stft = new Matrix(size, frames);
        unsafe {
            float* stft_unsafe = (float*)data;
            fixed (float* stftd = stft.d) {
                for (int i = 0; i < frames*size; i++) {
                    stftd[i] = stft_unsafe[i];
                }
            }
        }

        return stft;
    }

    ~AudioDecoder()
    {
        ma = mirageaudio_destroy(ma);
    }
}
    
}
