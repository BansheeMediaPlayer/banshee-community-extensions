
using System;
using System.Collections.Generic;

using Hyena.Data;
using Hyena.Collections;
using Selection = Hyena.Collections.Selection;

namespace Banshee.LiveRadio.Plugins
{

    /// <summary>
    /// ListModel implementation for objects implementing ILiveRadioPlugin
    /// </summary>
    public class LiveRadioPluginListModel : IListModel<ILiveRadioPlugin>
    {

        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        private ILiveRadioPlugin[] list;
        private List<ILiveRadioPlugin> original_list;

        private Selection selection;

        public LiveRadioPluginListModel (List<ILiveRadioPlugin> list) : base()
        {
            original_list = list;
            this.list = original_list.ToArray ();
            selection = new Selection ();
        }

        public void SetList (List<ILiveRadioPlugin> newlist)
        {
            original_list = newlist;
            Reload ();
        }

        public ILiveRadioPlugin this[int index] {
            get { return list[index]; }
        }

        public int Count {
            get { return list.Length; }
        }

        public bool CanReorder {
            get { return false; }
        }

        public void Clear ()
        {
            list = new ILiveRadioPlugin[0];
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
