//
// FetcherDialog.cs
//
// Author:
//   Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
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
using System.Net;

using Gtk;
using Mono.Addins;

using Banshee.Kernel;
using Banshee.Collection.Database;
using Banshee.Sources;

using Hyena;

namespace Banshee.RadioStationFetcher
{
    public abstract class FetcherDialog : Gtk.Dialog
    {
        public string source_name;
        public List<string> genre_list = new List<string> ();
        public static int genre_search = 0;
        public static int freeText_search = 1;

        private Button close_button;
        private ComboBox genre_entry;
        private Entry freeText_entry;
        private Button genre_button;
        private Button freeText_button;
        private Table table;
        protected Statusbar statusbar;

        public FetcherDialog ()
        {
        }

        public virtual void ShowDialog ()
        {
            SetStatusBarMessage (source_name);
            ShowAll ();
        }

        public void HideDialog ()
        {
            HideAll ();
        }

        public void InitializeDialog ()
        {
            FillGenreList ();

            AccelGroup accel_group = new AccelGroup ();
            AddAccelGroup (accel_group);

            Title = String.Empty;
            SkipTaskbarHint = true;

            BorderWidth = 6;
            HasSeparator = false;
            DefaultResponse = ResponseType.Ok;

            VBox.Spacing = 6;

            HBox split_box = new HBox ();
            split_box.Spacing = 12;
            split_box.BorderWidth = 6;

            Image image = new Image ();
            image.IconSize = (int)IconSize.Dialog;
            image.IconName = "radio";
            image.Yalign = 0.0f;
            image.Show ();

            VBox main_box = new VBox ();
            main_box.BorderWidth = 5;
            main_box.Spacing = 10;

            Label header = new Label ();
            header.Text = String.Format (AddinManager.CurrentLocalizer.GetString ("{0}Radiostation fetcher{1}\n({2})"),
                "<span weight=\"bold\" size=\"larger\">", "</span>",source_name);
            header.Xalign = 0.0f;
            header.Yalign = 0.0f;
            header.UseMarkup = true;
            header.Wrap = true;
            header.Show ();

            Label message = new Label ();
            message.Text = AddinManager.CurrentLocalizer.GetString ("Choose a genre or enter a text that you wish to be queried, " +
                "then press the Get stations button. Found stations will be added to internet-radio source.");
            message.Xalign = 0.0f;
            message.Wrap = true;
            message.Show ();

            table = new Table (5, 2, false);
            table.RowSpacing = 6;
            table.ColumnSpacing = 6;

            genre_entry = ComboBox.NewText ();
            freeText_entry = new Entry ();

            genre_button = new Button (AddinManager.CurrentLocalizer.GetString ("Get stations"));
            freeText_button = new Button (AddinManager.CurrentLocalizer.GetString ("Get stations"));

            genre_button.CanDefault = true;
            genre_button.UseStock = true;
            genre_button.Clicked += OnGenreQueryButtonClick;
            genre_button.Show ();


            freeText_button.CanDefault = true;
            freeText_button.UseStock = true;
            freeText_button.Clicked += OnFreetextQueryButtonClick;
            freeText_button.Show ();

            foreach (string genre in genre_list) {
                if (!String.IsNullOrEmpty (genre))
                    genre_entry.AppendText (genre);
            }

            if (this is IGenreSearchable) {
                AddRow (AddinManager.CurrentLocalizer.GetString ("Query by genre:"), genre_entry, genre_button);
            }

            if (this is IFreetextSearchable) {
                AddRow (AddinManager.CurrentLocalizer.GetString ("Query by free text:"), freeText_entry, freeText_button);
            }

            table.ShowAll ();

            main_box.PackStart (header, false, false, 0);
            main_box.PackStart (message, false, false, 0);
            main_box.PackStart (table, false, false, 0);
            main_box.Show ();

            split_box.PackStart (image, false, false, 0);
            split_box.PackStart (main_box, true, true, 0);
            split_box.Show ();

            VBox.PackStart (split_box, true, true, 0);

            close_button = new Button ();
            close_button.Image = new Image ("gtk-close", IconSize.Button);
            close_button.Label = AddinManager.CurrentLocalizer.GetString ("_Close");
            close_button.CanDefault = true;
            close_button.UseStock = true;
            close_button.Show ();
            AddActionWidget (close_button, ResponseType.Close);

            close_button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Escape,
                0, Gtk.AccelFlags.Visible);

