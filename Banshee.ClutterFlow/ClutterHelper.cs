
using System;
using System.Runtime.InteropServices;
using GLib;
using Clutter;

namespace Banshee.ClutterFlow
{

    /*[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct sVertex
    {       
        public float X;
		public float Y;
		public float Z;
		
		public sVertex(float X, float Y, float Z) {
			this.X = X;
			this.Y = Y;
			this.Z = Z;
		}
    }	*/
	
	public static class ClutterHelper
	{
		[DllImport ("libcairo.so.2")]
		private static extern void cairo_destroy(System.IntPtr context);
		
		[DllImport ("libclutter-glx-1.0.so.0")]
    	private static extern void clutter_actor_destroy(System.IntPtr actor);
		[DllImport ("libclutter-glx-1.0.so.0")]
		private static extern void clutter_group_remove_all(System.IntPtr group);
		
		public static void DestroyActor(Clutter.Actor actor) {
			clutter_actor_destroy(actor.Handle);
		}
		public static void RemoveAllFromGroup(Clutter.Group group) {
			clutter_group_remove_all(group.Handle);
		}
		public static void DestroyCairoContext(Cairo.Context context) {
			cairo_destroy(context.Handle);
		}
	}
}
