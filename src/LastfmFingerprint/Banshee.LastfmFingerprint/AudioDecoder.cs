//
// AudioDecoder.cs
//
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright (c) 2010 Olivier Dufour
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
using System.Runtime.InteropServices;

namespace Banshee.LastfmFingerprint
{

    public class AudioDecoderErrorException : Exception
    {
    }

    public class AudioDecoderCanceledException : Exception
    {
    }

    public class AudioDecoder
    {
        [DllImport("liblastfmfpbridge")]
        static extern IntPtr Lastfmfp_initialize (int seconds);

        [DllImport("liblastfmfpbridge")]
        static extern IntPtr Lastfmfp_decode (IntPtr ma, string file, ref int size, ref int ret);

        [DllImport("liblastfmfpbridge")]
        static extern IntPtr Lastfmfp_destroy (IntPtr ma);

        [DllImport("liblastfmfpbridge")]
        static extern void Lastfmfp_canceldecode (IntPtr ma);


        IntPtr ma;

        public AudioDecoder (int seconds)
        {
            ma = Lastfmfp_initialize (seconds);
        }

        public byte[] Decode (string file)
        {
            //int frames = 0;
            int size = 0;
            int ret = 0;

            IntPtr ptr = Lastfmfp_decode (ma, file, ref size, ref ret);
            byte[] buf = new byte[size];
            Marshal.Copy (ptr, buf, 0, size);

            return buf;
        }

        ~AudioDecoder ()
        {
            Lastfmfp_destroy (ma);
            ma = IntPtr.Zero;
        }

        public void CancelDecode ()
        {
            Lastfmfp_canceldecode (ma);
        }
    }
}

