//
// TransferList.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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
using System.Collections.Generic;

namespace Banshee.Telepathy.Data
{  
    public class TransferList<K, V> : Dictionary<K, V> where V : Transfer<K> where K : IEquatable<K>
    {
        public IEnumerable<V> Queued ()
        {
            lock (this) {
                foreach (V t in this.Values) {
                    if (t.State == TransferState.Queued) {
                        yield return t;
                    }
                }
            }
        }
        
        public IEnumerable<V> Ready ()
        {
            lock (this) {
                foreach (V t in this.Values) {
                    if (t.State == TransferState.Ready) {
                        yield return t;
                    }
                }
            }
        }
        
        public void Add (K key, V value)
        {
            lock (this) {
                base.Add (key, value);
            }
        }
        
        public bool Remove (K key)
        {
            lock (this) {
                return base.Remove (key);
            }
        }


    }
}