//
// TelepathyNotification.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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
using Gdk;

using Notifications;

namespace Banshee.Telepathy.Gui
{
    public class TelepathyNotification
    {
        private Notification current_nf;
        private static TelepathyNotification instance;
		private readonly object sync = new object ();

        private TelepathyNotification ()
        {
        }

        public static TelepathyNotification Create ()
        {
            if (instance == null) {
                instance = new TelepathyNotification ();
            }

            return instance;
        }

        public static TelepathyNotification Get {
            get { return instance; }
        }

        private bool? actions_supported;
        public bool ActionsSupported {
            get {
                if (!actions_supported.HasValue) {
                    actions_supported = Notifications.Global.Capabilities != null &&
                        Array.IndexOf (Notifications.Global.Capabilities, "actions") > -1;
                }

                return actions_supported.Value;
            }
        }

        public void Dispose ()
        {
            Dispose (true);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                if (current_nf == null) {
                    current_nf.Close ();
                }
                instance = null;
            }
        }

        public void Show (string summary, string body)
        {
            Show (summary, body, null);
        }

        public void Show (string summary, string body, Pixbuf image)
        {
           if (image == null) {
                image = Banshee.Gui.IconThemeUtils.LoadIcon (48, "banshee");
                if (image != null) {
                    image.ScaleSimple (42, 42, Gdk.InterpType.Bilinear);
                }
            }

			lock (sync) {
	            try {
	                if (current_nf == null) {
	                    current_nf = new Notification (summary,
	                        body, image);
	                } else {
	                    current_nf.Summary = summary;
	                    current_nf.Body = body;
	                    current_nf.Icon = image;
	                    //current_nf.AttachToWidget (notif_area.Widget);
	                }
	                current_nf.Urgency = Urgency.Low;
	                current_nf.Timeout = 4500;
	//                if (!current_track.IsLive && ActionsSupported && interface_action_service.PlaybackActions["NextAction"].Sensitive) {
	//                    current_nf.AddAction ("skip-song", AddinManager.CurrentLocalizer.GetString ("Skip this item"), OnSongSkipped);
	//                }
	                current_nf.Show ();
					Hyena.Log.Debug ("Showing notification");
	            } catch (Exception e) {
					Console.WriteLine (e.StackTrace);
	                Hyena.Log.Warning ("Cannot show notification", e.Message, false);
	            }
			}
        }
    }
}