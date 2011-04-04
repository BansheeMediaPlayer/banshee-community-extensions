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

using Banshee.Collection;

namespace Banshee.Lyrics.IO
{
    public class LyricsCache
    {

        private string GetLyricsFilename (string artist, string title)
        {
            if (!String.IsNullOrEmpty (artist) && artist.Contains (Path.DirectorySeparatorChar.ToString ())) {
                artist = artist.Replace (Path.DirectorySeparatorChar.ToString (), "_");
            }
            if (title.Contains (Path.DirectorySeparatorChar.ToString())) {
                title = title.Replace (Path.DirectorySeparatorChar.ToString(), "_");
            }
            return LyricsService.LyricsDir + artist + "_" + title + ".lyrics";
        }

        public void DeleteLyrics (TrackInfo track)
        {
            string filename = GetLyricsFilename (track.ArtistName, track.TrackTitle);
            DeleteLyrics (filename);
        }

        public void DeleteLyrics (string filename)
        {
            try {
                if (File.Exists (filename)) {
                    File.Delete (filename);
                }
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Can't delete lyrics file: {0}", filename), e);
            }
        }

        public string ReadLyrics (TrackInfo track)
        {
            string filename = GetLyricsFilename (track.ArtistName, track.TrackTitle);
            return ReadLyrics (filename);
        }

        public string ReadLyrics (String filename)
        {
            try {
                if (File.Exists (filename)) {
                    return File.ReadAllText (filename);
                }
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Can't read lyrics file: {0}", filename), e);
            }
            return null;
        }

        public void WriteLyrics (TrackInfo track, string lyrics)
        {
            string filename = GetLyricsFilename (track.ArtistName, track.TrackTitle);
            try {
                File.WriteAllText (filename, lyrics);
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Can't write lyrics file: {0}", filename), e);
            }
            return;
        }

        public bool IsInCache (TrackInfo track)
        {
            if (track == null) {
                return false;
            }
            string filename = GetLyricsFilename (track.ArtistName, track.TrackTitle);
            return  File.Exists (filename);
        }
    }
}
