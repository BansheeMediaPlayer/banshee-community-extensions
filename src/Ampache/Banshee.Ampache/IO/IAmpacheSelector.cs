//
// IAmpacheSelector.cs
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

namespace Banshee.Ampache
{
    public interface IAmpacheSelector<TEntity> where TEntity : IEntity
    {
        /// <summary>
        /// A method to query for all items in Ampache
        /// </summary>
        /// <returns>
        /// A complete collection of all <see cref="TEntity"/> that are in the Ampache server
        /// </returns>
        IEnumerable<TEntity> SelectAll();

        /// <summary>
        /// Queries ampache for all <see cref="TEntity"/> that are associated with the provided <see cref="TParameter"/>,
        /// i.e. finding all albums associated with an artist.
        /// </summary>
        /// <param name="parameter">
        /// A <see cref="TParameter"/>, this can be in implementer of <see cref="IEntity"/>
        /// </param>
        /// <returns>
        /// A <see cref="ICollection<TEntity>"/> where all elements are associated the the provided <see cref="TParameter"/>
        /// if Ampache cannot be queried with the provided parameter then <see cref="SelectAll"/> will be used.
        /// </returns>
        IEnumerable<TEntity> SelectBy<TParameter>(TParameter parameter) where TParameter : IEntity;
    }


}