using System;

namespace Banshee.Lirc
{	
	
	public interface IController
	{
		void Play();
		void Pause();
		void Stop();
		void Next();
		void Previous();
		void VolumeUp();
		void VolumeDown();
		void Mute();
		void Unhandled();
	}
}
