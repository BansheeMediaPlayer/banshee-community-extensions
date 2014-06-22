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
using System.Collections.Generic;

namespace Banshee.SongKick.UI
{
    public class SearchView<T> : Gtk.HBox where T : IResult
    {
        protected ListView<T> list_view;
        protected SortableMemoryListModel<T> model;

        public SortableMemoryListModel<T> Model {
            get { return model; }
        }
        public event RowActivatedHandler<T> RowActivated {
            add { list_view.RowActivated += value; }
            remove {list_view.RowActivated -= value; }
        }

        ScrolledWindow window = new ScrolledWindow ();

        public SearchView (SortableMemoryListModel<T> model)
        {
            list_view = new ResultListView ();
            var controller = new PersistentColumnController ("SongKick");
            list_view.ColumnController = controller;
            AddColumns ();
            SetDefultSortColumn ();
            controller.Load ();

            this.model = model;
            this.list_view.SetModel (model);

            window.Child = list_view;
            this.PackStart (window, true, true, 0);
            ShowAll ();
        }

        public void SetModel (SortableMemoryListModel<T> model)
        {
            this.model = model;
            list_view.SetModel (model);
        }

        public void OnUpdated ()
        {
            model.Reload ();
        }

        private class ResultListView : ListView<T> 
        {
            public ResultListView ()
            {
                RulesHint = true;
                IsEverReorderable = true;
                IsReorderable = true;
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
            var propertyInfoWithDisplayAttr = GetPropertyInfoWithDisplayAttr<DisplayAttribute> ();

            var cols = propertyInfoWithDisplayAttr
                .Select (infoWithAttr => CreateColumnHeader(infoWithAttr.Info, infoWithAttr.Attribute));

            foreach (var col in cols) {
                list_view.ColumnController.Add (col);
            }
        }

        private void SetDefultSortColumn ()
        {
            var propertyInfoWithDisplayAttr = GetPropertyInfoWithDisplayAttr<DefaultSortColumn> ();

            if (propertyInfoWithDisplayAttr.Count() == 1) {
                var infoWithAttr = propertyInfoWithDisplayAttr.First();
                var column = list_view
                    .ColumnController
                    .Where (col => (col as SortableColumn).SortKey == infoWithAttr.Info.Name)
                    .First () as SortableColumn;

                // for now, every column should be sorted in Descending order
                // TODO: get SortType from DefaultSortColumn attribute
                column.SortType = SortType.Descending;

                list_view.ColumnController.DefaultSortColumn = column;
            } else if (propertyInfoWithDisplayAttr.Count() > 1){
                Hyena.Log.Error ("Class must specify at most one DefaultSortColumn");
            }

        }

        private IEnumerable<InfoWithAttr<T2>> GetPropertyInfoWithDisplayAttr<T2> () where T2 : class
        { 
            Type genericTypeT = typeof(T);
            Type attributeType = typeof(T2);
            System.Collections.Generic.List<System.Reflection.PropertyInfo> propertyInfosWithDisplayableAttr = 
                genericTypeT
                .GetProperties ()
                .Where (p => Attribute.IsDefined (p, typeof(T2)))
                .ToList ();
            var displayAttributes = 
                propertyInfosWithDisplayableAttr
                .Select (propertyInfo => propertyInfo.GetCustomAttributes (attributeType, true) [0] as T2)
                .ToList<T2> ();
            var propertyInfoWithDisplayAttr = 
                propertyInfosWithDisplayableAttr
                .Zip (displayAttributes, 
                    (propInfo, attr) => new InfoWithAttr<T2>() {
                        Info = propInfo,
                        Attribute = attr
                    });
            return propertyInfoWithDisplayAttr;
        }

        private class InfoWithAttr<T2> {
            public System.Reflection.PropertyInfo Info { get; set; }
            public T2 Attribute  { get; set; }
        }

        SortableColumn CreateColumnHeader(System.Reflection.PropertyInfo propertyInfo, DisplayAttribute attr)
        {
            return CreateColumnHeader (propertyInfo.Name, attr.Name, 0.15, true, new ColumnCellText (null, true));
        }

        SortableColumn CreateColumnHeader (string property, string label, double width, bool visible, ColumnCell cell)
        {
            cell.Property = property;
            return new SortableColumn (label, cell, width, property, visible);
        }
      
    }


    // This class should inherit from MemoryListModel<T>, but MemoryListModel<T> has private field of type List<T>,
    // which would make "Sort" method in this class inefficient.
    public class SortableMemoryListModel<T> : BaseListModel<T>, ISortable
    {
        private ISortableColumn column;

        private IList<T> elements = new List<T>();
        
        public SortableMemoryListModel() : base()
        {
            Selection = new Hyena.Collections.Selection ();
            CanReorder = true;
        }

        #region implemented abstract members of BaseListModel

        public override void Clear ()
        {
            lock (elements) {
                elements.Clear ();
            }

            OnCleared ();
        }

        public override void Reload ()
        {
            OnReloaded ();
        }

        public int IndexOf (T item)
        {
            lock (elements) {
                return elements.IndexOf (item);
            }
        }

        public void Add (T item)
        {
            lock (elements) {
                elements.Add (item);
            }
        }

        public void Remove (T item)
        {
            lock (elements) {
                elements.Remove (item);
            }
        }

        public override T this[int index] {
            get {
                lock (elements) {
                    return elements[index];
                }
            }
        }

        public override int Count {
            get {
                lock (elements) {
                    return elements.Count;
                }
            }
        }

        #endregion

        private IEnumerable<T> Sort(IEnumerable<T> elements, ISortableColumn column)
        {
            switch (column.SortType) {
            case SortType.Ascending:
                return elements
                    .OrderBy (elem => typeof(T).GetProperty(column.SortKey).GetValue(elem, null));
            case SortType.Descending:
                return elements
                    .OrderByDescending (elem => typeof(T).GetProperty(column.SortKey).GetValue(elem, null));
            case SortType.None:
                return elements;
            default:
                Hyena.Log.Debug (String.Format("Unknown SortType {0}", column.SortType));
                return elements;
            }
        }

        public void Sort ()
        {
            if (column != null) {
                lock (elements) { // TODO: check if it is correct
                    if (column != null) {
                        elements = Sort (elements, column).ToList ();
                    }
                }
            }
        }

        #region ISortable implementation

        public bool Sort (ISortableColumn column)
        {
            this.column = column;
            Reload ();
            return true;
        }

        public ISortableColumn SortColumn {
            get {
                return column;
            }

            // setter is not required by ISortable
            set {
                this.Sort (value);
            }
        }

        #endregion

        protected override void OnReloaded ()
        {
            Sort ();
            base.OnReloaded ();
        }
    }
}

