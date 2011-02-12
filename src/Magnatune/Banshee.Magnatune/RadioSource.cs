/*
Magnatune Plugin for Banshee. This source provides just the basics
to browse Magnatune's genres.

Copyright 2008 Max Battcher <me@worldmaker.net>.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Hyena;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Gdk;
using Gtk;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Banshee.Magnatune
{
    public class Genre
    {
        private string title;
        private string desc;
        private string id;

        public string Title {
            get { return title; }
        }

        public string Description {
            get { return desc; }
        }

        public string Id {
            get { return id; }
        }

        public Genre (string title, string desc, string id)
        {
            this.title = title;
            this.desc = desc;
            this.id = id;
        }

        public SafeUri GetM3uUri (string type, string user, string pass)
        {
            return new SafeUri (string.Format ("http://{0}:{1}@{2}.magnatune.com/genres/m3u/{3}_nospeech.m3u",
                                               user, pass, type, id));
        }

        public SafeUri GetM3uUri ()
        {
            return new SafeUri (string.Format ("http://magnatune.com/genres/m3u/{0}.m3u", id));
        }
    }

    public class RadioSource : Source, IDisposable
    {
        private ActionGroup actions;
        private uint ui_manager_id;
        private InterfaceActionService action_service;

        public RadioSource () : base("Magnatune", "Magnatune", 200, "magnatune")
        {
            Pixbuf icon = new Pixbuf (System.Reflection.Assembly.GetExecutingAssembly ()
                                      .GetManifestResourceStream ("simple_icon.png"));
            Properties.Set<Pixbuf> ("Icon.Pixbuf_16", icon.ScaleSimple (16, 16, InterpType.Bilinear));
            Properties.Set<ISourceContents> ("Nereid.SourceContents", new RadioSourceContents ());
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);

            actions = new ActionGroup ("Magnatune");

            actions.Add (new ActionEntry[] {
                new ActionEntry ("MagnatuneAction", null, "_Magnatune", null,
                                 "Configure the Magnatune Addin", null),
                new ActionEntry ("MagnatuneConfigureAction", Stock.Properties, "_Configure", null,
                                 "Configure the Magnatune addin", OnConfigurePlugin)
            });

            action_service = ServiceManager.Get<InterfaceActionService> ();

            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("MagnatuneMenu.xml");
        }

        public static List<Genre> GetGenres ()
        {
            List<Genre> genres = new List<Genre> ();
            XmlTextReader xtr = new XmlTextReader (System.Reflection.Assembly.GetExecutingAssembly ()
                                                   .GetManifestResourceStream ("genres.xml"));
            xtr.Read ();
            // xml decl
            xtr.Read ();
            // <genres>
            while (!xtr.EOF) {
                xtr.Read ();
                if (xtr.Name == "genre") {
                    genres.Add (new Genre (xtr.GetAttribute ("title"), xtr.GetAttribute ("desc"),
                                           xtr.GetAttribute ("id")));
                }
            }

            xtr.Close ();
            return genres;
        }

        public void OnConfigurePlugin (object o, EventArgs args)
        {
            Configuration config = new Configuration (MembershipTypeSchema.Get (), UsernameSchema.Get (),
                                                      PasswordSchema.Get ());
            config.Run ();
            config.Destroy ();
        }

        public void Dispose ()
        {
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;
        }

        public static readonly SchemaEntry<string> MembershipTypeSchema = new SchemaEntry<string> (
            "plugins.magnatune.membership", "type", "", "Membership Type", "Membership Type: streaming or download");

        public static readonly SchemaEntry<string> UsernameSchema = new SchemaEntry<string> (
            "plugins.magnatune.membership", "user", "", "Magnatune user", "Magnatune member username");

        public static readonly SchemaEntry<string> PasswordSchema = new SchemaEntry<string> (
            "plugins.magnatune.membership", "pass", "", "Magnatune password", "Magnatune member password");
    }
}
