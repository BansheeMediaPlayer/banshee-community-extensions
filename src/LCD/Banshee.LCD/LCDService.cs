//
// LCDService.cs
//
// Authors:
//   André Gaul
//
// Copyright (C) 2010 André Gaul
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
using System.Collections.Generic;

using Gtk;
using Mono.Addins;

using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Gui;

namespace Banshee.LCD
{
    public class LCDService : IExtensionService, IDisposable
    {
        private LCDClient lcdclient;
        InterfaceActionService action_service;
        ActionGroup actions;
        uint ui_manager_id;

        private LCDParser parser;
        private LCDScreen idlescreen;
        private LCDWidgetTitle idletitle;
        private LCDWidgetScroller idletext;
        private Dictionary<LCDScreen, HashSet<LCDWidget> > userscreens;

        public LCDService ()
        {
            Hyena.Log.Debug ("Instantiating LCD service");
        }

        void IExtensionService.Initialize ()
        {
            Hyena.Log.Debug ("Initializing LCD service");

            action_service = ServiceManager.Get<InterfaceActionService> ();
            actions = new ActionGroup ("LCD");
            actions.Add (new ActionEntry [] {
                new ActionEntry ("LCDAction", null,
                    AddinManager.CurrentLocalizer.GetString ("LCD"), null,
                    null, null),
                new ActionEntry ("LCDConfigureAction", Stock.Properties,
                    AddinManager.CurrentLocalizer.GetString ("_Configure..."), null,
                    AddinManager.CurrentLocalizer.GetString ("Configure the LCD plugin"), OnConfigure)
            });
            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("LCDMenu.xml");

            ScreensCreate();

            lcdclient = new LCDClient(Host, Port);
            lcdclient.Connected += OnConnected;
            parser = new LCDParser();
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                PlayerEvent.Iterate |
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream |
                PlayerEvent.TrackInfoUpdated |
                PlayerEvent.StateChange);

        }

        private void ScreensCreate() {
            idlescreen = new LCDScreen("idlescreen", LCDScreen.Prio.Foreground, 32);
            idletitle = new LCDWidgetTitle("idletitle", "Banshee");
            idletext = new LCDWidgetScroller("idletext",1,2,16,1,LCDWidgetScroller.Direction.Horizontal,3,"Status: %S");

            userscreens = new Dictionary<LCDScreen, HashSet<LCDWidget> >();

            LCDScreen trackscreen = new LCDScreen("trackscreen", LCDScreen.Prio.Foreground, 32);
            userscreens[trackscreen] = new HashSet<LCDWidget>();
            userscreens[trackscreen].Add(new LCDWidgetScroller("artisttext",1,1,16,1,LCDWidgetScroller.Direction.Marquee,2,"%A / %B"));
            userscreens[trackscreen].Add(new LCDWidgetString("numtext",1,2,"%N/%C"));
            userscreens[trackscreen].Add(new LCDWidgetScroller("tracktext",7,2,9,1,LCDWidgetScroller.Direction.Horizontal,2,"%T"));

            LCDScreen posscreen = new LCDScreen("posscreen", LCDScreen.Prio.Foreground, 32);
            userscreens[posscreen] = new HashSet<LCDWidget>();
            userscreens[posscreen].Add(new LCDWidgetScroller("postext",1,2,16,1,LCDWidgetScroller.Direction.Marquee,2," %P / %L"));
        }

        private void ScreensDelete() {
            idlescreen = null;
            idletitle = null;
            idletext = null;

            userscreens = null;
        }

        public void Dispose ()
        {
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            Hyena.Log.Debug ("Disposing LCD service");
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;
            lcdclient.Dispose();
            lcdclient = null;
            ScreensDelete();
        }

        private void OnPlayerEvent(PlayerEventArgs args)
        {
            parser.ProcessEvent(args,
                ServiceManager.PlayerEngine.CurrentTrack,
                ServiceManager.PlayerEngine.Position,
                ServiceManager.PlayerEngine.Length);

            if (args.Event == PlayerEvent.StateChange)
            {
                switch(((PlayerEventStateChangeArgs)args).Current)
                {
                case PlayerState.NotReady:
                case PlayerState.Ready:
                case PlayerState.Idle:
                case PlayerState.Contacting:
                case PlayerState.Loading:
                case PlayerState.Loaded:
                    idlescreen.prio = LCDScreen.Prio.Foreground;
                    lcdclient.UpdScreen(idlescreen);
                    foreach(LCDScreen screen in userscreens.Keys)
                    {
                        screen.prio = LCDScreen.Prio.Hidden;
                        lcdclient.UpdScreen(screen);
                    }
                    break;
                case PlayerState.Playing:
                case PlayerState.Paused:
                    idlescreen.prio = LCDScreen.Prio.Hidden;
                    lcdclient.UpdScreen(idlescreen);
                    foreach(LCDScreen screen in userscreens.Keys)
                    {
                        screen.prio = LCDScreen.Prio.Foreground;
                        lcdclient.UpdScreen(screen);
                    }
                    break;
                }
            }
            lcdclient.UpdWidgetsAll(parser);
        }

        private void OnConnected()
        {
            Hyena.Log.Debug ("Connected to LCDproc");
            lcdclient.RegScreen(idlescreen);
            lcdclient.RegWidget(idlescreen, idletitle);
            lcdclient.RegWidget(idlescreen, idletext);
            foreach(LCDScreen screen in userscreens.Keys)
            {
                lcdclient.RegScreen(screen);
                foreach(LCDWidget widget in userscreens[screen])
                {
                    lcdclient.RegWidget(screen, widget);
                }
            }
            parser.ProcessEvent(
                new PlayerEventStateChangeArgs(ServiceManager.PlayerEngine.LastState,ServiceManager.PlayerEngine.CurrentState),
                ServiceManager.PlayerEngine.CurrentTrack,
                ServiceManager.PlayerEngine.Position,
                ServiceManager.PlayerEngine.Length);
            lcdclient.UpdWidgetsAll(parser);
        }

        private void OnConfigure (object o, EventArgs args)
        {
            Hyena.Log.Debug ("Configuring LCD service");

            ConfigurationDialog dialog = new ConfigurationDialog (this);
            dialog.Run ();
            dialog.Destroy ();
            lcdclient.Dispose();
            lcdclient = new LCDClient(Host ,Port);

            Hyena.Log.Debug ("Configured LCD service");
        }

        #region Configuration properties
        internal string Host
        {
            get { return ConfigurationSchema.Host.Get (); }
            set { ConfigurationSchema.Host.Set (value); }
        }

        internal ushort Port
        {
            get { return (ushort)ConfigurationSchema.Port.Get (); }
            set { ConfigurationSchema.Port.Set (value); }
        }
        #endregion

        string IService.ServiceName {
            get { return "LCDService"; }
        }

    }
}
