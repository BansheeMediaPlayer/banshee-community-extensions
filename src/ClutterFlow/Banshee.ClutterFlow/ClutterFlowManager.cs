//
// ClutterFlowManager.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
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

using Clutter;

using ClutterFlow;

namespace Banshee.ClutterFlow
{
    internal static class ClutterFlowManager
    {
        private static int state = 0;
        public static event EventHandler BeforeQuit;

        public static void Init ()
        {
            if (state < 1) {
                //TODO provide a static class for initialisation that does not get destroyed when this service is
                // it should hold reference to the ActorLoader instances, as they hold precious references to texture,
                // data that apparently remains in memory
                if (!GLib.Thread.Supported) {
                    GLib.Thread.Init ();
                }
                Clutter.Threads.Init ();
                if (ClutterHelper.gtk_clutter_init (IntPtr.Zero, IntPtr.Zero) != InitError.Success) {
                    throw new System.NotSupportedException ("Unable to initialize GtkClutter");
                }
                System.AppDomain.CurrentDomain.ProcessExit += HandleProcessExit;
                state = 1;
            }
        }

        static void HandleProcessExit (object sender, EventArgs e)
        {
            if (state == 1) {
                System.AppDomain.CurrentDomain.ProcessExit -= HandleProcessExit;
            }
            Quit ();
        }

        public static void Quit ()
        {
            if (state == 1) {
                var handler = BeforeQuit;
                if (handler != null) {
                    handler (null, EventArgs.Empty);
                }
                Clutter.Application.Quit ();
                filter_view.Dispose ();
                filter_view = null;
                state = 2;
            }
        }

        private static ClutterFlowView filter_view;
        public static ClutterFlowView FilterView {
            get {
                if (filter_view == null) {
                    filter_view = new ClutterFlowView ();
                } else if (!filter_view.Attached) {
                    filter_view.AttachEvents ();
                }
                return filter_view;
            }
        }
    }
}

