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

    public class LiveRadioPluginManager
    {

        private Assembly assembly;

        public LiveRadioPluginManager ()
        {
            assembly = Assembly.GetCallingAssembly ();
        }

        public List<ILiveRadioPlugin> LoadPlugins ()
        {
            List<ILiveRadioPlugin> plugins = new List<ILiveRadioPlugin> ();
            
            foreach (Type type in assembly.GetTypes ()) {
                if (type.GetInterface ("ILiveRadioPlugin") != null && !type.IsAbstract) {
                    ILiveRadioPlugin plugin = (ILiveRadioPlugin)Activator.CreateInstance (type);
                    plugins.Add (plugin);
                }
            }
            return plugins;
        }
        
        
    }
}
