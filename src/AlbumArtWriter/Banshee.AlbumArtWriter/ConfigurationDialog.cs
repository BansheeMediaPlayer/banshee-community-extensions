//
// AlbumArtWriterService.cs
//
// Authors:
//   Kevin Anthony <Kevin.S.Anthony@gmail.com>
//
// Copyright (C) 2011 Kevin Anthony
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
using Gtk;
using Mono.Addins;

using Banshee.Gui.Dialogs;

namespace Banshee.AlbumArtWriter
{
    public class ConfigurationDialog : BansheeDialog
    {
        private AlbumArtWriterService service;
        private Entry artname_entry;
        private RadioButton jpg;
        private RadioButton png;

        public ConfigurationDialog (AlbumArtWriterService service) : base (AddinManager.CurrentLocalizer.GetString ("Album Art Writer Configuration"))
        {
            this.service = service;

            Frame artframe = new Frame("artbox_frame");
            artframe.Label = AddinManager.CurrentLocalizer.GetString("Output File Name (No Extension)");
            HBox artname_box = new HBox ();
            artname_box.PackStart (new Label (AddinManager.CurrentLocalizer.GetString ("File Name:")), false, false, 0);
            artname_entry = new Entry ();
            artname_box.PackStart (artname_entry, true, true, 3);

            artframe.Add(artname_box);
            artframe.ShadowType = (ShadowType) 4;

            VBox.PackStart( artframe,false,false,3);

            Frame fileframe = new Frame("artbox_frame");
            fileframe.Label = AddinManager.CurrentLocalizer.GetString("Output File type");
            HBox image_radio_button_h_box = new HBox ();
            jpg = new RadioButton  (null, "JPG File");
            png = new RadioButton  (jpg, "PNG File");
            image_radio_button_h_box.PackStart(jpg, false, false, 3);
            image_radio_button_h_box.PackStart(png, false, false, 3);
            fileframe.Add(image_radio_button_h_box);
            fileframe.ShadowType = (ShadowType) 4;
            VBox.PackStart (fileframe, false, false, 3);
            AddDefaultCloseButton ();
            ShowAll();

            // initialize values
            artname_entry.Text = service.ArtName;
            if (service.JPG){
                jpg.Activate();
            } else if (service.PNG) {
                png.Activate();
            }

            // attach change handlers
            artname_entry.Changed += new EventHandler (on_ArtName_Changed);
            jpg.Toggled += new EventHandler(on_Radio_Clicked);
            png.Toggled += new EventHandler(on_Radio_Clicked);
        }

        private void on_ArtName_Changed (object source, System.EventArgs args)
        {
            service.ArtName = artname_entry.Text;
        }
        private void on_Radio_Clicked (object source, System.EventArgs args)
        {
            service.JPG = jpg.Active;
            service.PNG = png.Active;
        }
    }
}
