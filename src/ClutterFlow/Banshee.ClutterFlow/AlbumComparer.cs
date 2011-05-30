// 
// AlbumComparer.cs
// 
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
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

namespace Banshee.ClutterFlow
{
    public enum SortOptions { Artist = 0 , Album = 1 }

    public interface ISortComparer<T> : IComparer<T>
    {
        string GetSortLabel (T obj);
    }

    public abstract class AlbumComparer : ISortComparer<AlbumInfo>
    {
        protected virtual string GetTitle (AlbumInfo obj)
        {
            return (obj.TitleSort ?? obj.Title ?? obj.DisplayTitle ?? "");
        }

        protected virtual string GetArtist (AlbumInfo obj)
        {
            return (obj.ArtistNameSort ?? obj.ArtistName ?? obj.DisplayArtistName ?? "");
        }

        public abstract int Compare (AlbumInfo x, AlbumInfo y);
        public abstract string GetSortLabel (AlbumInfo obj);
    }

    public class AlbumAlbumComparer : AlbumComparer
    {

        public override string GetSortLabel (AlbumInfo obj)
        {
            if (obj!=null) {
                return obj.TitleSort ?? obj.Title ?? "?";
            } else {
                return "?";
            }
        }

        public override int Compare (AlbumInfo x, AlbumInfo y)
        {
            string tx = GetTitle(x) + GetArtist(x);
            string ty = GetTitle(y) + GetArtist(y);
            return tx.CompareTo(ty);
        }
    }

    public class AlbumArtistComparer : AlbumComparer
    {
        public override string GetSortLabel (AlbumInfo obj)
        {
            if (obj!=null) {
                return obj.ArtistNameSort ?? obj.ArtistName ?? "?";
            } else {
                return "?";
            }
        }

        public override int Compare (AlbumInfo x, AlbumInfo y)
        {
            string tx = GetArtist(x) + GetTitle(x);
            string ty = GetArtist(y) + GetTitle(y);
            return tx.CompareTo(ty);
        }
    }
}