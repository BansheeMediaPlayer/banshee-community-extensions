//
// AndroidRemoteSource.cs
//
// Authors:
//   Nikitas Stamatopoulos / Kristopher Dick
//
// Copyright (C) 2013 Nikitas Stamatopoulos
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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

using Mono.Unix;
using Mono.Addins;

using Banshee.Collection;
using Banshee.Sources;
using Banshee.PlaybackController;
using Banshee.ServiceStack;
using Banshee.Preferences;
using Banshee.Configuration;

using Hyena;

namespace Banshee.AndroidRemote
{
    
    public class AndroidRemoteService : IExtensionService, IDisposable
    {
        Socket bansheeServerConn;
		ushort volume = ServiceManager.PlayerEngine.Volume;
		byte[] socketBuffer = new byte[5000];
		
		private PreferenceBase port_pref;
		PreferenceService bansheePrefs;
		int port;
        
        string IService.ServiceName
	    {
            get { return "AndroidRemote"; }
        }

        void IExtensionService.Initialize()
		{	
			InstallPreferences();
            Console.WriteLine("AndroidRemote: Preferences installed");
			
			bansheePrefs["RemoteControl"]["AndroidRemote"]["remote_control_port"].ValueChanged += delegate {
				listen();
			};
            
			listen();
		}
		
		void IDisposable.Dispose()
		{
            UninstallPreferences();
			bansheeServerConn.Close();
		}
		
		public void listen () 
        {
			if (bansheeServerConn != null)
            {
				bansheeServerConn.Disconnect(false);
			}
			
			port = (int) bansheePrefs["RemoteControl"]["AndroidRemote"]["remote_control_port"].BoxedValue;
			Console.WriteLine("AndroidRemote will listen on port " + port.ToString());
			IPEndPoint bansheeServer_ep = new IPEndPoint (IPAddress.Any, port);
			
			bansheeServerConn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			bansheeServerConn.Bind(bansheeServer_ep);
			bansheeServerConn.Listen(1);
			bansheeServerConn.BeginAccept(new AsyncCallback(OnIncomingConnection), bansheeServerConn);
		}
		
		void OnIncomingConnection (IAsyncResult ar)
        {
            try
            {
                Socket client = ((Socket)ar.AsyncState).EndAccept(ar);
                client.BeginReceive(socketBuffer, 0, socketBuffer.Length, SocketFlags.None, OnSocketReceive,client);
                bansheeServerConn.BeginAccept(new AsyncCallback(OnIncomingConnection), bansheeServerConn);
            }
            catch (Exception)
            {
            }
        }
        
