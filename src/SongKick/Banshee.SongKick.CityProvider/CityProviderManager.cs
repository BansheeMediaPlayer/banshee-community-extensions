//
// CityProviderManager.cs
//
// Author:
//   dmitrii <>
//
// Copyright (c) 2014 dmitrii
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using Mono.Addins;

using Hyena;

namespace Banshee.SongKick.CityProvider
{
    public static class CityProviderManager
    {
        private static List<ICityObserver> cityObservers = new List<ICityObserver>();

        private static bool hasProvider;
        public static bool HasProvider {
            get { return hasProvider; }
        }

        private static BaseCityProvider provider;
        public static BaseCityProvider CityProvider {
            get { return provider; }
        }

        public static void Initialize()
        {
            Mono.Addins.AddinManager.AddExtensionNodeHandler ("/Banshee/GeoLocation/CityProvider", OnUpdated);
        }

        public static void OnUpdated (object o, ExtensionNodeEventArgs args)
        {
            TypeExtensionNode node = (TypeExtensionNode)args.ExtensionNode;

            if (args.Change == ExtensionChange.Add) {
                provider = (BaseCityProvider)node.CreateInstance ();
                hasProvider = true;
                ThreadAssist.SpawnFromMain (() => {
                    provider.GetData();
                    NotifyObservers ();
                });
            } else {
                provider = null;
                hasProvider = false;
                cityObservers.ForEach ((x) => x.UpdateCity ("Type your query"));
            }
        }

        public static void RegisterMe (ICityObserver o)
        {
            cityObservers.Add (o);
            NotifyObservers ();
        }

        public static void DetachMe (ICityObserver o)
        {
            cityObservers.Remove (o);
        }

        private static void NotifyObservers () 
        {
            foreach (ICityObserver o in cityObservers)
            {
                o.UpdateCity (provider.CityName);
            }
        }
    }
}
