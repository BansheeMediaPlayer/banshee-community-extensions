//
// ContactSourceContents.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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
using System.Collections;
using System.Collections.Generic;

using Gtk;
using Mono.Addins;

using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Telepathy.Data;
using Banshee.Widgets;

using Banshee.Telepathy.API;

namespace Banshee.Telepathy.Gui
{
    public class ContactSourceContents : Hyena.Widgets.ScrolledWindow, ISourceContents
    {
        private VBox main_box;
        private Viewport viewport;

        private TitledList contacts;
        private TileView contacts_view;

        private readonly IDictionary <Contact, MenuTile> tile_map = new Dictionary<Contact, MenuTile> ();

        public ContactSourceContents (ContactContainerSource source)
        {
            this.source = source;

            HscrollbarPolicy = PolicyType.Never;
            VscrollbarPolicy = PolicyType.Automatic;

            viewport = new Viewport ();
            viewport.ShadowType = ShadowType.None;

            main_box = new VBox ();
            main_box.Spacing = 6;
            main_box.BorderWidth = 5;
            main_box.ReallocateRedraws = true;

            // Clamp the width, preventing horizontal scrolling
            SizeAllocated += delegate (object o, SizeAllocatedArgs args) {
                // TODO '- 10' worked for Nereid, but not for Cubano; properly calculate the right width we should request
                main_box.WidthRequest = args.Allocation.Width - 30;
            };

            viewport.Add (main_box);

            StyleSet += delegate {
                viewport.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                viewport.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
            };

            AddWithFrame (viewport);
            ShowAll ();
        }

        public void Refresh ()
        {
            tile_map.Clear ();

            if (contacts_view == null) {
                contacts_view = new TileView (1);
            }

            if (contacts == null) {
                contacts = new TitledList (AddinManager.CurrentLocalizer.GetString ("Contacts"));
                contacts.PackStart (contacts_view, true, true, 0);
                contacts_view.Show ();

                contacts.StyleSet += delegate {
                    contacts_view.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                    contacts_view.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
                };
            }

            while (main_box.Children.Length != 0) {
                main_box.Remove (main_box.Children[0]);
            }

            main_box.PackStart (contacts, false, false, 0);

            contacts_view.ClearWidgets ();
            foreach (Connection conn in source.TelepathyService.GetActiveConnections ()) {
                AppendToList (conn);
            }

            ShowAll ();
        }

        private void AppendToList (Connection conn)
        {
            if (conn == null || conn.Roster == null) {
                return;
            }

            foreach (Contact contact in conn.Roster.GetAllContacts ()) {
                if (contact == null || contact.Avatar == null) {
                    continue;
                }

                MenuTile tile = new MenuTile ();
                tile.SizeAllocated += delegate (object o, SizeAllocatedArgs args) {
                    int main_width, main_height = 0;
                    main_box.GetSizeRequest (out main_width, out main_height);

                    tile.WidthRequest = main_width;
                };

                tile.PrimaryText = contact.Name;
                tile.SecondaryText = String.IsNullOrEmpty (contact.StatusMessage) ? contact.Status.ToString () : contact.StatusMessage;

                Avatar avatar = contact.Avatar;
                if (avatar.State == AvatarState.Loaded) {
                    tile.Pixbuf = new Gdk.Pixbuf (avatar.Image);
                    avatar.Clear (false);
                }
                else {
                    tile_map.Add (contact, tile);

                    avatar.Loaded += delegate(object sender, AvatarStateEventArgs e) {
                        Avatar a = sender as Avatar;
                        if (a != null && tile_map.ContainsKey (a.Contact)) {
                            if (e.State == AvatarState.Loaded) {
                                tile_map[a.Contact].Pixbuf = new Gdk.Pixbuf (a.Image);
                                a.Clear (false);

                                main_box.QueueDraw ();
                            }

                            tile_map.Remove (a.Contact);
                        }
                    };

                    avatar.Load ();
                }

                contacts_view.AddWidget (tile);
            }
        }

#region ISourceContents

        public bool SetSource (ISource src)
        {
            source = src as ContactContainerSource;
            if (source == null) {
                return false;
            }

            Refresh ();
            return true;
        }

        private ContactContainerSource source;
        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
            tile_map.Clear ();
        }

        public Widget Widget {
            get { return this; }
        }

#endregion

    }
}
