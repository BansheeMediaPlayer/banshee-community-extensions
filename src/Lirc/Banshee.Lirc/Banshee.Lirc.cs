using System;
using System.Threading;

using Hyena;
using Banshee.ServiceStack;
using Banshee.Base;
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
		private ActionGroup actions;
		private InterfaceActionService action_service;
        
        public LircPlugin()
        {
			actions = new ActionGroup("Lirc");
			ctrl = new ActionMapper(new BansheeController());
			actions.Add(new ActionEntry[] {
				new ActionEntry("LircAction", null, "_Lirc", null,
				                "Configure the Lirc Addin", null),
				new ActionEntry("LircConfigureAction", Stock.Properties, "_Configure",
				                null, "Configure the Lirc addin", OnConfigurePlugin),
			});
			
			action_service = ServiceManager.Get<InterfaceActionService>("InterfaceActionService");
			
			action_service.UIManager.InsertActionGroup(actions, 0);
            action_service.UIManager.AddUiFromResource("Ui.xml");
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

		public void OnConfigurePlugin(object o, EventArgs args)
		{
            ConfigDialog dlg = new ConfigDialog();
			dlg.Run();
			dlg.Destroy();
		}

		~LircPlugin()
        {
        }
               
        private void PollThread()
        {
            Console.WriteLine("Waiting for LIRC button press...");
            string command;
            while (lirc.ErrorValue >= 0) {
                command = lirc.NextCommand ();
                ctrl.DispatchAction(command);
            }
            Console.WriteLine("Lost connection to LIRC daemon.  FIXME: should try to reconnect");
        }

		string IService.ServiceName {
            get { return "LIRC Extension"; }
        }
    }
}
