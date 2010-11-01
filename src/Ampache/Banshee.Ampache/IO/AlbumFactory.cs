//
// AlbumFactory.cs
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
    internal class AlbumFactory : FactoryBase<AmpacheAlbum>, IEntityFactory<AmpacheAlbum>
    {
        public ICollection<AmpacheAlbum> Construct(ICollection<XElement> raw)
        {
            return new HashSet<AmpacheAlbum>(raw.Select(n=>Construct(n)));
        }

        public AmpacheAlbum Construct(XElement raw)
        {
            var result = BuildBase(raw);
            result.Id = int.Parse(raw.Attribute("id").Value);
            result.ArtistId = int.Parse(raw.Descendants("artist").First().Attribute("id").Value);
            result.Title = raw.Descendants("name").First().Value;
            result.TitleSort = raw.Descendants("name").First().Value;
            int yr = 1900;
            int.TryParse(raw.Descendants("year").First().Value, out yr);
            result.Year = yr;
            result.ArtUrl = raw.Descendants("art").First().Value;
            return result;
        }
    }
}
