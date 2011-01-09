//
// LiveRadioPluginManager.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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
using System.Reflection;
using System.Collections.Generic;

using Banshee.LiveRadio.Plugins;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// Management class to load all plugins implemented in the assembly that implement the ILiveRadioPlugin interface
    /// </summary>
    public class LiveRadioPluginManager
    {

        private Assembly assembly;

        /// <summary>
        /// Constructor -- sets the calling assembly which contains all the plugin classes
        /// </summary>
        public LiveRadioPluginManager ()
        {
            assembly = Assembly.GetCallingAssembly ();
        }

        /// <summary>
        /// Goes through all the assembly classes to find any non-abstract class implementing ILiveRadioPlugin and adds
        /// an object instance of the class to the plugin list
        /// </summary>
        /// <returns>
        /// A <see cref="List<ILiveRadioPlugin>"/> -- a list of one instance for each plugin class
        /// </returns>
        public List<ILiveRadioPlugin> LoadPlugins ()
        {
            List<ILiveRadioPlugin> plugins = new List<ILiveRadioPlugin> ();

            foreach (Type type in assembly.GetTypes ()) {
                if (type.GetInterface ("ILiveRadioPlugin") != null && !type.IsAbstract) {
                    ILiveRadioPlugin plugin = (ILiveRadioPlugin)Activator.CreateInstance (type);
                    //shoutcast not working at the moment, don't activate
                    if (plugin.Active)
                        plugins.Add (plugin);
                }
            }
            return plugins;
        }


    }
}
