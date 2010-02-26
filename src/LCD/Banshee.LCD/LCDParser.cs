//
// LCDParser.cs
//
// Authors:
//   André Gaul
//
// Copyright (C) 2010 André Gaul
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
using System.Collections.Generic;
using Banshee.Collection;
using Banshee.MediaEngine;
using Banshee.ServiceStack;


namespace Banshee.LCD
{
    public class LCDParser
    {
        Dictionary<string, string> replaces;

        public LCDParser()
        {
            this.replaces = new Dictionary<string, string>();
            replaces["%A"] = ""; //artist
            replaces["%T"] = ""; //title
            replaces["%B"] = ""; //album
            replaces["%N"] = ""; //track number, e.g. " 1"
            replaces["%C"] = ""; //track count, e.g.  "23"
            replaces["%Y"] = ""; //year
            replaces["%G"] = ""; //genre
            replaces["%P"] = ""; //position, e.g. " 0:00"
            replaces["%L"] = ""; //length,   e.g. " 0:00"
            replaces["%S"] = ""; //status as word, e.g. "Playing"
        }

        public void ProcessEvent(PlayerEventArgs args, TrackInfo track, uint pos, uint len)
        {
            switch(args.Event) {
            case PlayerEvent.StateChange:
                StateChange((PlayerEventStateChangeArgs)args, track, pos, len);
                break;
            case PlayerEvent.TrackInfoUpdated:
                UpdateTrack(track, pos, len);
                break;
            case PlayerEvent.Iterate:
                UpdatePosition(pos, len);
                break;
            }

        }

        private void StateChange(PlayerEventStateChangeArgs args, TrackInfo track, uint pos, uint len)
        {
            replaces["%S"] = args.Current.ToString();
            switch(args.Current){
            case PlayerState.Playing:
            case PlayerState.Paused:
                UpdateTrack(track,pos,len);
                break;
            case PlayerState.NotReady:
                Reset();
                break;
            }
        }


        private void UpdateTrack(TrackInfo track, uint pos, uint len)
        {
            if (track!=null)
            {
                replaces["%A"] = track.DisplayArtistName;
                replaces["%T"] = track.DisplayTrackTitle;
                replaces["%B"] = track.DisplayAlbumTitle;
                replaces["%N"] = track.TrackNumber.ToString().PadLeft(2);
                replaces["%C"] = track.TrackCount.ToString().PadLeft(2);
                UpdatePosition(pos, len);
                replaces["%Y"] = track.Year.ToString();
                replaces["%G"] = ""; //genre
            }
        }

        private void UpdatePosition(uint pos, uint len)
        {
            replaces["%P"] = ((pos/1000)/60).ToString().PadLeft(2)
                +":"+((pos/1000)%60).ToString().PadLeft(2, '0');
            replaces["%L"] = ((len/1000)/60).ToString().PadLeft(2)
                +":"+((len/1000)%60).ToString().PadLeft(2, '0');
        }

        private void Reset()
        {
            foreach(string key in replaces.Keys)
                if (replaces[key].Length>0)
                    replaces[key].Remove(0);
        }

        public string Parse(string format)
        {
            foreach(KeyValuePair<string,string> pair in replaces)
                format=format.Replace(pair.Key, pair.Value);
            return format;
        }
    }
}