/***************************************************************************
 *  AwnPlugin.cs
 *
 *  Written by Marcos Almeida Jr. <junalmeida@gmail.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.IO;
using Mono.Unix;
using Banshee.Base;
using Banshee.Configuration;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Hyena;
	
namespace Banshee.Awn
{
	
	[NDesk.DBus.Interface("com.google.code.Awn")]
	public interface IAvantWindowNavigator
	{
		void SetTaskIconByName (string TaskName, string ImageFileLocation);
		void UnsetTaskIconByName (string TaskName);
		void SetProgressByName (string TaskName, int SomePercent);
		void SetInfoByName (string TaskName, string SomeText);
		void UnsetInfoByName (string app);
		
		//int AddTaskMenuItemByName (string TaskName, string stock_id, string item_name);
		//int AddTaskCheckItemByName (string TaskName, string item_name, bool active);
		//void SetTaskCheckItemByName (string TaskName, int id, bool active);

		//event MenuItemClickedHandler MenuItemClicked;
		//event CheckItemClickedHandler CheckItemClicked;

		void SetTaskIconByXid (long xid, string ImageFileLocation);
		void UnsetTaskIconByXid (long xid);

	}
    
	public delegate void MenuItemClickedHandler (int id);
	public delegate void CheckItemClickedHandler (int id, bool active);
	
    public class AwnService : Banshee.ServiceStack.IExtensionService, IDisposable
    {
		string[] taskName = new string[] {"banshee", "banshee-1"};	

		private IAvantWindowNavigator awn;
            
        public string ServiceName { get { return "Avant Window Navigator"; } }
   
		PlayerEngineService service = null;				
				
		void IExtensionService.Initialize ()
		{
			try 
			{
			
				Log.Debug("BansheeAwn. Starting...");

				awn = NDesk.DBus.Bus.Session.GetObject<IAvantWindowNavigator> ("com.google.code.Awn",
                                                              new NDesk.DBus.ObjectPath ("/com/google/code/Awn"));
				if (awn == null)
					throw new NullReferenceException();
				service = ServiceManager.PlayerEngine;
		
				service.ConnectEvent(new PlayerEventHandler(this.OnEventChanged));
				
				Log.Debug("BansheeAwn - Initialized");
	
			}
			catch (Exception ex)
			{
				Log.Debug("BansheeAwn - Failed loading Awn Extension. " + ex.Message);
			}
		}
		

        void IDisposable.Dispose()
        {
            UnsetIcon ();
            //UnsetTrackProgress();
			service = null;
			awn = null;
        }

        
			
		private void OnEventChanged (PlayerEventArgs args)
        {
			Log.Debug("BansheeAwn - " + args.Event.ToString());
            switch (args.Event)
			{
			case PlayerEvent.EndOfStream:
			case PlayerEvent.StartOfStream:
                UnsetIcon ();
                SetIcon ();
                break;
            case PlayerEvent.TrackInfoUpdated:
                SetIcon ();
                break;
			case PlayerEvent.StateChange:
				if (service.CurrentState != PlayerState.Playing &&
				    service.CurrentState != PlayerState.Playing)
					UnsetIcon();
				else 
					SetIcon();
				
				break;
            default:
                break;
            }
        }

		private void SetIcon ()
		{
			try 
			{
				if (awn != null && service != null && service.CurrentTrack != null)
				{	
					string fileName = CoverArtSpec.GetPath(service.CurrentTrack.ArtworkId);
					if(File.Exists (fileName))
					{
						for (int i =0; i < taskName.Length; i++)
							awn.SetTaskIconByName (taskName[i], fileName);
						Log.Debug("BansheeAwn - Setting cover: " + fileName);
					}
					else
					{
						Log.Debug("BansheeAwn - No Cover");
					}

				}

			}
			catch 
			{
			}
		}
			/*
		private void SetTrackProgress()
		{
			if (awn != null && service != null) 
			{
				if (service.CurrentTrack == null) 
				{
					UnsetTrackProgress();
				}
				else
				{
					uint total = Convert.ToUInt32(service.CurrentTrack.Duration.TotalSeconds);
					uint current = service.Position;
					int progress = 100;
					if (total > 0)
						progress = Convert.ToInt32(current * 100 / total); 
					//awn.SetProgressByName(taskName, progress); 
					awn.SetInfoByName(taskName, progress.ToString());
					Console.WriteLine("{0} - {1} - {2}", current, total, progress);
				}
			}
		}
		

		private void Progress(object sender, EventArgs e) 
		{
			SetTrackProgress();
			System.Threading.Thread.Sleep(1000);
		}

		private void ProgressCallback(IAsyncResult ar) 
		{
			//timer.EndInvoke(ar);
			//timer.BeginInvoke(new AsyncCallback(this.ProgressCallback), timer);
		}
		*/
		
		private void UnsetIcon ()
		{
            if (awn != null) {
 				for (int i =0; i < taskName.Length; i++)
					awn.UnsetTaskIconByName (taskName[i]);
            }
        }
//       	private void UnsetTrackProgress ()
//		{
//            if (awn != null)
//			{
//				awn.SetProgressByName(taskName, 100);
//				awn.UnsetInfoByName(taskName);
//            }
//        } 
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.awn", "enabled",
            true,
            "Plugin enabled",
            "Awn plugin enabled");
	}
}