            close_button.Clicked += OnCloseButtonClick;

            statusbar = new Statusbar ();
            statusbar.HasResizeGrip = false;
            SetStatusBarMessage (source_name);
            main_box.PackEnd (statusbar, false, false, 0);
        }

        public void OnCloseButtonClick (object o, EventArgs args)
        {
            HideDialog ();
        }

        private void OnGenreQueryButtonClick (object o, EventArgs args)
        {
            Banshee.Kernel.Scheduler.Schedule (new DelegateJob (DoGenreQuery));
        }

        private void OnFreetextQueryButtonClick (object o, EventArgs args)
        {
            Banshee.Kernel.Scheduler.Schedule (new DelegateJob (DoFreetextQuery));
        }

        private void DoGenreQuery ()
        {
            SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("Querying genre \"{0}\""), Genre));

            try {
                List<DatabaseTrackInfo> fetched_stations = (this as IGenreSearchable).FetchStationsByGenre (Genre);
                SaveFetchedStationsToDatabase (fetched_stations);
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("Query done. Fetched {0} stations."),
                    fetched_stations.Count.ToString ()));
            }
            catch (InternetRadioExtensionNotFoundException) {
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("ERROR: Internet-radio extension not available.")));
            }
            catch (WebException) {
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("ERROR: Network error.")));
            }
            catch (Exception) {
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("ERROR")));
            }
        }

        private void DoFreetextQuery ()
        {
            SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("Querying freetext \"{0}\""), Freetext));

            try {
                List<DatabaseTrackInfo> fetched_stations = (this as IFreetextSearchable).FetchStationsByFreetext (Freetext);
                SaveFetchedStationsToDatabase (fetched_stations);
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("Query done. Fetched {0} stations."),
                    fetched_stations.Count.ToString ()));
            }
            catch (InternetRadioExtensionNotFoundException) {
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("ERROR: Internet-radio extension not available.")));
            }
            catch (WebException) {
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("ERROR: Network error.")));
            }
            catch (Exception) {
                SetStatusBarMessage (String.Format (AddinManager.CurrentLocalizer.GetString ("ERROR")));
            }
        }

        private void SaveFetchedStationsToDatabase (List<DatabaseTrackInfo> fetched_stations)
        {
            if (fetched_stations == null) {
                throw new Exception ();
            }

            foreach (DatabaseTrackInfo track in fetched_stations) {
                track.Save ();
            }
        }

        public abstract void FillGenreList ();

        private uint row = 0;
        private void AddRow (string title, Widget entry, Widget button)
        {
            Label label = new Label (title);
            label.Xalign = 0.0f;
            table.Attach (label, 0, 1, row, row + 1, AttachOptions.Fill, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            table.Attach (entry, 1, 2, row, row + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            table.Attach (button, 2, 3, row, row + 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
            row++;
        }

        public string Genre {
            get { return genre_entry.ActiveText.Trim (); }
        }

        public string Freetext {
            get { return freeText_entry.Text.Trim (); }
        }

        protected PrimarySource GetInternetRadioSource ()
        {
            Log.Debug ("[FetcherDialog] <GetInternetRadioSource> Start");

            foreach (Source source in Banshee.ServiceStack.ServiceManager.SourceManager.Sources) {
                Log.DebugFormat ("[FetcherDialog] <GetInternetRadioSource> Source: {0}", source.GenericName);

                if (source.UniqueId.Equals ("InternetRadioSource-internet-radio")) {
                    return (PrimarySource) source;
                }
            }

            Log.Debug ("[FetcherDialog] <GetInternetRadioSource> Not found throwing exception");
            throw new InternetRadioExtensionNotFoundException ();
        }

        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
            HideAll ();
            return true;
        }

        protected override void OnClose()
        {
            HideAll ();
        }

        protected void SetStatusBarMessage (string message)
        {
            ThreadAssist.ProxyToMain (delegate
                {
                     statusbar.Push (0, message);
                });
        }
    }

    public class ParseException : Exception {
    }

    public class InternetRadioExtensionNotFoundException : Exception {
    }
}
