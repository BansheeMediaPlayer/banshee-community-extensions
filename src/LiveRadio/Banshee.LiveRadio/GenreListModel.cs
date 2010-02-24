
using System;
using System.Collections.Generic;

using Hyena.Data;
using Hyena.Collections;
using Selection = Hyena.Collections.Selection;

namespace Banshee.LiveRadio
{


    public class GenreListModel : IListModel<string>
    {

        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        private string[] list;
        private List<string> original_list;

        private Selection selection;

        public GenreListModel (List<string> list) : base ()
        {
            original_list = list;
            this.list = original_list.ToArray ();
            selection = new Selection();
        }

        public void SetList(List<string> newlist)
        {
            original_list = newlist;
            Reload ();
        }

        public string this[int index]
        {
            get { return list[index]; }
        }

        public int Count
        {
            get { return list.Length; }
        }

        public bool CanReorder
        {
            get { return false; }
        }

        public void Clear()
        {
            list = new string[0];
            RaiseCleared ();
        }

        public void Reload()
        {
            list = original_list.ToArray ();
            RaiseReloaded ();
        }

        protected virtual void OnCleared ()
        {
            EventHandler handler = Cleared;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnReloaded ()
        {
            EventHandler handler = Reloaded;
            if(handler != null) {
                handler(this, EventArgs.Empty);
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
            selection.Select(index);
        }

        public void SetSelection (Selection selection)
        {
            this.selection = selection;
        }

        public Selection Selection
        {
            get { return selection; }
        }

    }
}
