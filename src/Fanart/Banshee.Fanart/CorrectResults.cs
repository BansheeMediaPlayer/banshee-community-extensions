//
// CorrectResults.cs
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
using Hyena.Json;

using System.Linq;
using System.Collections;

using Utils;

namespace Banshee.Fanart
{
    public class CorrectResults : Results
    {
        public String Artist { get; private set; }
        public ArtistImageInfo[] ArtistImageInfos { get; private set; }

        public ArtistImageInfo BestArtistImageInfo {
            get { 
                if (ArtistImageInfos.Length > 0) {
                    return ArtistImageInfos[0];
                } else {
                    return null;
                }
            }
        }

        public CorrectResults (JsonObject results)
        {
            if (results == null) {
                throw new ArgumentNullException ("results");
            }
            if (results.Keys.Count != 0) {
                Artist = results.Keys.First ();
                var artistObject = results.Get<JsonObject> (Artist);

                ArtistImageInfos = artistObject.Get<JsonArray> ("musiclogo")
                    .Select (elem => new ArtistImageInfo (elem as JsonObject))
                    .ToArray ();
            } else {
                ArtistImageInfos = new ArtistImageInfo [] {};
                Hyena.Log.Debug ("No results in CorrectResults constructor");
            }
        }
    }

    public class ArtistImageInfo
    {
        public long Id { get; private set; }
        public String Url { get; private set; }
        public long Likes { get; private set; }

        public ArtistImageInfo (JsonObject json)
        {
            Id = json.Get<long> ("id");
            Url = json.Get<String> ("url");
            Likes = json.Get<long> ("likes");
        }
    }
}

