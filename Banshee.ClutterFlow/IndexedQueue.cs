#region License

/* Copyright (c) 2006 Leslie Sanford (jabberdabber@hotmail.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
	
	public class IndexedQueue<T> where T : class, IIndexable
	{
		#region Fields
		private SortedDictionary<int, List<T>> queue = new SortedDictionary<int, List<T>> ();

		public event EventHandler Changed;
		
		public T this [int index] {
			get { return queue[index][0]; }
		}

		public int Count {
			get { return queue.Count; }
		}

		private int largest_key = 0;
		public int LargestKey {
			get {
				return largest_key; 
			}
		}
		#endregion
		
		public IndexedQueue () : base () 
		{
		}

		#region Methods
		public T TryKey (int key) 
		{
			List<T> list;
			queue.TryGetValue (key, out list);
			return (list==null || list.Count==0) ? null : list[0];
		}
		
		public void Add (T value)
		{
			Add (value.Index, value, true);
		}
		
		private void Add (int key, T value, bool do_delegates)
		{
			if (!queue.ContainsKey (key))
				queue.Add (key, new List<T> ());
			else if (queue[key].Contains (value))
				throw new InvalidOperationException ("An IndexedQueue requires unique values, cannot insert a value twice!");
			queue[key].Add (value);
			if (do_delegates) { 
				value.IndexChanged += HandleIndexChanged;
				OnChanged ();
			}
		}

		/// <summary>
		/// This method atempts to remove & return the first element of a given key.
		/// This does not invoke the Changed event, unlike Add and Remove.
		/// </summary>
		/// <param name="key">
		/// The index (a <see cref="System.Int32"/>)
		/// </param>
		/// <returns>
		/// The first element for this key.
		/// </returns>
		/// <exception cref="InvalidOperationException">key refers to an empty or null list</exception>
		public T PopFrom (int key)
		{
			if (!queue.ContainsKey (key) || queue[key].Count==0) {
				
				if (queue.ContainsKey (key))
					Hyena.Log.Information ("PopFrom was called but key held no values");
				else
					Hyena.Log.Information ("PopFrom was called but did not contain key " + key);
				throw new InvalidOperationException ("Value not found in IndexedQueue");
			}
			T value = queue[key][0];
			value.IndexChanged -= HandleIndexChanged;
			queue[key].RemoveAt (0);
			if (queue[key].Count==0)
				queue.Remove(key);
			return value;
		}
		
		public void Remove (T value)
		{
			Remove (value.Index, value, true);
		}
		private void Remove (int key, T value, bool do_delegates)
		{
			if (!queue.ContainsKey (key) || !queue[key].Contains (value))
				throw new InvalidOperationException ("Value not found in IndexedQueue");
			queue[key].Remove (value);
			if (do_delegates) { 
				value.IndexChanged -= HandleIndexChanged;
				OnChanged ();
			}
			if (queue[key].Count==0)
				queue.Remove(key);
		}

		protected void HandleIndexChanged(IIndexable item, int old_index, int new_index)
		{
			if (item is T) {
				Remove (old_index, (T) item, false);
				Add (new_index, (T) item, false);
				OnChanged ();
			}
		}
		
		protected virtual void OnChanged () 
			{
			if (Changed!=null) Changed (this, EventArgs.Empty);
		}
		#endregion
	}
}
