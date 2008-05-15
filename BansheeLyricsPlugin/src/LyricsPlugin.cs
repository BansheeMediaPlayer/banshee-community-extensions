using System;
using Gtk;
using GConf;
using System.IO;
using Mono.Unix;
using Banshee.Base;
using Banshee.Sources;
using Banshee.MediaEngine;
using System.Threading;

public static class PluginModuleEntry
{
	public static Type [] GetTypes()
	{
    return new Type [] {typeof(Banshee.Plugins.Lyrics.LyricsPlugin)};
	}
}


namespace Banshee.Plugins.Lyrics
{

public partial class LyricsPlugin : Banshee.Plugins.Plugin
{

	/* Begin Plugin overrides */
	protected override string ConfigurationName { get { return "Lyrics"; }
                                            }
	public override string DisplayName { get { return "Lyrics"; }}

	public override string Description {
    	get {return Constants.plugin_description;}
	}

	public override string [] Authors {
    	get {
    		return new string [] {"Christian Martellini <christian.martellni@gmail.com>,Fabiano Ridolfi",
    												"Thanks to: Maurizio Tardozzi,Jose Ramon Palanco"};
    	}
	}
	/* End Plugin overrides */

	/* Begin plugin initialization */
	protected override void PluginInitialize()
	{
		InitPlugin();
	}

	protected override void PluginDispose()
	{
		PlayerEngineCore.EventChanged -= OnPlayerEngineEventChanged;
		DisposePlugin();
	}
	
	protected override void InterfaceInitialize(){
		PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;
		InstallInterfaceActions();
	}
	
	/*event handler*/
	void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
	{
		if(!Enabled) {
			return;
		}
		
		if (args.Event == PlayerEngineEvent.StartOfStream ||args.Event == PlayerEngineEvent.TrackInfoUpdated){
				dialog.Update();
		}
	}
}

}
