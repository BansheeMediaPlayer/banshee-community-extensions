/***************************************************************************
 *  TrackViewColumn.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Configuration;
using Banshee.Plugins.Mirage;

namespace Banshee.Plugins.Mirage.TrackView.Columns
{
    public enum TrackColumnID
    {
        TrackNumber,
        Artist,
        Title,
        Album,
        Duration,
        Rating,
        Genre,
        Year,
        PlayCount,
        LastPlayed,
        Uri,
        DateAdded
    }

    public class TrackViewColumn : TreeViewColumn, IComparable<TrackViewColumn>
    {
        public class IDComparer : IComparer<TrackViewColumn>
        {
            public int Compare(TrackViewColumn a, TrackViewColumn b)
            {
                return a.ColumnID.CompareTo(b.ColumnID);
            }
        }
    
        protected delegate int ModelCompareHandler(PlaylistModel model, TreeIter a, TreeIter b); 
    
        private int id;
        private int order;
        private bool preference;    // User preference
        private bool hidden;        // Source preference
        
        private PlaylistModel model;
        private CellRenderer renderer;
        private Menu menu;
        
        protected string [] fixed_width_strings;
        
        protected virtual ModelCompareHandler CompareHandler {
            get { return null; }
        }
        
        protected virtual SchemaEntry<int> WidthSchema { 
            get { return SchemaEntry<int>.Zero; }
        }
        
        protected virtual SchemaEntry<int> OrderSchema {
            get { return SchemaEntry<int>.Zero; }
        }
        
        protected virtual SchemaEntry<bool> VisibleSchema {
            get { return SchemaEntry<bool>.Zero; }
        }
    
        public TrackViewColumn(string title, CellRenderer renderer, int order) : base()
        {
            this.order = order;
            this.id = order;
            this.renderer = renderer;
            
            Title = title;
            Resizable = true;
            Reorderable = true;
            Sizing = TreeViewColumnSizing.Fixed;
            
            if(renderer is CellRendererText) {
                ((CellRendererText)renderer).Ellipsize = Pango.EllipsizeMode.End;
            }
            
            PackStart(renderer, true);
            
            Clickable = true;
            SortColumnId = id;
            
            int fixed_width = !WidthSchema.Equals(SchemaEntry<int>.Zero) ? WidthSchema.Get() : 75;
            FixedWidth = fixed_width <= 0 ? 75 : fixed_width;
            preference = Visible = !VisibleSchema.Equals(SchemaEntry<bool>.Zero) ? VisibleSchema.Get() : true;
            
            if(!OrderSchema.Equals(SchemaEntry<int>.Zero)) {
                this.order = OrderSchema.Get();
            }
        }
        
        public void CreatePopupableHeader()
        {
            Widget = new Label(Title);
            Widget.Show();
            
            Widget parent_widget = Widget.Parent;
            Button parent_button = parent_widget as Button;
            
            while(parent_widget != null && parent_button == null) {
                parent_widget = parent_widget.Parent;
                parent_button = parent_widget as Button;
            }
            
            if(parent_button != null) {
                parent_button.ButtonPressEvent += OnLabelButtonPressEvent;
                
                menu = new Menu();
                
                MenuItem columns_item = new MenuItem(Catalog.GetString("Columns..."));
                
                // Translators: {0} is the title of the column, e.g. 'Hide Artist'
                MenuItem hide_item = new MenuItem(String.Format(Catalog.GetString("Hide {0}"), Title));
                
                columns_item.Activated += delegate { Globals.ActionManager["ColumnsAction"].Activate(); };
                hide_item.Activated += delegate { VisibilityPreference = !VisibilityPreference; };
                
                menu.Add(columns_item);
                menu.Add(hide_item);
                
                menu.ShowAll();
            }
        }
        
        [GLib.ConnectBefore]
        private void OnLabelButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if(args.Event.Button == 3 && menu != null) {
                menu.Popup();
                args.RetVal = false;
            }
        }
        
        public void Save(TreeViewColumn [] columns)
        {
            // find current order
            int order_t = 0,  n = columns.Length;
            for(; order_t < n; order_t++)
                if(columns[order_t].Equals(this))
                    break;

            if(!VisibleSchema.Equals(SchemaEntry<bool>.Zero)) {
                VisibleSchema.Set(preference);
            }

            if(!WidthSchema.Equals(SchemaEntry<int>.Zero)) {
                WidthSchema.Set(Width);
            }
            
            if(!OrderSchema.Equals(SchemaEntry<int>.Zero)) {
                OrderSchema.Set(order_t);
            }
        }
   
        protected void SetRendererAttributes(CellRendererText renderer, string text, TreeIter iter)
        {
            renderer.Text = text;
            renderer.Weight = iter.Equals(model.PlayingIter) 
                ? (int)Pango.Weight.Bold 
                : (int)Pango.Weight.Normal;

            renderer.Foreground = null;
            renderer.Sensitive = true;

            TrackInfo ti = model.IterTrackInfo(iter);
            if(ti == null) {
                return;
            }

            renderer.Sensitive = ti.CanPlay && ti.PlaybackError == TrackPlaybackError.None;

            renderer.CellBackground = null;
            foreach (TrackInfo t in PlaylistGeneratorSource.seeds) {
                if (ti == t) {
                    renderer.CellBackground = "#FFF065";
                }
            }

        }
        
        public void SetMaxFixedWidth(TreeView view)
        {
            int max_width = 0;
            
            if(fixed_width_strings == null) {
                return;
            }
            
            foreach(string str in fixed_width_strings) {
                int width, height;
                
                Pango.Layout layout = new Pango.Layout(view.PangoContext);
                layout.SetText(str);
                layout.GetPixelSize(out width, out height);
                
                if(width > max_width) {
                    max_width = width;
                }
            }
            
            FixedWidth = (int)Math.Ceiling(max_width * 1.4);
        }
        
        public int CompareTo(TrackViewColumn column)
        {
            return Order.CompareTo(column.Order);
        }
        
        protected int TreeIterCompareFunc(TreeModel _model, TreeIter a, TreeIter b)
        {
            return CompareHandler != null ? CompareHandler(model, a, b) : 0;
        }
        
        public static int LongFieldCompare(long a, long b)
        {
            return a < b ? -1 : (a == b ? 0 : 1);
        }
        
        public static int DefaultTreeIterCompareFunc(TreeModel model, TreeIter a, TreeIter b)
        {
            return 0;
        }
        
        // This is what the user wants (and what's stored in GConf)
        public bool VisibilityPreference {
            set { preference = value; Visible = (preference && !hidden); }
            get { return preference; }
        }

        // This can be set by the source, to hide a specific column
        public bool Hidden {
            set { hidden = value; Visible = (preference && !hidden); }
            get { return hidden; }
        }
        
        public int Order { 
            get { return order; }
        }
        
        public int ColumnID {
            get { return id; }
        }
        
        public PlaylistModel Model {
            get { return model; }
            set { 
                model = value;
                
                if(CompareHandler != null) {
                    model.SetSortFunc(ColumnID, new TreeIterCompareFunc(TreeIterCompareFunc));
                }
            }
        }
        
        public CellRenderer Renderer {
            get { return renderer; }
        }
    }
    
    public class TrackViewColumnText : TrackViewColumn
    {
        public TrackViewColumnText(string title, int order) : base(title, new CellRendererText(), order)
        {
        }
    }
}
