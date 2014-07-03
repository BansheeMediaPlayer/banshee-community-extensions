//
// CityProviderManager.cs
//
// Author:
//   Dmitrii Petukhov <dimart.sp@gmail.com>
//
// Copyright (c) 2014 Dmitrii Petukhov
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

        private static string city_name = String.Empty;
        public static string GetCityName {
            get { return city_name; }
        }

        private static int latitude  = 0;
        public static int GetLatitude {
            get { return latitude; }
        }

        private static int longitude = 0;
        public static int GetLongitude {
            get { return longitude; }
        }

        private static BaseCityProvider provider;
        public static BaseCityProvider GetProvider {
            get { return provider; }
        }

        public static bool HasProvider {
            get { return provider != null; }
        }

        public static void Initialize()
        {
            Mono.Addins.AddinManager.AddExtensionNodeHandler ("/Banshee/GeoLocation/CityProvider", OnUpdated);
        }

        private static void OnUpdated (object o, ExtensionNodeEventArgs args)
        {
            TypeExtensionNode node = (TypeExtensionNode)args.ExtensionNode;

            if (args.Change == ExtensionChange.Add) {
                provider = (BaseCityProvider)node.CreateInstance ();
                ThreadAssist.SpawnFromMain (() => {
                    city_name = provider.CityName;
                    latitude  = provider.Latitude;
                    longitude = provider.Longitude;
                    NotifyObservers ();
                });
            } else {
                provider = null;
            }
        }

        public static void Register (ICityObserver o)
        {
            cityObservers.Add (o);
            if (HasProvider) {
                o.UpdateCity (provider.CityName);
            }
        }

        public static void Detach (ICityObserver o)
        {
            cityObservers.Remove (o);
        }

        private static void NotifyObservers () 
        {
            if (HasProvider) {
                foreach (ICityObserver o in cityObservers)
                {
                    o.UpdateCity (city_name);
                }
            }
        }
    }
}