        void OnSocketReceive (IAsyncResult ar) {
			Socket client = null;
			int bytes = 0;
			try
            {
                client = (Socket) ar.AsyncState;
                bytes =  client.EndReceive(ar);
                
                client.BeginReceive(socketBuffer, 0, socketBuffer.Length, SocketFlags.None, OnSocketReceive, client);
                
                
                string text = Encoding.UTF8.GetString(socketBuffer,0,bytes);
                string sep = "/";
                string[] remoteMessage = text.Split('/');
                string action = remoteMessage[0];
                string variable = remoteMessage[1];
                if (action.Equals("play"))
                {
                    variable = variable.Replace('*','/');
                }
			
                Banshee.Collection.TrackInfo currTrack = ServiceManager.PlayerEngine.CurrentTrack;
                string replyText = "";
                ushort currVol;
                ushort volStep = 10;
                bool replyReq = false;
                string home = Environment.GetEnvironmentVariable("HOME");
                string coverPath="";
                string dbPath = home + "/.config/banshee-1/banshee.db";
                if(currTrack != null && currTrack.ArtworkId != null){
                    coverPath = home + "/.cache/media-art/" + currTrack.ArtworkId.ToString() +".jpg";
                }
                
                switch (action)
                {
                case "coverImage":
                    byte[] coverImage = File.ReadAllBytes(coverPath);
                    client.Send(coverImage);
                    replyReq = true;
                    break;
                    
                case "syncCount":
                    int count = System.IO.File.ReadAllBytes(dbPath).Length;
                    client.Send(System.Text.Encoding.UTF8.GetBytes(count.ToString()));
                    replyReq=true;
                    break;
                    
                case "sync":
                    byte[] db = File.ReadAllBytes(dbPath);
                    client.Send(db);
                    replyReq = true;
                    break;
                    
                case "coverExists":	
                    replyText = coverExists(coverPath);
                    replyReq = true;
                    break;
                    
                case "playPause":			
                    ServiceManager.PlayerEngine.TogglePlaying();
                    replyReq = true;
                    break;
                    
                case "next":				
                    ServiceManager.PlaybackController.Next();
                    replyReq = true;
                    break;
                    
                case "prev":				
                    ServiceManager.PlaybackController.Previous();
                    replyReq = true;
                    break;
                    
                case "play":
                    var source = ServiceManager.SourceManager.MusicLibrary as DatabaseSource;
                    source.FilterQuery="";
                    if(source!=null)
                    {
                        var countSongs = source.Count;
                        
                        UnknownTrackInfo track = new UnknownTrackInfo(new SafeUri(variable));
                        TrackInfo trackTemp = null;
                        for(int i=0; i<countSongs; i++)
                        {
                            trackTemp = source.TrackModel [i];
                            if(trackTemp.TrackEqual(track))
                            {
                                break;
                            }
                        }
                        if(trackTemp != null)
                            ServiceManager.PlayerEngine.OpenPlay (trackTemp);
                    }
                    replyReq = true;
                    break;
                    
                case "volumeDown":			
                    currVol = ServiceManager.PlayerEngine.Volume;
                    if (currVol < 10) 
                    {
                        ServiceManager.PlayerEngine.Volume = 0;	
                    }
                    else 
                    {
                        ServiceManager.PlayerEngine.Volume = (ushort) (currVol - volStep);	
                    }
                    replyReq = true;
                    break;
                    
                case "volumeUp":			
                    currVol = ServiceManager.PlayerEngine.Volume;
                    if (currVol > 90) 
                    {
                        ServiceManager.PlayerEngine.Volume = 100;
                    }
                    else 
                    {
                        ServiceManager.PlayerEngine.Volume = (ushort) (currVol + volStep);
                    }
                    replyReq = true;
                    break;
                    
                case "mute":				
					currVol = ServiceManager.PlayerEngine.Volume;
					if (currVol > 0) 
                    {
						volume = currVol;
						ServiceManager.PlayerEngine.Volume = 0;
                    }
                    else 
                    {
                        ServiceManager.PlayerEngine.Volume = volume;	
                    }
                    replyReq = true;
                    break;
                case "status":						
                    replyText = ServiceManager.PlayerEngine.CurrentState.ToString().ToLower();
                    replyReq = true;
                    break;
                    
                case "album":				
                    replyText = currTrack.DisplayAlbumTitle;
                    replyReq = true;
                    break;
                    
                case "artist":				
                    replyText = currTrack.DisplayArtistName;
                    replyReq = true;
                    break;
                    
                case "title":				
                    replyText = currTrack.DisplayTrackTitle;
                    replyReq = true;
                    break;
                case "trackCurrentTime":	
                    replyText = (ServiceManager.PlayerEngine.Position/1000).ToString();
                    replyReq = true;
                    break;
                    
                case "trackTotalTime":		
                    replyText = currTrack.Duration.ToString();
                    replyReq = true;
                    break;
                    
                case "seek":				
                    ServiceManager.PlayerEngine.Position = UInt32.Parse(variable)*1000;
                    replyReq = true;
                    break;
                    
                case "shuffle":				 
                    if (ServiceManager.PlaybackController.ShuffleMode.ToString() == "off") 
                    {
                        ServiceManager.PlaybackController.ShuffleMode = "song";
                        replyText = "song";
                    }
                    else if(ServiceManager.PlaybackController.ShuffleMode.ToString() == "song") 
                    {
                        ServiceManager.PlaybackController.ShuffleMode = "artist";
                        replyText = "Artist";
                    }
                    else if(ServiceManager.PlaybackController.ShuffleMode.ToString() == "artist") 
                    {
                        ServiceManager.PlaybackController.ShuffleMode = "album";
                        replyText = "Album";
                    }
                    else if(ServiceManager.PlaybackController.ShuffleMode.ToString() == "album") 
                    {
                        ServiceManager.PlaybackController.ShuffleMode = "rating";
                        replyText = "Rating";
                    }
                    else if(ServiceManager.PlaybackController.ShuffleMode.ToString() == "rating")
                    {
                        ServiceManager.PlaybackController.ShuffleMode = "score";
                        replyText = "Score";
                    }
                    else
                    {
                        ServiceManager.PlaybackController.ShuffleMode = "off";
                        replyText = "off";
                    }
                    replyReq = true;
                    break;
                    
                case "repeat":				 
                    if (ServiceManager.PlaybackController.RepeatMode == PlaybackRepeatMode.None)
                    {
                        ServiceManager.PlaybackController.RepeatMode = PlaybackRepeatMode.RepeatAll;
                        replyText = "all";
                    }
                    else if (ServiceManager.PlaybackController.RepeatMode == PlaybackRepeatMode.RepeatAll)
                    {
                        ServiceManager.PlaybackController.RepeatMode = PlaybackRepeatMode.RepeatSingle;
                        replyText = "single";
                    }
                    else 
                    {
                        ServiceManager.PlaybackController.RepeatMode = PlaybackRepeatMode.None;
                        replyText = "off";
                    }
                    replyReq = true;
                    break;
                    
                case "all":
                    replyText = ServiceManager.PlayerEngine.CurrentState.ToString().ToLower() + sep;
                    replyText += currTrack.DisplayAlbumTitle.Replace('/','\\') + sep;
                    replyText += currTrack.DisplayArtistName.Replace('/','\\') + sep;
                    replyText += currTrack.DisplayTrackTitle.Replace('/','\\') + sep;
                    replyText += ((uint) (ServiceManager.PlayerEngine.Position/1000)).ToString() + sep;
                    replyText += ((uint) (currTrack.Duration.TotalSeconds)).ToString() + sep;
                    replyText += coverExists(coverPath);
                    replyReq = true;
                    break;
                    
                case "test":
                    replyText = "";
                    replyReq = true;
                    break;
                    
                default:
                    replyText = "";
                    replyReq = false;
                    break;
                }
                
                byte[] messageByte = System.Text.Encoding.UTF8.GetBytes(replyText);
                
                if (replyReq)
                {
                    reply(client, messageByte);
                }
				client.Close();
			}
			catch(Exception)
            {
			}
        }
		
