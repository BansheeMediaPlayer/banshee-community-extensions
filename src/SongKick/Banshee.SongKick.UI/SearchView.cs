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

using System.Linq;

namespace Banshee.SongKick.UI
{
    public class SearchView<T> : Gtk.HBox where T : IResult
    {
        protected ListView<T> list_view;
        protected MemoryListModel<T> model;

        MemoryListModel<T> Model {
            get { return model; }
        }

        ScrolledWindow window = new ScrolledWindow ();

        public SearchView (MemoryListModel<T> model)
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


            window.Child = list_view;

            this.PackStart (window, true, true, 0);
            ShowAll ();
        }


        public void OnUpdated ()
        {
            // TODO: implement
        }

        private class ResultListView : ListView<T>
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
        protected virtual void AddColumns ()
        {
            Type genericTypeT = typeof(T);
            System.Collections.Generic.List<System.Reflection.PropertyInfo> propertyInfosWithDisplayableAttr = genericTypeT.GetProperties().Where(
                p => Attribute.IsDefined(p, typeof(DisplayAttribute))).ToList();

            var displayAttributes = propertyInfosWithDisplayableAttr
                .Select (propertyInfo => propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), true)[0] as DisplayAttribute)
                .ToList<DisplayAttribute>();

            var propertyInfoWithDisplayAttr = propertyInfosWithDisplayableAttr.Zip (
                displayAttributes, 
                (propInfo, attr) => new Tuple<System.Reflection.PropertyInfo, DisplayAttribute>(propInfo, attr));

            var cols = propertyInfoWithDisplayAttr
                .Select (pair => CreateColumnHeader(pair.Item1.Name, pair.Item2.Name, 0.15, true, new ColumnCellText (null, true)));

            foreach (var col in cols) {
                list_view.ColumnController.Add (col);
            }
        }

        SortableColumn CreateColumnHeader (string property, string label, double width, bool visible, ColumnCell cell)
        {
            cell.Property = property;
            return new SortableColumn (label, cell, width, property, visible);
        }
    }
}

