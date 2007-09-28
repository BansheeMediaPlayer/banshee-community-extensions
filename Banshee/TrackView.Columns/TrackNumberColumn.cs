/***************************************************************************
 *  TrackNumberColumn.cs
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Configuration;
using Banshee.Plugins.Mirage;

namespace Banshee.Plugins.Mirage.TrackView.Columns
{
    public class TrackNumberColumn : TrackViewColumnText
    {
        public const int ID = (int)TrackColumnID.TrackNumber;
    
        public TrackNumberColumn() : base(Catalog.GetString("Track"), ID)
        {
            SetCellDataFunc(Renderer, new TreeCellDataFunc(DataHandler));
            Resizable = false;
            fixed_width_strings = new string [] { Title, "999999" };
        }
        
        protected void DataHandler(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = Model.IterTrackInfo(iter);
            if(ti == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell,
			    ti.TrackNumber > 0 ? Convert.ToString(ti.TrackNumber) : String.Empty, iter);
        }
        
        protected override ModelCompareHandler CompareHandler {
            get { return ModelCompare; }
        }
        
        public static int ModelCompareBase(PlaylistModel model, TreeIter a, TreeIter b)
        {
            return ModelCompareBase(model, a, b, false);
        }
        
        public static int ModelCompareBase(PlaylistModel model, TreeIter a, TreeIter b, bool ascending)
        {
            int ascending_value = 1;
            int column;
            SortType sort_type;
            
            if(ascending && model.GetSortColumnId(out column, out sort_type)) {
                ascending_value = sort_type == SortType.Ascending ? 1 : -1;
            }
            
            return ascending_value * LongFieldCompare((long)model.IterTrackInfo(a).TrackNumber,
                (long)model.IterTrackInfo(b).TrackNumber);
        }
        
        public static int ModelCompare(PlaylistModel model, TreeIter a, TreeIter b)
        {
            int v = ArtistColumn.ModelCompare(model, a, b, false);
            return v != 0 ? v : ModelCompareBase(model, a, b);
        }

        public static readonly SchemaEntry<int> order_schema = new SchemaEntry<int>(
            "view_columns.track", "order",
            ID,
            "Order",
            "Order of Track column"
        );
        
        public static readonly SchemaEntry<bool> visible_schema = new SchemaEntry<bool>(
            "view_columns.track", "visible",
            true,
            "Visiblity",
            "Visibility of Track column"
        );
        
        protected override SchemaEntry<int> OrderSchema {
            get { return order_schema; }
        }
        
        protected override SchemaEntry<bool> VisibleSchema {
            get { return visible_schema; }
        }
    }
}
