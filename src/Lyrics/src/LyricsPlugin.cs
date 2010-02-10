using System;
using Gtk;
using GConf;
using System.IO;
using Mono.Unix;
using Banshee.ServiceStack;
using System.Threading;
using Banshee.MediaEngine;

namespace Banshee.Plugins.Lyrics 
{

public partial class LyricsPlugin : IExtensionService, IDisposable{
		
		void IExtensionService.Initialize (){
			ServiceManager.PlayerEngine.ActiveEngine.EventChanged += OnPlayerEngineEventChanged;
			InstallInterfaceActions();
			InitPlugin();
		}
     
        public void Dispose ()
        {
			ServiceManager.PlayerEngine.ActiveEngine.EventChanged -= OnPlayerEngineEventChanged;
			DisposePlugin();
        }
                
        string IService.ServiceName {
            get { return "LyricsPlugin"; }
        }
        
        void OnPlayerEngineEventChanged (PlayerEventArgs args)	
        {
		    if(!Enabled) {
		        return;
		    }
		    
		    if(ValidTrack){
		    	actions.Sensitive = true;
		    	if (args.Event == PlayerEvent.StartOfStream ||args.Event == PlayerEvent.TrackInfoUpdated){					
		    		dialog.Update();
		    	}
		    }else{
		   		actions.Sensitive = false;
		    }
		}
	}
}
