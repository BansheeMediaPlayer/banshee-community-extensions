//
// CoverWallpaperService.cs
//
// Authors:
//   David Corrales <corrales.david@gmail.com>
//
// Copyright (C) 2009 David Corrales.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using GConf;

using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.MediaEngine;

namespace Banshee.CoverWallpaper
{
    public class CoverWallpaperService : IExtensionService
    {
        private InterfaceActionService action_service;
        private ArtworkManager artwork_manager_service;
        private bool disposed;
        private TrackInfo current_track;
        private Gdk.Pixbuf image;
		
		private static GConf.Client gClient;
		private static string GCONF_BACKGROUND_PATH = "/desktop/gnome/background/picture_filename";
        private static string albumWallpaper = 
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/banshee-1/banshee-wallpaper.png";
		private string userWallpaper = "";
        private string lastAlbum = "";
        
        public CoverWallpaperService () {}
        
        void IExtensionService.Initialize ()
        {  
            action_service = ServiceManager.Get<InterfaceActionService> ();
            artwork_manager_service = ServiceManager.Get<ArtworkManager> ();
            
            if (!ServiceStartup ())
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
        }
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ())
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
        }
        
        private bool ServiceStartup ()
        {
            if (action_service == null || ServiceManager.SourceManager.MusicLibrary == null)
                return false;
            
            Initialize ();
            
            return true;
        }
        
        private void Initialize ()
        {            
            Banshee.Base.ThreadAssist.AssertInMainThread ();
            
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
               PlayerEvent.StartOfStream |
               PlayerEvent.TrackInfoUpdated);
			
			// capture the current wallpaper to reestablish when exiting or in albums with no art
            gClient = new GConf.Client();
			try {
				userWallpaper = (string) gClient.Get(GCONF_BACKGROUND_PATH);
			} catch (GConf.NoSuchKeyException ex) {
				Console.WriteLine(ex.Message);
				//TODO: handle this exception (shouldn't happen though)
			}
        }
        
        public void Dispose ()
        {
            if (disposed)
                return;

            Banshee.Base.ThreadAssist.ProxyToMain (delegate {
                ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
                action_service = null;
                artwork_manager_service = null;
                current_track = null;
                image = null;
            
				// reestablish the user wallpaper
				SetWallpaper(userWallpaper);
				
                disposed = true;
            });
        }
    
        private void OnPlayerEvent (PlayerEventArgs args) 
        {
            switch (args.Event) {
                case PlayerEvent.StartOfStream:
                case PlayerEvent.TrackInfoUpdated:
                    current_track = ServiceManager.PlayerEngine.CurrentTrack;
                
                    //check to see if there was an album change
                    if (lastAlbum != current_track.AlbumTitle) {
                    
                        if (AlbumArtExists(current_track))
                            SetWallpaper(albumWallpaper);
                        else
                            SetWallpaper(userWallpaper);
                    
                        lastAlbum = current_track.AlbumTitle;
                    }
                    break;
            }
        }
        
        private bool AlbumArtExists(TrackInfo currentTrack)
        {
            image = artwork_manager_service.LookupPixbuf(current_track.ArtworkId);
            
            if (image == null || !image.Save(albumWallpaper, "png"))
                return false;
            else
                return true;
        }
        
        private void SetWallpaper(string filename)
        {
            try {
                if (filename != string.Empty)
                    gClient.Set(GCONF_BACKGROUND_PATH, filename);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                //TODO: handle this exception
            }
        }
        
        string IService.ServiceName {
            get { return "CoverWallpaperService"; }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.cover_wallpaper", "enabled",
            true,
            "Plugin enabled",
            "Cover wallpaper plugin enabled"
        );
    }
}
