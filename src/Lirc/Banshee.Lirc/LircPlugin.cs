using System;
using System.Threading;

using Hyena;

using Banshee.ServiceStack;
using Banshee.Gui;
using Gtk;

using Lirc;

namespace Banshee.Lirc
{
    public class LircPlugin : IExtensionService
    {

        private LircClient lirc;
        private Thread poll;
		private ActionMapper ctrl;

        public LircPlugin()
        {
			ctrl = new ActionMapper(new BansheeController());
        }

		void IExtensionService.Initialize()
		{
            lirc = new LircClient ("banshee");
            poll = new Thread(new ThreadStart(PollThread));
			poll.Start();
		}

		public void Dispose ()
		{
            poll.Abort();
            lirc.Dispose ();
            lirc = null;
		}

        private void PollThread()
        {
            Log.Debug ("Waiting for LIRC button press...");
            string command;
            while (lirc.ErrorValue >= 0) {
                command = lirc.NextCommand ();
                ctrl.DispatchAction(command);
            }
            // FIXME: Should try to reconnect
            Log.Debug("Lost connection to LIRC daemon");
        }

		string IService.ServiceName {
            get { return "LIRC Extension"; }
        }
    }
}
