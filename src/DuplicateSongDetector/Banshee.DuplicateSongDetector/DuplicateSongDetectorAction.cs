//
// DuplicateSongDetectorAction.cs
//
// Authors:
//   Kevin Anthony <Kevin@NoSideRacing.com>
//
// Copyright (C) 2011 Kevin Anthony
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

using System;

using Mono.Addins;
using Mono.Unix;

using Banshee.Base;
using Banshee.Gui;
using Banshee.ServiceStack;

using Hyena;
using Hyena.Data.Sqlite;

namespace Banshee.DuplicateSongDetector
{
    public class DuplicateSongDetectorAction : BansheeActionGroup
    {
        public DuplicateSongDetectorAction () : base (AddinManager.CurrentLocalizer.GetString ("Detect Duplicate Songs"))
        {
            Add (new Gtk.ActionEntry ("DuplicateSongAction", null,
                AddinManager.CurrentLocalizer.GetString ("Detect Duplicate Songs"), null, null, onStartDetecting));
            AddUiFromFile ("GlobalUI.xml");
        }

        public void onStartDetecting (object o, EventArgs args)
        {
            var Source = new DuplicateSongDetectorSource ();
            ServiceManager.SourceManager.MusicLibrary.AddChildSource (Source);
            ServiceManager.SourceManager.SetActiveSource (Source);

            HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query (@"SELECT
                             CT.TrackID,CT.Title,CA.Title, CA.ArtistName,CT.URI
                             FROM CoreTracks CT,CoreAlbums CA ON Ct.AlbumID = CA.AlbumID
                             AND CT.TrackID IN (
                                 SELECT
                                     CT1.TrackID from CoreTracks CT1,CoreTracks CT2 where
                                     CT1.PrimarySourceID=1
                                     AND CT1.TrackID <> CT2.TrackID
                                     AND CT1.TitleLowered = CT2.TitleLowered
                                     AND CT1.TrackNumber = CT2.TrackNumber
                                     AND CT1.AlbumID = CT2.AlbumID
                                     AND CT1.ArtistID = CT2.ArtistID
                             )
                             ORDER BY CT.Title"));
            while (reader.Read ()) {
                int ID = reader.Get<int> (0);
                String Title = reader.Get<String> (1);
                String Album = reader.Get<String> (2);
                String Artist = reader.Get<String> (3);
                String URI = reader.Get<String>(4);
                SongDuplicateView.AddData (ID,Title,Album,Artist,URI);
            }
        }
    }
}

