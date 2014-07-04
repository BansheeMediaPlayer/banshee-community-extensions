//
// LocationProviderManager.cs
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

namespace Banshee.SongKick.LocationProvider
{
    public static class LocationProviderManager
    {
        private static List<ICityNameObserver> cityObservers = new List<ICityNameObserver>();

        private static string city_name = String.Empty;
        public static string GetCityName {
            get { return city_name; }
        }

        private static double latitude = 0.0;
        public static double GetLatitude {
            get { return latitude; }
        }

        private static double longitude = 0.0;
        public static double GetLongitude {
            get { return longitude; }
        }

        private static BaseLocationProvider provider;
        public static BaseLocationProvider GetProvider {
            get { return provider; }
        }

        public static bool HasProvider {
            get { return provider != null; }
        }

        public static void Initialize()
        {
            Mono.Addins.AddinManager.AddExtensionNodeHandler ("/Banshee/GeoLocation/LocationProvider", OnUpdated);
        }

        private static void OnUpdated (object o, ExtensionNodeEventArgs args)
        {
            TypeExtensionNode node = (TypeExtensionNode)args.ExtensionNode;

            if (args.Change == ExtensionChange.Add) {
                provider = (BaseLocationProvider)node.CreateInstance ();
                ThreadAssist.SpawnFromMain (() => {
                    RefreshGeoPosition ();
                    NotifyObservers ();
                });
            } else {
                provider = null;
            }
        }

        public static void Register (ICityNameObserver o)
        {
            cityObservers.Add (o);
            if (HasProvider) {
                o.OnCityNameUpdated (provider.CityName);
            }
        }

        public static void Detach (ICityNameObserver o)
        {
            cityObservers.Remove (o);
        }

        public static void RefreshGeoPosition ()
        {
            city_name = provider.CityName;
            latitude  = provider.Latitude;
            longitude = provider.Longitude;
        }

        private static void NotifyObservers () 
        {
            if (HasProvider) {
                foreach (ICityNameObserver o in cityObservers)
                {
                    o.OnCityNameUpdated (city_name);
                }
            }
        }
    }
}
