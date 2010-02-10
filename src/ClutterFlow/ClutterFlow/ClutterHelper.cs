// 
// ClutterHelper.cs
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
using System.Runtime.InteropServices;
using GLib;
using Clutter;

namespace ClutterFlow
{
	
	public static class ClutterHelper
	{
		[DllImport ("libclutter-glx-1.0.so.0")]
    	private static extern void clutter_actor_destroy (System.IntPtr actor);
		[DllImport ("libclutter-glx-1.0.so.0")]
		private static extern void clutter_group_remove_all (System.IntPtr group);
		[DllImport ("libclutter-glx-1.0.so.0")]
		private static extern void clutter_container_remove (System.IntPtr container, System.IntPtr actor);
		[DllImport ("libclutter-glx-1.0.so.0")]
		public static extern IntPtr cogl_texture_new_from_data (uint width, uint height, Cogl.TextureFlags flags, Cogl.PixelFormat format, Cogl.PixelFormat internal_format, uint rowstride, IntPtr data);
		
		[DllImport ("libclutter-glx-1.0.so.0")]
		public static extern void clutter_texture_set_cogl_texture (IntPtr texture, IntPtr cogl_tex);

        [DllImport ("libclutter-gtk-0.10.so.0")]
        public static extern Clutter.InitError gtk_clutter_init (IntPtr argc, IntPtr argv);

		public static void DestroyActor(Clutter.Actor actor) {
            GC.SuppressFinalize (actor);
			clutter_actor_destroy(actor.Handle);
		}
		public static void RemoveAllFromGroup(Clutter.Group group) {
            foreach (Actor actor in group)
                GC.SuppressFinalize (actor);
			clutter_group_remove_all(group.Handle);
		}
		public static void RemoveFromGroup(System.IntPtr group, Clutter.Actor actor) {
            GC.SuppressFinalize (actor);
			clutter_container_remove(group, actor.Handle);
		}
        
	}
}
