
using System;
using Banshee.Collection;
using Banshee.Streaming;

namespace Banshee.Lyrics.IO
{
    public static class TagLibUtils
    {

        private static string DeTagLyric (string lyric) 
        {
            String l = lyric.Replace("<br/>","\n");
            l = l.Replace("<br />","\n");
            l = l.Replace("<br>","\n");
            return l;
        }

        public static bool SaveToID3 (TrackInfo track, string lyric)
        {
            // Note: this should be kept in sync with the metadata read in StreamTagger.cs
            TagLib.File file = StreamTagger.ProcessUri (track.Uri);
            if (file == null) {
                return false;
            }

            file.Tag.Lyrics = DeTagLyric(lyric);
            file.Save ();

            track.FileSize = Banshee.IO.File.GetSize (track.Uri);
            track.FileModifiedStamp = Banshee.IO.File.GetModifiedTime (track.Uri);
            track.LastSyncedStamp = DateTime.Now;
            return true;
        }
    }
}
