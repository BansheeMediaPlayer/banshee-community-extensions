//
// PlayQueue.cs
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
using System.Linq;

using Banshee.Collection;
using Banshee.Base;

namespace Banshee.Ampache
{
    internal class PlayQueue
    {
        private IList<AmpacheSong> _playList;
        private readonly IList<AmpacheSong> _original;
        private int _playing;

        private readonly AmpacheSong _first;

        public PlayQueue (IEnumerable<AmpacheSong> songs) : this (songs, Enumerable.Empty<AmpacheSong>())
        {}

        public PlayQueue (IEnumerable<AmpacheSong> nexts, IEnumerable<AmpacheSong> prevs)
        {
            var tmp = prevs.ToList();
            _playing = tmp.Count;
            tmp.AddRange(nexts);
            _playList = tmp;
            _first = _playList[SafePlaying];
            _original = new List<AmpacheSong>(tmp).AsReadOnly();
        }

        public AmpacheSong Current { get { return _playList[SafePlaying]; } }
        private int SafePlaying { get { return  _playing % _playList.Count; } }
        public AmpacheSong First { get { return _first; } }
        public AmpacheSong PeekNext()
        {
            if(_playList.Count == 0){
                return null;
            }
            return _playList[(_playing + 1) % _playList.Count];
        }
        public AmpacheSong Next()
        {
            _playing ++;
            return _playList[SafePlaying];
        }
        public AmpacheSong PeekPrevious()
        {
            if (_playing != 0) {
                return _playList[SafePlaying - 1];
            }
            return _playList.Last();
        }
        public AmpacheSong Previous()
        {
            if (_playing == 0) {
                _playing = _playList.Count;
            }
            _playing --;
            return _playList[SafePlaying];
        }
        public void Shuffle(object o)
        {
            var tmp = Current;
            var old = _playList;
            old.Remove(tmp);
            _playList = new List<AmpacheSong>();
            _playList.Add(tmp);
            Random rand = new Random();
            while (old.Count != 0) {
                tmp = old[rand.Next(old.Count)];
                old.Remove(tmp);
                _playList.Add(tmp);
            }
            _playing = 0;
        }
        public void Unshuffle(object o)
        {
            var tmp = Current;
            _playList = new List<AmpacheSong>(_original);
            _playing = _playList.IndexOf(tmp);
        }
    }
}
