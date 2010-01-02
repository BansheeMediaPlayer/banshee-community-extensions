
using System;
using System.Runtime.InteropServices;
using GLib;
using Clutter;

namespace ClutterFlow
{
	
	public static class ClutterHelper
	{
		[DllImport ("libclutter-glx-1.0.so.0")]
    	private static extern void clutter_actor_destroy(System.IntPtr actor);
		[DllImport ("libclutter-glx-1.0.so.0")]
		private static extern void clutter_group_remove_all(System.IntPtr group);
		[DllImport ("libclutter-glx-1.0.so.0")]
		private static extern void clutter_container_remove(System.IntPtr container, System.IntPtr actor);
		
		public static void DestroyActor(Clutter.Actor actor) {
			clutter_actor_destroy(actor.Handle);
		}
		public static void RemoveAllFromGroup(Clutter.Group group) {
			clutter_group_remove_all(group.Handle);
		}
		public static void RemoveFromGroup(System.IntPtr group, Clutter.Actor actor) {
			clutter_container_remove(group, actor.Handle);
		}		
	}
}