		void reply (Socket remoteClient, byte[] reply) 
        {
			remoteClient.Send(reply);
		}
		
		string coverExists(string coverPath)
        {
			string retVal = "false";
            if (System.IO.File.Exists(coverPath))
            {
				if (System.IO.File.ReadAllBytes(coverPath).Length < 110000)
                {
					retVal = "true";
				}
			}
            return retVal;
		}
            
        private void InstallPreferences()
        {
			bansheePrefs = ServiceManager.Get<PreferenceService>();
			
			if (bansheePrefs == null)
            {
				return;
			}
			Page remoteControlPage = new Page("RemoteControl", "Remote Control", 3);
			bansheePrefs.FindOrAdd(remoteControlPage);
			
			Section BansheeRemotePrefs = remoteControlPage.FindOrAdd(new Section("AndroidRemote", "Android Remote", 0));
            
			port_pref = BansheeRemotePrefs.Add (new SchemaPreference<int>(
                                                                          RemotePortSchema,
                                                                          Catalog.GetString("Android Remote port"),
                                                                          Catalog.GetString("Banshee will listen for the Android Banshee Remote app on this port")));
		}

        private void UninstallPreferences()
        {
            Console.WriteLine("AndroidRemote: UninstallPreferences() called");
            bansheePrefs["RemoteControl"]["AndroidRemote"].Remove(port_pref);
            bansheePrefs["RemoteControl"].Remove(bansheePrefs["RemoteControl"].FindById("AndroidRemote"));
            bansheePrefs.Remove(bansheePrefs.FindById("RemoteControl"));
        }

        public static readonly SchemaEntry<int> RemotePortSchema = new SchemaEntry<int> (
                                                                                         "remote_control", "remote_control_port",
                                                                                         8484,1024,49151,
                                                                                         "Android Remote Port",
                                                                                         "Android Remote will listen for the Android Banshee Remote app on this port"
                                                                                         );
    }
}
