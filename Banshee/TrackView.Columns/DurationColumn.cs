/***************************************************************************
 *  DurationColumn.cs
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
    public class DurationColumn : TrackViewColumnText
    {
        public const int ID = (int)TrackColumnID.Duration;
    
        public DurationColumn() : base(Catalog.GetString("Time"), ID)
        {
            SetCellDataFunc(Renderer, new TreeCellDataFunc(DataHandler));
        }
        
        protected void DataHandler(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            TrackInfo Track = Model.IterTrackInfo(iter);
            if(Track == null) {
                return;
            }
            
            SetRendererAttributes((CellRendererText)cell, 
                Track.Duration.TotalSeconds < 0.0 ? Catalog.GetString("N/A") : 
                DateTimeUtil.FormatDuration((long)Track.Duration.TotalSeconds), iter);
        }
                
        protected override ModelCompareHandler CompareHandler {
            get { return ModelCompare; }
        }
        
        public static int ModelCompare(PlaylistModel model, TreeIter a, TreeIter b)
        {
            return model.IterTrackInfo(a).Duration.CompareTo(model.IterTrackInfo(b).Duration);
        }
        
        public static readonly SchemaEntry<int> width_schema = new SchemaEntry<int>(
            "view_columns.time", "width",
            60,
            "Width",
            "Width of Time column"
        );
        
        public static readonly SchemaEntry<int> order_schema = new SchemaEntry<int>(
            "view_columns.time", "order",
            ID,
            "Order",
            "Order of Time column"
        );
        
        public static readonly SchemaEntry<bool> visible_schema = new SchemaEntry<bool>(
            "view_columns.time", "visible",
            true,
            "Visiblity",
            "Visibility of Time column"
        );
        
        protected override SchemaEntry<int> WidthSchema {
            get { return width_schema; }
        }
        
        protected override SchemaEntry<int> OrderSchema {
            get { return order_schema; }
        }
        
        protected override SchemaEntry<bool> VisibleSchema {
            get { return visible_schema; }
        }
    }
}
