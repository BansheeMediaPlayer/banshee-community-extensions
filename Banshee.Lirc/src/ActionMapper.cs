using System;
using System.Collections;
using Hyena;

namespace Banshee.Lirc
{	
	public class ActionMapper
	{
		private Hashtable action_map;
		private IController ctrl;
		delegate void ActionToPerform();
		
		public ActionMapper( IController controller)
		{
			action_map = new Hashtable();
			ctrl = controller;
			ActionToPerform action;
			action = ctrl.Play;
			action_map.Add("play", action);
			action = ctrl.Pause;
			action_map.Add("pause", action);			
			action = ctrl.Stop;
			action_map.Add("stop", action);			
			action = ctrl.Previous;
			action_map.Add("previous", action);
			action = ctrl.Next;
			action_map.Add("next", action);
			action = ctrl.VolumeUp;
			action_map.Add("volume-up", action);
			action = ctrl.VolumeDown;
			action_map.Add("volume-down", action);
			action = ctrl.Mute;
			action_map.Add("toggle-mute", action);
			action = ctrl.Unhandled;
			action_map.Add("unhandled", action);
		}
		public void DispatchAction(string action)
		{
			ActionToPerform do_this;
			if(action_map.ContainsKey(action))
			{
				do_this = (ActionToPerform)action_map[action];
			}
			else
			{
				do_this = (ActionToPerform)action_map["unhandled"];
				Log.Debug("Unknown LIRC command {0}", action);
			}
			do_this();			
		}
	}
}
