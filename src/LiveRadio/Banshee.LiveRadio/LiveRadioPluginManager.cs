
using System;
using System.Reflection;
using System.Collections.Generic;

using Banshee.Sources;
using Banshee.LiveRadio.Plugins;

namespace Banshee.LiveRadio
{

    public class LiveRadioPluginManager
    {

        private Assembly assembly;

        public LiveRadioPluginManager ()
        {
            assembly = Assembly.GetCallingAssembly();
        }

        public List<ILiveRadioPlugin> LoadPlugins()
        {
            List<ILiveRadioPlugin> plugins = new List<ILiveRadioPlugin> ();

            foreach(Type type in assembly.GetTypes())
            {
                if (type.GetInterface("ILiveRadioPlugin") != null && !type.IsAbstract)
                {
                    ILiveRadioPlugin plugin = (ILiveRadioPlugin)Activator.CreateInstance(type);
                    plugins.Add(plugin);
                }
            }
            return plugins;
        }


    }
}
