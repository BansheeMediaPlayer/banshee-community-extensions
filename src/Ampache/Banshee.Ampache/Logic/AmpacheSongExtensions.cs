// 
// AmpacheAlbumExtensions.cs
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

namespace Banshee.Ampache
{
	internal static class AmpacheSongExtensions
	{
        /// <summary>
        /// The Ampache Song XML only provides an album and artist id, this operation
        /// hydrates the <see cref="AmpacheSong"/> with relevent album and artist information
        /// </summary>
        /// <param name="song">
        /// A <see cref="AmpacheSong"/>
        /// </param>
        /// <param name="album">
        /// A <see cref="AmpacheAlbum"/>
        /// </param>
		public static void Hydrate(this AmpacheSong song, AmpacheAlbum album)
		{
            // TODO: accept an Ampache Artist as a parameter to account for albums with "various" artists
			song.ArtistName = album.ArtistName;
			song.ArtistNameSort = album.ArtistNameSort;
			song.AlbumTitle = album.Title;
			song.AlbumTitleSort = album.TitleSort;
		}
	}
}
