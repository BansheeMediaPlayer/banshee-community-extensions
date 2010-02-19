using System;
using Banshee.ServiceStack;
using Banshee.Base;

namespace Banshee.Lirc
{
	public class BansheeController : IController
	{		
        private ushort volume_increment = 5;
		private short volume_before_mute = -1;

		public BansheeController()
		{
		}
		
		public void Play()
		{
			ServiceManager.PlayerEngine.Play();
		}
			
		public void Pause()
		{
			ServiceManager.PlayerEngine.Pause();
		}
		
		public void Stop()
		{
			ServiceManager.PlayerEngine.Close();
		}
		
		public void Next()
		{
			ServiceManager.PlaybackController.Next();
		}
			
		public void Previous()
		{
			ServiceManager.PlaybackController.Previous();
		}
			
		public void VolumeUp()
		{
			ServiceManager.PlayerEngine.Volume += volume_increment;
		}
			
		public void VolumeDown()
		{
			ServiceManager.PlayerEngine.Volume -= volume_increment;
		}
			
		public void Mute()
		{
		    if(volume_before_mute == -1)
		    {
			   volume_before_mute = (short)ServiceManager.PlayerEngine.Volume;
			   ServiceManager.PlayerEngine.Volume = 0;
		    }
		    else
		    {					   
			   ServiceManager.PlayerEngine.Volume = (ushort)volume_before_mute;
			   volume_before_mute = -1;
		    }			
		}
		
		public void Unhandled()
		{			
		}
	}
}
