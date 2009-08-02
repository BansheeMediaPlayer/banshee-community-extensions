//
// RadioStationFetcherService.cs
//
// Author:
//   Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
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

using Banshee.ServiceStack;
using Banshee.Gui;
using Banshee.I18n;

namespace Banshee.RadioStationFetcher
{       
    public class RadioStationFetcherService : IExtensionService, IDisposable
    {
        Dictionary<string, FetcherDialog> fetcher_sources = new Dictionary<string, FetcherDialog>();
        
        private ActionGroup actions;
        private InterfaceActionService action_service;
        private uint ui_manager_id;
        
        public RadioStationFetcherService ()
        {
            Console.WriteLine ("[RadioStationFetcherService] <RadioStationFetcherService> Constructor START");
            
            Console.WriteLine ("[RadioStationFetcherService] <RadioStationFetcherService> Constructor END");
        }
        
        void IExtensionService.Initialize () 
        {
            Console.WriteLine ("[RadioStationFetcherService] <Initialize> START");       
            
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            actions = new ActionGroup ("Radio-station fetcher");
            
            ActionEntry[] source_actions = new ActionEntry[3];
            
            source_actions[0] = new ActionEntry ("RadioStationFetcherAction", null,
                Catalog.GetString ("Radiostation fetcher"), null,
                null, null);
            
            source_actions[1] = new ActionEntry ("ShoutcastAction", null,
                    Catalog.GetString ("Shoutcast"), null,
                    Catalog.GetString ("Fetch stations from shoutcast"), delegate {
                        (new Shoutcast ()).ShowDialog (); } );
            
            source_actions[2] = new ActionEntry ("XiphAction", null,
                    Catalog.GetString ("Xiph"), null,
                    Catalog.GetString ("Fetch stations from Xiph"), delegate {
                        (new Xiph ()).ShowDialog (); } );
            
            actions.Add (source_actions);
            
            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("Resources.RadioStationFetcherMenu.xml");
            
            Console.WriteLine ("[RadioStationFetcherService] <Initialize> END");
        }
        
        string IService.ServiceName {
            get { return "RadioStationFetcherService"; }
        }
        
        public void Dispose ()
        {
            Console.WriteLine ("Disposing RadioStationFetcher");
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;
        }
    }
}
