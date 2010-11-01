//
// SongFactory.cs
//
// Author:
//       John Moore <jcwmoore@gmail.com>
//
// Copyright (c) 2010 John Moore
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
using System.Xml.Linq;
using System.Linq;

namespace Banshee.Ampache
{
    internal class SongFactory : FactoryBase<AmpacheSong>, IEntityFactory<AmpacheSong>
    {
        public ICollection<AmpacheSong> Construct(ICollection<XElement> raw)
        {
            return new HashSet<AmpacheSong>(raw.Select(n=> Construct(n)));
        }

        public AmpacheSong Construct(XElement raw)
        {
            var result = BuildBase(raw);
            int tmp = int.MinValue;
            result.Id = int.Parse(raw.Attribute("id").Value);
            result.TrackTitle = raw.Descendants("title").First().Value;
            result.TrackTitleSort = raw.Descendants("title").First().Value;
            result.Uri = new Hyena.SafeUri(raw.Descendants("url").First().Value);
            result.ArtUrl = raw.Descendants("art").First().Value;
            int.TryParse(raw.Descendants("track").First().Value, out tmp);
            result.TrackNumber = tmp;
            tmp = 0;
            int.TryParse(raw.Descendants("time").First().Value, out tmp);
            result.Duration = TimeSpan.FromSeconds(tmp);
            tmp = int.MinValue;
            int.TryParse(raw.Descendants("size").First().Value, out tmp);
            result.FileSize = tmp;
            result.ArtistId = int.Parse(raw.Descendants("artist").First().Attribute("id").Value);
            result.AlbumId = int.Parse(raw.Descendants("album").First().Attribute("id").Value);
            return result;
        }
    }
}
