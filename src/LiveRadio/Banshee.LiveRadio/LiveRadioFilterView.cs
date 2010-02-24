
using System;
using System.Collections.Generic;

using Gtk;
using ScrolledWindow=Gtk.ScrolledWindow;

using Banshee.I18n;

using Hyena;
using Hyena.Data;
using Hyena.Data.Gui;
using Gdk;
using Cairo;
using Hyena.Gui;
using Hyena.Widgets;

namespace Banshee.LiveRadio
{
    public delegate void GenreSelectedEventHandler(object sender, string genre);

    public class LiveRadioFilterView : VBox
    {

        private ListView<string> genre_view;

        public event GenreSelectedEventHandler GenreSelected;
        public event GenreSelectedEventHandler GenreActivated;

        public LiveRadioFilterView ()
        {
            genre_view = new ListView<string> ();
            Column genre_column = new Column (new ColumnDescription(null,Catalog.GetString("Choose By Genre"),100));
            genre_view.ColumnController = new ColumnController();
            genre_view.ColumnController.Add(genre_column);
            genre_view.RowActivated += OnGenreActivated;
            List<string> stringlist = new List<string> ();
            stringlist.Add(Catalog.GetString("Loading..."));
            genre_view.SetModel(new GenreListModel (stringlist));
            //genre_view.HeaderVisible = false;
            genre_view.Model.Selection.FocusChanged += OnGenreSelected;

            Label query_label = new Label (Catalog.GetString("Choose By Query"));
            HBox query_box = new HBox ();
            query_box.BorderWidth = 1;
            Entry query_input = new Entry ();
            Button query_button = new Button (Stock.Find);
            query_box.PackStart(query_input,true,true,5);
            query_box.PackStart(query_button,false,true,5);

            this.PackStart(SetupView(genre_view),true,true,0);
            this.PackStart(query_label,false,true,0);
            this.PackStart(query_box,false,true,5);
        }

        public string GetSelectedGenre()
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            return model[genre_view.Model.Selection.FocusedIndex];
        }

        void OnGenreSelected (object sender, EventArgs e)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            RaiseGenreSelected(model[genre_view.Model.Selection.FocusedIndex]);
            Hyena.Log.DebugFormat("[LiveRadioFilterView]<OnSelectGenreNotify> selected entry: {0}", model[genre_view.Model.Selection.FocusedIndex]);
        }

        void OnGenreActivated (object o, RowActivatedArgs<string> args)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.SetSelection(args.Row);
            RaiseGenreActivated(model[genre_view.Model.Selection.FocusedIndex]);
            Hyena.Log.DebugFormat("[LiveRadioFilterView]<OnSelectGenreNotify> doubleclicked entry: {0}",model[model.Selection.FocusedIndex]);
        }

        public void UpdateGenres (List<string> newlist)
        {
            GenreListModel model = genre_view.Model as GenreListModel;
            model.SetList(newlist);
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
            if(handler != null) {
                handler(this, genre);
            }
        }

        public void RaiseGenreSelected (string genre)
        {
            OnGenreSelected (genre);
        }

        protected virtual void OnGenreActivated (string genre)
        {
            GenreSelectedEventHandler handler = GenreActivated;
            if(handler != null) {
                handler(this, genre);
            }
        }

        public void RaiseGenreActivated (string genre)
        {
            OnGenreActivated (genre);
        }
    }
}
