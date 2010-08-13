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
using Mono.Addins;

using Hyena;
using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;
using Banshee.Gui;
using Banshee.Widgets;
using Banshee.ServiceStack;

namespace Banshee.LiveRadio
{
    /// <summary>
    /// EventHandler for genre selection
    /// </summary>
    public delegate void GenreSelectedEventHandler (object sender, Genre genre);
    /// <summary>
    /// EventHandler for query request
    /// </summary>
    public delegate void QuerySentEventHandler (object sender, string query);


    /// <summary>
    /// Gtk Container to hold the control elements for a LiveRadio source
    /// </summary>
    public class LiveRadioFilterView : VBox
    {

        private ListView<Genre> genre_view;
        private Entry query_input;
        private Button query_button;

        /// <summary>
        /// Event is raised when a genre is clicked in the genre choose box
        /// </summary>
        public event GenreSelectedEventHandler GenreSelected;
        /// <summary>
        /// Event is raised when a genre is double clicked in the genre choose box
        /// </summary>
        public event GenreSelectedEventHandler GenreActivated;
        /// <summary>
        /// Event is raised when a freetext query is sent by the user=
        /// </summary>
        public event QuerySentEventHandler QuerySent;

        /// <summary>
        /// Constructor -- creates a Gtk Widget holding a genre choose box and a freetext query field
        /// </summary>
        public LiveRadioFilterView ()
        {
            genre_view = new ListView<Genre> ();
            Column genre_column = new Column (new ColumnDescription ("Name", AddinManager.CurrentLocalizer.GetString ("Choose By Genre"), 100));
            genre_view.ColumnController = new ColumnController ();
            genre_view.ColumnController.Add (genre_column);
            List<Genre> stringlist = new List<Genre> ();
            stringlist.Add (new Genre(AddinManager.CurrentLocalizer.GetString ("Loading...")));
            genre_view.SetModel (new GenreListModel (stringlist));
            genre_view.Model.Selection.FocusChanged += OnViewGenreSelected;

            Label query_label = new Label (AddinManager.CurrentLocalizer.GetString ("Choose By Query"));
            HBox query_box = new HBox ();
            query_box.BorderWidth = 1;
            query_input = new Entry ();
            query_input.KeyReleaseEvent += OnInputKeyReleaseEvent;
            query_button = new Button (Stock.Find);
            query_button.Clicked += OnViewQuerySent;
            query_box.PackStart (query_input, true, true, 5);
            query_box.PackStart (query_button, false, true, 5);

            this.PackStart (SetupView (genre_view), true, true, 0);
            this.PackStart (query_label, false, true, 0);
            this.PackStart (query_box, false, true, 5);
        }

        /// <summary>
        /// Captures the press of the RETURN key within the query field and delegates to OnViewQuerySent
        /// </summary>
        /// <param name="o">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="args">
        /// A <see cref="KeyReleaseEventArgs"/> -- holding information about the key released
        /// </param>
        void OnInputKeyReleaseEvent (object o, KeyReleaseEventArgs args)
        {
            if (args.Event.Key == Gdk.Key.Return) {
                OnViewQuerySent (o, new EventArgs ());
                query_button.GrabFocus ();
            }
        }

        /// <summary>
        /// Handler when a query has been sent by the user. Clears any genre selection and raises QuerySent event
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/>
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/>
        /// </param>
        void OnViewQuerySent (object sender, EventArgs e)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.Selection.Clear (true);
            RaiseQuerySent (query_input.Text.Trim ());
        }

        /// <summary>
        /// Returns the genre entry which currently has focus in the genre choose box
        /// </summary>
        /// <returns>
        /// A <see cref="Genre"/> -- the focused genre
        /// </returns>
        public Genre GetSelectedGenre ()
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            return model[genre_view.Model.Selection.FocusedIndex];
        }

        /// <summary>
        /// Handler when a genre has been selected by the user. Raises the GenreSelected event for the selected genre
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        void OnViewGenreSelected (object sender, EventArgs e)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            RaiseGenreSelected (model[genre_view.Model.Selection.FocusedIndex]);
        }

        /// <summary>
        /// Set a new Genre list in the genre choose box
        /// </summary>
        /// <param name="newlist">
        /// A <see cref="List<Genre>"/> -- the list of new genres
        /// </param>
        public void UpdateGenres (List<Genre> newlist)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.SetList (newlist);
        }

        /// <summary>
        /// Capsules a Gtk Widget in a scolled window to add scrolling
        /// </summary>
        /// <param name="view">
        /// A <see cref="Widget"/> -- the widget to be capsuled
        /// </param>
        /// <returns>
        /// A <see cref="ScrolledWindow"/> -- the scrolled window containing the capsuled widget
        /// </returns>
        private ScrolledWindow SetupView (Widget view)
        {
            ScrolledWindow window = null;

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

        /// <summary>
        /// Method to invoke the GenreSelected event
        /// </summary>
        /// <param name="genre">
        /// A <see cref="Genre"/> -- the genre selected
        /// </param>
        protected virtual void OnGenreSelected (Genre genre)
        {
            GenreSelectedEventHandler handler = GenreSelected;
            if (handler != null) {
                handler (this, genre);
            }
        }

        /// <summary>
        /// Raise the GenreSelected event
        /// </summary>
        /// <param name="genre">
        /// A <see cref="Genre"/> -- the genre selected
        /// </param>
        public void RaiseGenreSelected (Genre genre)
        {
            OnGenreSelected (genre);
        }

        /// <summary>
        /// Method to invoke the GenreActivated event
        /// </summary>
        /// <param name="genre">
        /// A <see cref="Genre"/> -- the genre selected
        /// </param>
        protected virtual void OnGenreActivated (Genre genre)
        {
            GenreSelectedEventHandler handler = GenreActivated;
            if (handler != null) {
                handler (this, genre);
            }
        }

        /// <summary>
        /// Raise the GenreActivated event
        /// </summary>
        /// <param name="genre">
        /// A <see cref="Genre"/> -- the genre selected
        /// </param>
        public void RaiseGenreActivated (Genre genre)
        {
            OnGenreActivated (genre);
        }

        /// <summary>
        /// Method to invoke the QuerySent event
        /// </summary>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the query that has been sent
        /// </param>
        protected virtual void OnQuerySent (string query)
        {
            QuerySentEventHandler handler = QuerySent;
            if (handler != null) {
                handler (this, query);
            }
        }

        /// <summary>
        /// Raise the QuerySent event
        /// </summary>
        /// <param name="query">
        /// A <see cref="System.String"/> -- the query that has been sent
        /// </param>
        public void RaiseQuerySent (string query)
        {
            OnQuerySent (query);
        }
    }
}
