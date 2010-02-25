//
// LiveRadioFilterView.cs
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

using Gtk;
using ScrolledWindow = Gtk.ScrolledWindow;

using Banshee.I18n;

using Hyena;
using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

namespace Banshee.LiveRadio
{
    public delegate void GenreSelectedEventHandler (object sender, string genre);
    public delegate void QuerySentEventHandler (object sender, string query);

    public class LiveRadioFilterView : VBox
    {

        private ListView<string> genre_view;
        private Entry query_input;

        public event GenreSelectedEventHandler GenreSelected;
        public event GenreSelectedEventHandler GenreActivated;
        public event QuerySentEventHandler QuerySent;

        public LiveRadioFilterView ()
        {
            genre_view = new ListView<string> ();
            Column genre_column = new Column (new ColumnDescription (null, Catalog.GetString ("Choose By Genre"), 100));
            genre_view.ColumnController = new ColumnController ();
            genre_view.ColumnController.Add (genre_column);
            genre_view.RowActivated += OnViewGenreActivated;
            List<string> stringlist = new List<string> ();
            stringlist.Add (Catalog.GetString ("Loading..."));
            genre_view.SetModel (new GenreListModel (stringlist));
            //genre_view.HeaderVisible = false;
            genre_view.Model.Selection.FocusChanged += OnViewGenreSelected;
            
            Label query_label = new Label (Catalog.GetString ("Choose By Query"));
            HBox query_box = new HBox ();
            query_box.BorderWidth = 1;
            query_input = new Entry ();
            query_input.KeyReleaseEvent += OnInputKeyReleaseEvent;
            Button query_button = new Button (Stock.Find);
            query_button.Clicked += OnViewQuerySent;
            query_box.PackStart (query_input, true, true, 5);
            query_box.PackStart (query_button, false, true, 5);
            
            this.PackStart (SetupView (genre_view), true, true, 0);
            this.PackStart (query_label, false, true, 0);
            this.PackStart (query_box, false, true, 5);
            
            this.Sensitive = false;
        }

        void OnInputKeyReleaseEvent (object o, KeyReleaseEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Return) {
                OnViewQuerySent (o, new EventArgs ());
            }
        }

        void OnViewQuerySent (object sender, EventArgs e)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.Selection.Clear (false);
            RaiseQuerySent (query_input.Text.Trim ());
        }

        public string GetSelectedGenre ()
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            return model[genre_view.Model.Selection.FocusedIndex];
        }

        void OnViewGenreSelected (object sender, EventArgs e)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            RaiseGenreSelected (model[genre_view.Model.Selection.FocusedIndex]);
            Hyena.Log.DebugFormat ("[LiveRadioFilterView]<OnSelectGenreNotify> selected entry: {0}",
                                   model[genre_view.Model.Selection.FocusedIndex]);
        }

        void OnViewGenreActivated (object o, RowActivatedArgs<string> args)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.SetSelection (args.Row);
            RaiseGenreActivated (model[genre_view.Model.Selection.FocusedIndex]);
            Hyena.Log.DebugFormat ("[LiveRadioFilterView]<OnSelectGenreNotify> doubleclicked entry: {0}",
                                   model[model.Selection.FocusedIndex]);
        }

        public void UpdateGenres (List<string> newlist)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.SetList (newlist);
            this.Sensitive = true;
        }

        private ScrolledWindow SetupView (Widget view)
        {
            ScrolledWindow window = null;
            
            //if (!Banshee.Base.ApplicationContext.CommandLine.Contains ("no-smooth-scroll")) {
            if (ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
                window = new SmoothScrolledWindow ();
            } else {
                window = new ScrolledWindow ();
            }
            
            window.Add (view);
            window.HscrollbarPolicy = PolicyType.Automatic;
            window.VscrollbarPolicy = PolicyType.Automatic;
            
            return window;
        }

        protected virtual void OnGenreSelected (string genre)
        {
            GenreSelectedEventHandler handler = GenreSelected;
            if (handler != null) {
                handler (this, genre);
            }
        }

        public void RaiseGenreSelected (string genre)
        {
            OnGenreSelected (genre);
        }

        protected virtual void OnGenreActivated (string genre)
        {
            GenreSelectedEventHandler handler = GenreActivated;
            if (handler != null) {
                handler (this, genre);
            }
        }

        public void RaiseGenreActivated (string genre)
        {
            OnGenreActivated (genre);
        }

        protected virtual void OnQuerySent (string query)
        {
            QuerySentEventHandler handler = QuerySent;
            if (handler != null) {
                handler (this, query);
            }
        }

        public void RaiseQuerySent (string query)
        {
            OnQuerySent (query);
        }
    }
}
