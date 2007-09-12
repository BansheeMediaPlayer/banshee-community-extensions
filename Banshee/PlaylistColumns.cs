/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections;
using Mono.Unix;
using Gtk;
using GConf;

using Banshee.Base;
using Banshee.Database;
using Banshee.Configuration;
using Banshee.Plugins.Mirage;

namespace Banshee
{
    public class PlaylistColumn
    {
        public string Name;
        public TreeViewColumn Column;
        public int Order;
    
        private string keyName;
        private bool preference;    // User preference
        private bool hidden;        // Source preference
        private static GConf.Client gcc = new GConf.Client();
    
        public PlaylistColumn(PlaylistGeneratorView view, string name, string keyName,
            TreeCellDataFunc datafunc, CellRenderer renderer, 
            int Order, int SortId)
        {
            this.Name = name;
            this.keyName = keyName;
            this.Order = Order;
            
            Column = new TreeViewColumn();
            Column.Title = name;
            Column.Resizable = true;
            Column.Reorderable = true;
            Column.Sizing = TreeViewColumnSizing.Fixed;
            Column.PackStart(renderer, false);
            Column.SetCellDataFunc(renderer, datafunc);
                
            if(SortId >= 0) {
                Column.Clickable = true;
                Column.SortColumnId = SortId;
            } else {
                Column.Clickable = false;
                Column.SortColumnId = -1;
                Column.SortIndicator = false;
            }
            
            try {
                int width = (int)gcc.Get("/apps/banshee/view_columns/"+keyName+"/width");
                    
                if(width <= 1)
                    throw new Exception(Catalog.GetString("Invalid column width"));
                    
                Column.FixedWidth = width;
            } catch(Exception) { 
                Column.FixedWidth = 75;
            }
            
            try {
                preference = Column.Visible = (bool)gcc.Get("/apps/banshee/view_columns/"+keyName+"/visible");
                this.Order = (int)gcc.Get("/apps/banshee/view_columns/"+keyName+"/order");
            } catch(Exception) {}
        }
        
        public void Save(TreeViewColumn [] columns)
        {
            // find current order
            int order_t = 0,  n = columns.Length;
            for(; order_t < n; order_t++)
                if(columns[order_t].Equals(Column))
                    break;
                    
            gcc.Set("/apps/banshee/view_columns/"+keyName+"/width", Column.Width);
            gcc.Set("/apps/banshee/view_columns/"+keyName+"/order", order_t);
            gcc.Set("/apps/banshee/view_columns/"+keyName+"/visible", preference);
        }

        // This is what the user wants (and what's stored in GConf)
        public bool VisibilityPreference {
            set {
                preference = value;
                Column.Visible = (preference && !hidden);
            }
            get {
                return preference;
            }
        }

        // This can be set by the source, to hide a specific column
        public bool Hidden {
            set {
                hidden = value;
                Column.Visible = (preference && !hidden);
            }
            get {
                return hidden;
            }
        }
    }

    public class PlaylistColumnChooserDialog : Gtk.Window
    {    
        private Hashtable boxes;
    
        public PlaylistColumnChooserDialog(ArrayList columns) 
            : base(Catalog.GetString("Choose Columns"))
        {
            BorderWidth = 10;
            SetPosition(WindowPosition.Center);
            TypeHint = Gdk.WindowTypeHint.Utility;
            Resizable = false;
            
            IconThemeUtils.SetWindowIcon(this);
        
            VBox vbox = new VBox();
            vbox.Spacing = 10;
            vbox.Show();
            
            Add(vbox);
            
            Label label = new Label();
            label.Markup = "<b>" + Catalog.GetString("Visible Playlist Columns") + "</b>";
            label.Show();
            vbox.Add(label);
            
            Table table = new Table(
                (uint)System.Math.Ceiling((double)columns.Count), 
                2, false);

            table.Show();
            table.ColumnSpacing = 15;
            table.RowSpacing = 5;
            vbox.Add(table);

            boxes = new Hashtable();
                        
            int i = 0;
            foreach(PlaylistColumn plcol in columns) {
                CheckButton cbtn = new CheckButton(plcol.Name);
                boxes[cbtn] = plcol;
                cbtn.Show();
                cbtn.Toggled += OnCheckButtonToggled;
                cbtn.Active = plcol.Column.Visible;
                table.Attach(cbtn, 
                    (uint)(i % 2), 
                    (uint)((i % 2) + 1), 
                    (uint)(i / 2), 
                    (uint)(i / 2) + 1,
                    AttachOptions.Fill,
                    AttachOptions.Fill,
                    0, 0);
                i++;
            }
            
            HButtonBox actionArea = new HButtonBox();
            actionArea.Show();
            actionArea.Layout = ButtonBoxStyle.End;    
            
            Button closeButton = new Button("gtk-close");
            closeButton.Clicked += OnCloseButtonClicked;
            closeButton.Show();
            actionArea.PackStart(closeButton);

            vbox.Add(actionArea);
        }
        
        private void OnCheckButtonToggled(object o, EventArgs args)
        {
            CheckButton button = (CheckButton)o;
            
            ((PlaylistColumn)boxes[button]).VisibilityPreference = button.Active;
        }
        
        private void OnCloseButtonClicked(object o, EventArgs args)
        {
            Destroy();
        }
    }
}
