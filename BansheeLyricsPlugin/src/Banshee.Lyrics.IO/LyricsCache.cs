//  
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
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
// 

using System;
using System.Threading;
using System.IO;

namespace Banshee.Lyrics.IO
{
    public class LyricsCache
    {

        private string GetLyricsFilename (string artist, string title)
        {
            return LyricsService.lyrics_dir + artist + "_" + title + ".lyrics";
        }

        public void DeleteLyric (string artist, string title)
        {
            string filename = GetLyricsFilename (artist, title);
            DeleteLyric (filename);
        }

        public void DeleteLyric (string filename)
        {
            try {
                if (File.Exists (filename)) {
                    Hyena.Log.Debug ("Deleting lyric: " + filename);
                    File.Delete (filename);
                }
            } catch (Exception e) {
                Hyena.Log.DebugFormat ("Unable to delete lyric {0}: {1} ", filename, e.Message);
            }
        }

        public string ReadLyric (string artist, string title)
        {
            string filename = GetLyricsFilename (artist, title);
            return ReadLyric (filename);
        }

        public string ReadLyric (String filename)
        {
            try {
                if (File.Exists (filename)) {
                    return File.ReadAllText (filename);
                }
            } catch (Exception e) {
                Hyena.Log.DebugFormat ("Unable to read lyric {0}: {1} ", filename, e.Message);
            }
            return null;
        }

        public void WriteLyric (string artist, string title, string lyrics)
        {
            string filename = GetLyricsFilename (artist, title);
            try {
                if (File.Exists (filename)) {
                    return;
                }
                //create a new file
                FileStream stream = File.Create (filename);
                stream.Close ();
                //write the lyrics
                File.WriteAllText (filename, lyrics);
                Hyena.Log.Debug ("Lyric successfully written " + filename);
            } catch (Exception e) {
                Hyena.Log.DebugFormat ("Unable to save lyric {0}: {1} ", filename, e.Message);
            }
            return;
        }

        public bool IsInCache (string artist, string title)
        {
            string filename = GetLyricsFilename (artist, title);
            return  File.Exists (filename);
        }
    }
}
