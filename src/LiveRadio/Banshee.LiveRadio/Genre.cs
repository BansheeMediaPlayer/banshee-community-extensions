//
// Genre.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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

namespace Banshee.LiveRadio
{

    /// <summary>
    /// Implements a comparable Genre object to be used in List structures
    /// </summary>
    public class Genre : IComparable<Genre>
    {

        string name;
        string link;

        /// <summary>
        /// Constructor
        /// </summary>
        public Genre ()
        {
            name = null;
        }

        /// <summary>
        /// Full Constructor
        /// </summary>
        /// <param name="genre">
        /// A <see cref="System.String"/> -- the genre name
        /// </param>
        public Genre (string genre)
        {
            name = genre;
        }

        public string Hyperlink
        {
            get { return link; }
            set { link = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// The dictionary key for this genre
        /// </summary>
        public string GenreKey
        {
            get { return "Genre:" + name; }
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Equals Handler comparing Genres by their name
        /// </summary>
        /// <param name="genre">
        /// A <see cref="System.Object"/> -- a Genre object
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/> -- true if name of the compared genre equals the Genre name of the caller
        /// </returns>
        public override bool Equals (object genre)
        {
            if (!(genre is Genre)) return false;
            return this.Name.Equals ((genre as Genre).Name);
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }

        /// <summary>
        /// Implements string comparison of genre names
        /// </summary>
        /// <param name="genre">
        /// A <see cref="Genre"/> -- a Genre object
        /// </param>
        /// <returns>
        /// A <see cref="System.Int32"/> -- the comparison result of the genre names
        /// </returns>
        public int CompareTo (Genre genre)
        {
            return this.Name.CompareTo (genre.Name);
        }


    }
}
