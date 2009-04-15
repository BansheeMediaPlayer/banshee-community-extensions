// LyricsPluginBase.cs created with MonoDevelop
// User: sgrang at 1:28 PM 4/12/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Gtk;
using GConf;
using System.IO;
using Mono.Unix;
using Banshee.MediaEngine;
using System.Threading;

namespace Banshee.Plugins.Lyrics
{
	
public partial class LyricsPlugin
{

	public  static event 	LyricEventHandler	 LyricEvent;
	public  static event 	TextSaveEventHandler TextSaveEvent;
	private static 			LyricsWindow 		 dialog;
	
	private bool   			enabled = true;
	private uint   			ui_manager_id;
	private ActionGroup 	actions;
	
	protected void InitPlugin(){
		CreatePluginDir();
		InitDialog();
	}
	
	protected void DisposePlugin(){
		BansheeWidgets.GetUiManager().RemoveUi(ui_manager_id);
		BansheeWidgets.GetUiManager().RemoveActionGroup(actions);
		HideDialog();
	}
	
	private void ShowDialog ()
	{
		dialog.Show();
		dialog.Present();
	}

	private void HideDialog ()
	{
		dialog.Hide();
	}

	private void CreatePluginDir(){			
		if (!Directory.Exists(LyricsDefaultDir)){
			Directory.CreateDirectory(LyricsDefaultDir);
		}
	}
	
	private void InstallInterfaceActions(){
		actions= new ActionGroup("Lyrics");
		actions.Add(new ActionEntry [] {
			new ActionEntry (Constants.lyric_action, null,
			                      Constants.show_lyrics, "<control>T",
			                      Constants.show_lyrics, OnToggleShow)
				});
		actions.Sensitive = true;
		
		BansheeWidgets.GetUiManager().InsertActionGroup(actions,0);
		ui_manager_id = BansheeWidgets.GetUiManager().AddUiFromResource("LyricsMenu.xml");
	}	
	
	private void InitDialog (){
		lock(this) {
			if(dialog == null) {
				dialog = new LyricsWindow();
			}
			if (ValidTrack){
				dialog.Update();
			}
		}
	}
	
	private static string GetGConfKey(string key, string defaultValue){
		GConf.Client gconf = new GConf.Client();
		string retval;
		try {				
			retval= (string)gconf.Get(key);
			if (retval.Equals("") && defaultValue!=String.Empty){
				gconf.Set(key,defaultValue);
				retval=defaultValue;			
			}		
		}
		catch(Exception) {//string not found
			gconf.Set(key,defaultValue);				
			retval= defaultValue;
		}
		return retval;				
	}


	/*event handler*/
	public static void OnLyricEvent(System.Object o,LyricEventArgs e)
	{
		if(LyricEvent!=null)
			LyricEvent(o,e);
	}

	public static void OnTextSaveEvent(System.Object o,TextSaveEventArgs e)
	{
		if(TextSaveEvent!=null){
			TextSaveEvent(o,e);
		}
	}

	private void OnToggleShow(object o, EventArgs args)
	{
		if (dialog==null)
			return;
		
		if (!dialog.Visible || !dialog.IsActive){
			ShowDialog();
		}
	}

	/*end event handler*/
	
	/*properties*/
	bool Enabled 
	{
    	get { return enabled; }
    	set { enabled = value;}
	}

	bool ValidTrack 
	{
    	get {
        	return (BansheeWidgets.CurrentTrack.GetCurrentTrack() != null &&
                BansheeWidgets.CurrentTrack.GetArtist() != null);
    	}
	}

	static internal string LyricsDefaultDir 
	{
   	 get {
		UnixUserInfo user= UnixUserInfo.GetRealUser();				
		return GetGConfKey(Constants.path_key, user.HomeDirectory+Constants.default_lyrics_dir);
   	 }
   	 set {
    	GConf.Client gconf = new GConf.Client();
		string defaultDir=value;
		if (!defaultDir.EndsWith("/"))
			 defaultDir=defaultDir+"/";			    
    	gconf.Set(Constants.path_key, defaultDir);
   	 }
	}
	/* End properties */
}
}
