//
// GenerationalLifeBoard.cs
//
// Author:
//       Chris Howie <cdhowie@gmail.com>
//
// Copyright (c) 2010 Chris Howie
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
using System.Collections;

namespace Banshee.OpenVP.Visualizations.Conway
{
    public class GenerationalLifeBoard : ICollection<GenerationalLifeSquare>
    {
        private GenerationalLifeSquare[,] board;

        public GenerationalLifeSquare this[int x, int y]
        {
            get { return board[x, y]; }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool Wrap { get; set; }

        public GenerationalLifeBoard(int width, int height)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException("width < 1");

            if (height < 1)
                throw new ArgumentOutOfRangeException("height < 1");

            Width = width;
            Height = height;

            board = new GenerationalLifeSquare[width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    board[x, y] = new GenerationalLifeSquare(x, y);
        }

        public void NextGeneration()
        {
            foreach (var i in this)
                i.NextGeneration(GetNeighbors(i));
        }

        public void CommitGeneration()
        {
            foreach (var i in this)
                i.Commit();
        }

        private IEnumerable<GenerationalLifeSquare> GetNeighbors(GenerationalLifeSquare square)
        {
            int x = square.X;
            int y = square.Y;
            GenerationalLifeSquare n;

            if (GetSquare(x - 1, y - 1, out n)) yield return n;
            if (GetSquare(x    , y - 1, out n)) yield return n;
            if (GetSquare(x + 1, y - 1, out n)) yield return n;

            if (GetSquare(x - 1, y    , out n)) yield return n;
            if (GetSquare(x + 1, y    , out n)) yield return n;

            if (GetSquare(x - 1, y + 1, out n)) yield return n;
            if (GetSquare(x    , y + 1, out n)) yield return n;
            if (GetSquare(x + 1, y + 1, out n)) yield return n;
        }

        private bool GetSquare(int x, int y, out GenerationalLifeSquare square)
        {
            square = null;

            if (!Wrap) {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                    return false;
            } else {
                while (x < 0)
                    x += Width;

                while (x >= Width)
                    x -= Width;

                while (y < 0)
                    y += Height;

                while (y >= Height)
                    y -= Height;
            }

            square = board[x, y];
            return true;
        }

        #region ICollection<GenerationalLifeSquare> implementation

        void ICollection<GenerationalLifeSquare>.Add(GenerationalLifeSquare item)
        {
            throw new NotSupportedException();
        }

        void ICollection<GenerationalLifeSquare>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<GenerationalLifeSquare>.Contains(GenerationalLifeSquare item)
        {
            return item != null &&
                item.X < Width && item.Y < Height &&
                    item.X >= 0 && item.Y >= 0 &&
                    board[item.X, item.Y] == item;
        }

        public void CopyTo(GenerationalLifeSquare[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex == " + arrayIndex);

            if (array.Length - arrayIndex < Count)
                throw new ArgumentOutOfRangeException("Not enough room in array at arrayIndex");

            foreach (var i in this)
                array[arrayIndex++] = i;
        }

        bool ICollection<GenerationalLifeSquare>.Remove(GenerationalLifeSquare item)
        {
            throw new NotSupportedException();
        }

        public int Count {
            get { return Width * Height; }
        }

        bool ICollection<GenerationalLifeSquare>.IsReadOnly {
            get { return true; }
        }

        #endregion

        #region IEnumerable<GenerationalLifeSquare> implementation

        public IEnumerator<GenerationalLifeSquare> GetEnumerator()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    yield return board[x, y];
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
