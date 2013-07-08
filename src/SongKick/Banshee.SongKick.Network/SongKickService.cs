//
// SongKickService.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using Banshee.ServiceStack;
using Banshee.SongKick.Recommendations;
using Hyena.Jobs;

namespace Banshee.SongKick.Network
{
    public class SongKickService : IExtensionService
    {
        
        Scheduler scheduler = new Scheduler ();

        public SongKickService ()
        {
        }

        public string ServiceName {
            get { return "SongKickService"; }
        }

        public void Initialize ()
        {
            //for testing only:

            //text file:
            //string uri = @"http://textfiles.serverrack.net/computers/1003v-mm";

            //events in London:
            string uri = @"http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=Qjqhc2hkfU3BaTx6";

            //events recomended for tmtimon user
            //string uri =  @"http://api.songkick.com/api/3.0/users/tmtimon/calendar.json?reason=tracked_artist&apikey=Qjqhc2hkfU3BaTx6"

            //invalid API key:
            //string uri = @"http://api.songkick.com/api/3.0/metro_areas/24426/calendar.json?apikey=invalidKey";


            var downloadJob = new DownloadRecommendationsJob(uri, Events.GetMusicEventListResultsDelegate);
            scheduler.Add(downloadJob);
        }

        public void Dispose ()
        {
            scheduler.CancelAll (true);
        }
    }
}

