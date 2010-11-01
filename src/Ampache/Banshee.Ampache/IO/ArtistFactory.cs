//
// ArtistFactory.cs
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
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Banshee.Ampache
{
    internal class ArtistFactory : FactoryBase<AmpacheArtist>, IEntityFactory<AmpacheArtist>
    {
        public ICollection<AmpacheArtist> Construct(ICollection<XElement> raw)
        {
            return new HashSet<AmpacheArtist>(raw.Select(n=>Construct(n)));
        }

        public AmpacheArtist Construct(XElement raw)
        {
            var result = this.BuildBase(raw);
            result.Id = int.Parse(raw.Attribute("id").Value);
            result.Name = raw.Descendants("name").First().Value;
            result.NameSort = raw.Descendants("name").First().Value;
            result.AlbumCount = int.Parse(raw.Descendants("albums").First().Value);
            result.SongCount = int.Parse(raw.Descendants("songs").First().Value);
            return result;
        }
    }
}
