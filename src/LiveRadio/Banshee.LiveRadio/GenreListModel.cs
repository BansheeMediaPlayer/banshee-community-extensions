//
// GenreListModel.cs
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
using System.Collections.Generic;

using Hyena.Data;
using Hyena.Collections;
using Selection = Hyena.Collections.Selection;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// Models a list of Genre objects and implements the IListModel interface so the model can be used in a ListView
    /// </summary>
    public class GenreListModel : IListModel<Genre>
    {

        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        private Genre[] list;
        private List<Genre> original_list;

        private Selection selection;

        public GenreListModel (List<Genre> list) : base()
        {
            original_list = list;
            this.list = original_list.ToArray ();
            selection = new Selection ();
        }

        public void SetList (List<Genre> newlist)
        {
            original_list = newlist;
            Reload ();
        }

        public Genre this[int index] {
            get { return list[index]; }
        }

        public object GetItem (int index)
        {
            return this[index];
        }

        public int Count {
            get { return list.Length; }
        }

        public bool CanReorder {
            get { return false; }
        }

        public void Clear ()
        {
            list = new Genre[0];
            RaiseCleared ();
        }

        public void Reload ()
        {
            list = original_list.ToArray ();
            RaiseReloaded ();
        }

        protected virtual void OnCleared ()
        {
            EventHandler handler = Cleared;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected virtual void OnReloaded ()
        {
            EventHandler handler = Reloaded;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void RaiseReloaded ()
        {
            OnReloaded ();
        }

        private void RaiseCleared ()
        {
            OnCleared ();
        }

        public void SetSelection (int index)
        {
            selection.Select (index);
        }

        public void SetSelection (Selection selection)
        {
            this.selection = selection;
        }

        public Selection Selection {
            get { return selection; }
        }

    }
}
