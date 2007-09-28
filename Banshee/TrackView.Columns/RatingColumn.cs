/***************************************************************************
 *  RatingColumn.cs
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

using Banshee.Gui;
using Banshee.Base;
using Banshee.Configuration;
using Banshee.Plugins.Mirage;

namespace Banshee.Plugins.Mirage.TrackView.Columns
{
    public class RatingColumn : TrackViewColumn
    {
        public const int ID = (int)TrackColumnID.Rating;
        
        public RatingColumn() : base(Catalog.GetString("Rating"), new CellRendererRating(), ID)
        {
            CellRendererRating rating_renderer = Renderer as CellRendererRating;
            rating_renderer.RatingChanged += OnRatingChanged;
            SetCellDataFunc(rating_renderer, new TreeCellDataFunc(DataHandler));
            Resizable = false;
            FixedWidth = CellRendererRating.Width;
        }
        
        protected void DataHandler(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = Model.IterTrackInfo(iter);
            if(ti == null) {
                return;
            }
             
            ((CellRendererRating)cell).Rating = ti.Rating;

            cell.CellBackground = null;
            foreach (TrackInfo t in PlaylistGeneratorSource.seeds) {
                if (ti == t) {
                    cell.CellBackground = "#FFF065";
                }
            }
        }
        
        private void OnRatingChanged(object o, CellRatingChangedArgs args)
        {
            TreeIter iter;
            if(Model.GetIter(out iter, args.Path)) {
                try {
                    TrackInfo track = (TrackInfo)Model.GetValue(iter, 0);
                    track.Rating = args.Rating;
                } catch {
                }
            }
        }
                
        protected override ModelCompareHandler CompareHandler {
            get { return ModelCompare; }
        }
        
        public static int ModelCompare(PlaylistModel model, TreeIter a, TreeIter b)
        {
            return LongFieldCompare((long)model.IterTrackInfo(a).Rating, (long)model.IterTrackInfo(b).Rating);
        }
        
        public static readonly SchemaEntry<int> order_schema = new SchemaEntry<int>(
            "view_columns.rating", "order",
            ID,
            "Order",
            "Order of Rating column"
        );
        
        public static readonly SchemaEntry<bool> visible_schema = new SchemaEntry<bool>(
            "view_columns.rating", "visible",
            true,
            "Visiblity",
            "Visibility of Rating column"
        );
        
        protected override SchemaEntry<int> OrderSchema {
            get { return order_schema; }
        }
        
        protected override SchemaEntry<bool> VisibleSchema {
            get { return visible_schema; }
        }
    }
}
