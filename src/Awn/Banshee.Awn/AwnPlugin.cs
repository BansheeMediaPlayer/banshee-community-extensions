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
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Hyena;

namespace Banshee.Awn
{
    [DBus.Interface("com.google.code.Awn")]
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
        string[] taskName = new string[] { "banshee", Banshee.ServiceStack.Application.InternalName };

        private IAvantWindowNavigator awn;

        public string ServiceName {
            get { return "Avant Window Navigator"; }
        }

        PlayerEngineService service = null;

        void IExtensionService.Initialize ()
        {
            try {
                Log.DebugFormat ("BansheeAwn. Starting {0}", Application.ActiveClient.ClientId);

                awn = DBus.Bus.Session.GetObject<IAvantWindowNavigator> ("com.google.code.Awn",
                        new DBus.ObjectPath ("/com/google/code/Awn"));

                // Dummy call to check that awn is really there
                awn.UnsetTaskIconByName ("banshee-dummy");

                service = ServiceManager.PlayerEngine;
                service.ConnectEvent (new PlayerEventHandler (this.OnEventChanged));

                Log.Debug ("BansheeAwn - Initialized");

            } catch (Exception ex) {
                Log.Exception ("BansheeAwn - Failed loading Awn Extension. ", ex);
                awn = null;
            }
        }

        void IDisposable.Dispose ()
        {
            UnsetIcon ();
            //UnsetTrackProgress ();
            service = null;
            awn = null;
        }

        private void OnEventChanged (PlayerEventArgs args)
        {
            Log.DebugFormat ("BansheeAwn - {0}", args.Event.ToString ());
            switch (args.Event) {
                //case PlayerEvent.EndOfStream:
                //    UnsetIcon ();
                //    break;
                //case PlayerEvent.StartOfStream:
                //    SetIcon ();
                //    break;
                case PlayerEvent.TrackInfoUpdated:
                    SetIcon ();
                    break;
                case PlayerEvent.StateChange:
                    if (service != null) {
                        if (service.CurrentState != PlayerState.Playing)
                            UnsetIcon ();
                        else
                            SetIcon ();
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// This was made as a workaround on new awn and banshee versions.
        /// Guess that I need to discover window's XID cause taskname changes with music.
        /// May only work in english gnome.
        /// </summary>
        private string PossibleTitle {
            get {
                if (service == null || service.CurrentTrack == null)
                    return "Banshee Media Player";
                else
                    return String.Format ("{0} by {1}", service.CurrentTrack.TrackTitle, service.CurrentTrack.ArtistName);
            }
        }

        private void SetIcon ()
        {
            try {
                if (awn != null && service != null && service.CurrentTrack != null) {
                    string fileName = CoverArtSpec.GetPath (service.CurrentTrack.ArtworkId);
                    if (File.Exists (fileName)) {
                        for (int i = 0; i < taskName.Length; i++)
                            awn.SetTaskIconByName (taskName[i], fileName);
                        awn.SetTaskIconByName (PossibleTitle, fileName);
                        Log.DebugFormat ("BansheeAwn - Setting cover: {0}", fileName);
                    } else {
                        UnsetIcon ();
                        Log.Debug ("BansheeAwn - No Cover");
                    }
                }
            } catch (Exception ex) {
                Log.Exception ("BansheeAwn - Error setting icon", ex);
            }
        }
        /*
        private void SetTrackProgress ()
        {
            if (awn != null && service != null) {
                if (service.CurrentTrack == null) {
                    UnsetTrackProgress();
                }
                else {
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


        private void Progress (object sender, EventArgs e)
        {
            SetTrackProgress ();
            System.Threading.Thread.Sleep (1000);
        }

        private void ProgressCallback (IAsyncResult ar)
        {
            //timer.EndInvoke (ar);
            //timer.BeginInvoke (new AsyncCallback (this.ProgressCallback), timer);
        }
        */

        private void UnsetIcon ()
        {
            if (awn != null) {
                for (int i = 0; i < taskName.Length; i++)
                    awn.UnsetTaskIconByName (taskName[i]);
                awn.UnsetTaskIconByName (this.PossibleTitle);
            }
        }

//        private void UnsetTrackProgress ()
//        {
//            if (awn != null) {
//                awn.SetProgressByName(taskName, 100);
//                awn.UnsetInfoByName(taskName);
//            }
//        }
    }
}
