/***************************************************************************
 *  UriColumn.cs
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
    public class UriColumn : TrackViewColumnText
    {
        public const int ID = (int)TrackColumnID.Uri;
       
        public UriColumn() : base(Catalog.GetString("Location"), ID)
        {
            SetCellDataFunc(Renderer, new TreeCellDataFunc(DataHandler));
        }
        
        protected void DataHandler(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = Model.IterTrackInfo(iter);
            if(ti == null || ti.Uri == null) {
                return;
            }
            
            string str = ti.Uri.AbsoluteUri;
            if(ti.Uri.IsLocalPath) {
                str = ti.Uri.LocalPath;
                int len = Globals.Library.CachedLocation.Length;
                if(str.StartsWith(Globals.Library.CachedLocation) && str.Length > len + 2) {
                    str = str.Substring(len + 1);
                }
            }
            
            SetRendererAttributes((CellRendererText)cell, str, iter);
        }
                
        protected override ModelCompareHandler CompareHandler {
            get { return ModelCompare; }
        }
                
        public static int ModelCompare(PlaylistModel model, TreeIter a, TreeIter b)
        {
            return ModelCompare(model, a, b, false);
        }
        
        public static int ModelCompare(PlaylistModel model, TreeIter a, TreeIter b, bool folder)
        {
            SafeUri uri_a = model.IterTrackInfo(a).Uri;
            SafeUri uri_b = model.IterTrackInfo(b).Uri;
            
            if(folder && uri_a.IsLocalPath && uri_b.IsLocalPath) {
                string folder_a = System.IO.Path.GetDirectoryName(uri_a.LocalPath);
                string folder_b = System.IO.Path.GetDirectoryName(uri_b.LocalPath);
                
                return String.Compare(folder_a, folder_b);
            }
            
            if(uri_a == null) {
                return -1;
            } else if(uri_b == null) {
                return 1;
            }
            
            return String.Compare(uri_a.AbsoluteUri, uri_b.AbsoluteUri);
        }
        
        public static readonly SchemaEntry<int> width_schema = new SchemaEntry<int>(
            "view_columns.uri", "width",
            75,
            "Width",
            "Width of Uri column"
        );
        
        public static readonly SchemaEntry<int> order_schema = new SchemaEntry<int>(
            "view_columns.uri", "order",
            ID,
            "Order",
            "Order of Uri column"
        );
        
        public static readonly SchemaEntry<bool> visible_schema = new SchemaEntry<bool>(
            "view_columns.uri", "visible",
            false,
            "Visiblity",
            "Visibility of Uri column"
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
