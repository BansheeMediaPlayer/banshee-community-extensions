//
// Location.cs
//
// Author:
//   Tomasz Maczyński <tmtimon@gmail.com>
//
// Copyright 2013 Tomasz Maczyński
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
using Hyena.Json;

namespace Banshee.SongKick.Recommendations
{
    public class Location : IResult
    {
        public long Id { get; private set; }
        [DisplayAttribute ("City", DisplayAttribute.DisplayType.Text)]
        public string CityName { get; private set; }
        [DisplayAttribute ("Country", DisplayAttribute.DisplayType.Text)]
        public string CityCountryName { get; private set; }
        [DisplayAttribute ("State", DisplayAttribute.DisplayType.Text)]
        public string CityStateName { get; private set; }

        public string Uri { get; private set; }

        /*
            Example Location: 
            {
               "city":{
                  "displayName":"London",
                  "country":{
                     "displayName":"UK"
                  },
                  "lng":-0.128,
                  "lat":51.5078
               },
               "metroArea":{
                  "uri":"http://www.songkick.com/metro_areas/24426-uk-london",
                  "displayName":"London",
                  "country":{
                     "displayName":"UK"
                  },
                  "id":24426,
                  "lng":-0.128,
                  "lat":51.5078
               }
            }
        */
        public Location (JsonObject jsonObject)
        {
            var city = jsonObject.Get<JsonObject> ("city");
            var metroArea = jsonObject.Get<JsonObject> ("metroArea");

            Id = metroArea.Get <int> ("id");
            Uri = metroArea.Get <String> ("uri");

            CityName = city.Get <String> ("displayName");
            CityCountryName = city.GetObjectDisplayName ("country");
            CityStateName = city.GetObjectDisplayName ("state");
        }

        public override string ToString ()
        {
            return string.Format ("[Location: Id={0}, CityName={1}, CityCountryName={2}, CityStateName={3}, Uri={4}]", 
                                  Id, CityName, CityCountryName, CityStateName, Uri);
        }
    }
}

