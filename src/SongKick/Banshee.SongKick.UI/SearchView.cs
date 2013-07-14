//
// SearchView.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using Hyena.Data.Gui;
using Banshee.SongKick.Recommendations;
using Hyena.Data;
using Hyena.Widgets;
using Banshee.Collection.Gui;
using Banshee.SongKick.Network;
using Hyena;

namespace Banshee.SongKick.UI
{
    public class SearchView : Gtk.HBox
    {
        private ListView<Result> list_view;
        private MemoryListModel<Result> model;

        MemoryListModel<Result> Model {
            get { return model; }
        }

        ScrolledWindow window = new ScrolledWindow ();

        public SearchView (MemoryListModel<Result> model)
        {
            list_view = new ResultListView ();
            var controller = new PersistentColumnController ("SongKick");
            list_view.ColumnController = controller;
            AddColumns ();
            controller.Load ();

            list_view.RowActivated += (o, a) => {
                return;
            };

            this.model = model;
            this.list_view.SetModel (model);

            model.Add (new Artist(-1, "Test Artist"));

            window.Child = list_view;

            this.PackStart (window, true, true, 0);
            ShowAll ();
        }

        private class ResultListView : ListView<Artist>
        {
            public ResultListView ()
            {
                RulesHint = true;
                IsEverReorderable = false;
            }

            protected override bool OnPopupMenu ()
            {
                //ServiceManager.Get<InterfaceActionService> ()["InternetArchive.IaResultPopup"].Activate ();
                return true;
            }
        }
        // TODO: do it using OOP
        private void AddColumns ()
        {
            var cols = new SortableColumn [] {
                Create ("DisplayName",      "Name"  , 0.9,  true, new ColumnCellText (null, true)),
                Create ("Id",               "Id"    , 0.15, true, new ColumnCellText (null, true)),
            };

            foreach (var col in cols) {
                list_view.ColumnController.Add (col);
            }


        }

        SortableColumn Create (string property, string label, double width, bool visible, ColumnCell cell)
        {
            cell.Property = property;
            return new SortableColumn (label, cell, width, property, visible);
        }
    }
}

