//
// RecommendationProcessor.cs
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
using System.Collections.Generic;
using Banshee.SongKick.Recommendations;

namespace Banshee.SongKick.Search
{
    public delegate void AddSongKickInfo(RecommendedArtist artist, 
                                         ResultsPage<Event> songKickFirstAtristEvents);

    public class RecommendationProcessor
    {
        private Queue<RecommendedArtist> artist_queue = 
            new Queue<RecommendedArtist>();

        public AddSongKickInfo AddSongKickInfo { get; private set; }

        public RecommendationProcessor (AddSongKickInfo addSongKickInfo)
        {
            AddSongKickInfo = addSongKickInfo;
        }

        public void EnqueueArtists(IEnumerable<RecommendedArtist> artists)
        {
            lock (artist_queue) {
                foreach (var artist in artists) {
                    artist_queue.Enqueue (artist);
                }
            }
        }

        public void ProcessAll() {
            const int numberOfThreads = 3;
            for (int i = 0; i < numberOfThreads; i++) {
                System.Threading.Thread thread = 
                    new System.Threading.Thread (
                        new System.Threading.ThreadStart (
                            () => ProcessAllOneThread ()));
                thread.Start();
            }
        }

        private void ProcessAllOneThread()
        {
            try {
                while (artist_queue.Count > 0) {
                    RecommendedArtist artist = null;
                    lock (artist_queue) {
                        if (artist_queue.Count > 0) {
                            artist = artist_queue.Dequeue();
                        }
                    }
                    if (artist != null) {
                        Process (artist);
                    }
                }
            } catch (InvalidOperationException e) { // this exception should never be thrown by Dequeue() thanks to lock
                Hyena.Log.Exception (e);
            }
        }

        private void Process(RecommendedArtist artist)
        {
            var search = new EventsByArtistSearch();
            search.GetResultsPage (new Banshee.SongKick.Search.Query(null, artist.Name));
            AddSongKickInfo (artist, search.ResultsPage);
        }
    }
}

