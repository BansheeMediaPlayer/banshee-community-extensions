//
// Album.cs
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
using Banshee.Collection;

namespace Banshee.Ampache
{
    public class AmpacheAlbum : AlbumInfo, IRatable
    {
        public int ArtistId { get; set; }
        public int Year { get; set; }
        public string ArtUrl { get; set; }
        public override DateTime ReleaseDate { get { return new DateTime(Year, 1,1); } set { base.ReleaseDate = value; } }
        public string DisplayName { get { return this.DisplayTitle; } }

        #region IRatable implementation

        public int PerciseRating { get; set; }

        public int Rating  { get; set; }

        public ICollection<Tag> Tags  { get; set; }

        #endregion

        #region IEntity implementation

        public int Id  { get; set; }

        #endregion
}
}
