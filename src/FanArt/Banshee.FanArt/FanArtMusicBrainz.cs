//
// FanArtMusicBrainz.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//   Dmitrii Petukhov <dimart.sp@gmail.com>

// Copyright 2013 Tomasz Maczyński
// Copyright 2014 Dmitrii Petukhov
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

using Hyena.Data.Sqlite;

using Banshee.ServiceStack;

namespace Banshee.FanArt
{
    public static class FanArtMusicBrainz
    {
        public static string MBIDByArtistID (long dbId) {
            return ServiceManager.DbConnection.Query<string> (
                    String.Format (@"SELECT MusicBrainzID FROM CoreArtists WHERE ArtistID={0} LIMIT 1" , dbId)
                );
        }

        public static string MBIDByArtistName (string artistName)
        {
            return ServiceManager.DbConnection.Query<string> (
                    new HyenaSqliteCommand (@"SELECT MusicBrainzID FROM ArtistMusicBrainz WHERE ArtistName=? LIMIT 1" , artistName)
                );
        }

        public static string ArtistNameByMBID (string mbId)
        {
            return ServiceManager.DbConnection.Query<string>
                (new HyenaSqliteCommand (@"SELECT ArtistName
                                           FROM ArtistMusicBrainz
                                           WHERE MusicBrainzId = ? LIMIT 1",
                                           mbId));
        }

        public static bool HasImage (string mbId) {
            return ServiceManager.DbConnection.Query<bool> (
                new HyenaSqliteCommand (@"
                    SELECT COUNT(*)>0 FROM ArtistImageDownloads 
                    WHERE MusicBrainzID=? AND 
                    Downloaded=1",
                    mbId)
                );
        }
    }
}

