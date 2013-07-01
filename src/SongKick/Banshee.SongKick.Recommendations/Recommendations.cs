//
// Recommendations.cs
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

namespace Banshee.SongKick.Recommendations
{
    public abstract class Recommendations : IList<MusicEvent>
    {
        protected IList<MusicEvent> musicEvents = new List<MusicEvent>();

        #region IList implementation

        public int IndexOf (MusicEvent item)
        {
            return musicEvents.IndexOf(item);
        }

        public void Insert (int index, MusicEvent item)
        {
            musicEvents.Insert(index, item);
        }

        public void RemoveAt (int index)
        {
            musicEvents.RemoveAt(index);
        }

        public MusicEvent this [int index] {
            get {
                return musicEvents[index];
            }
            set {
                musicEvents[index] = value;
            }
        }

        #endregion

        #region ICollection implementation

        public void Add (MusicEvent item)
        {
            musicEvents.Add(item);
        }

        public void Clear ()
        {
            musicEvents.Clear();
        }

        public bool Contains (MusicEvent item)
        {
            return musicEvents.Contains(item);
        }

        public void CopyTo (MusicEvent[] array, int arrayIndex)
        {
            musicEvents.CopyTo(array, arrayIndex);
        }

        public bool Remove (MusicEvent item)
        {
            return musicEvents.Remove(item);
        }

        public int Count {
            get {
                return musicEvents.Count;
            }
        }

        public bool IsReadOnly {
            get {
                return musicEvents.IsReadOnly;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<MusicEvent> GetEnumerator ()
        {
            return musicEvents.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            var musicEventNonGeneric = musicEvents as System.Collections.IEnumerable;
            return musicEventNonGeneric.GetEnumerator();
        }

        #endregion
    }
}

