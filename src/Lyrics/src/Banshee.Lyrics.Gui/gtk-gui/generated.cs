//  
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
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

namespace Banshee.Lyrics.Gui {
    
    
    internal class Gui {
        
        private static bool initialized;
        
        internal static void Initialize(Gtk.Widget iconRenderer) {
            if ((Gui.initialized == false)) {
                Gui.initialized = true;
            }
        }
    }
    
    internal class BinContainer {
        
        private Gtk.Widget child;
        
        private Gtk.UIManager uimanager;
        
        public static BinContainer Attach(Gtk.Bin bin) {
            BinContainer bc = new BinContainer();
            bin.SizeRequested += new Gtk.SizeRequestedHandler(bc.OnSizeRequested);
            bin.SizeAllocated += new Gtk.SizeAllocatedHandler(bc.OnSizeAllocated);
            bin.Added += new Gtk.AddedHandler(bc.OnAdded);
            return bc;
        }
        
        private void OnSizeRequested(object sender, Gtk.SizeRequestedArgs args) {
            if ((this.child != null)) {
                args.Requisition = this.child.SizeRequest();
            }
        }
        
        private void OnSizeAllocated(object sender, Gtk.SizeAllocatedArgs args) {
            if ((this.child != null)) {
                this.child.Allocation = args.Allocation;
            }
        }
        
        private void OnAdded(object sender, Gtk.AddedArgs args) {
            this.child = args.Widget;
        }
        
        public void SetUiManager(Gtk.UIManager uim) {
            this.uimanager = uim;
            this.child.Realized += new System.EventHandler(this.OnRealized);
        }
        
        private void OnRealized(object sender, System.EventArgs args) {
            if ((this.uimanager != null)) {
                Gtk.Widget w;
                w = this.child.Toplevel;
                if (((w != null) && typeof(Gtk.Window).IsInstanceOfType(w))) {
                    ((Gtk.Window)(w)).AddAccelGroup(this.uimanager.AccelGroup);
                    this.uimanager = null;
                }
            }
        }
    }
    
    internal class IconLoader {
        
        public static Gdk.Pixbuf LoadIcon(Gtk.Widget widget, string name, Gtk.IconSize size, int sz) {
            Gdk.Pixbuf res = widget.RenderIcon(name, size, null);
            if ((res != null)) {
                return res;
            }
            else {
                try {
                    return Gtk.IconTheme.Default.LoadIcon(name, sz, 0);
                }
                catch (System.Exception ) {
                    if ((name != "gtk-missing-image")) {
                        return IconLoader.LoadIcon(widget, "gtk-missing-image", size, sz);
                    }
                    else {
                        Gdk.Pixmap pmap = new Gdk.Pixmap(Gdk.Screen.Default.RootWindow, sz, sz);
                        Gdk.GC gc = new Gdk.GC(pmap);
                        gc.RgbFgColor = new Gdk.Color(255, 255, 255);
                        pmap.DrawRectangle(gc, true, 0, 0, sz, sz);
                        gc.RgbFgColor = new Gdk.Color(0, 0, 0);
                        pmap.DrawRectangle(gc, false, 0, 0, (sz - 1), (sz - 1));
                        gc.SetLineAttributes(3, Gdk.LineStyle.Solid, Gdk.CapStyle.Round, Gdk.JoinStyle.Round);
                        gc.RgbFgColor = new Gdk.Color(255, 0, 0);
                        pmap.DrawLine(gc, (sz / 4), (sz / 4), ((sz - 1) - (sz / 4)), ((sz - 1) - (sz / 4)));
                        pmap.DrawLine(gc, ((sz - 1) - (sz / 4)), (sz / 4), (sz / 4), ((sz - 1) - (sz / 4)));
                        return Gdk.Pixbuf.FromDrawable(pmap, pmap.Colormap, 0, 0, 0, 0, sz, sz);
                    }
                }
            }
        }
    }
    
    internal class ActionGroups {
        
        public static Gtk.ActionGroup GetActionGroup(System.Type type) {
            return ActionGroups.GetActionGroup(type.FullName);
        }
        
        public static Gtk.ActionGroup GetActionGroup(string name) {
            return null;
        }
    }
}
