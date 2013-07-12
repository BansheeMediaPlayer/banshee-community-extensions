//
// ColumnHeaders.cs
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
using Mono.Unix;

namespace Banshee.SongKick.Search
{
    public class ColumnHeader
    {

        private static List<ColumnHeader> columnHeaders = new List<ColumnHeader> ();
        public static ColumnHeader NameHeader     = new ColumnHeader   ("display_name", "Name", "DisplayName");
        public static ColumnHeader IdHeader     = new ColumnHeader    ("id", "Id", "Id");

        public static IEnumerable<ColumnHeader> ColumnHeaders {
            get { return columnHeaders; }
        }

        public string Name { get; private set; }
        public string Id { get; private set; }
        public string Property { get; private set; }

        public ColumnHeader (string id, string property, string name)
        {
            Id = id;
            Name = name;
            Property = property;

            columnHeaders.Add (this);
            columnHeaders.Sort ((a, b) => a.Name.CompareTo (b.Name));
        }
    }
}

