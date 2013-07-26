//
// MusicEvent.cs
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
using System.Collections.Generic;
using System.Collections;

namespace Banshee.SongKick.Recommendations
{
    public class Event : IResult
    {
        //adding attribute below is useful during debugging:
        //[DisplayAttribute("ID", DisplayAttribute.DisplayType.Text)]
        public long Id { get; private set; }
        [DisplayAttribute("Name", DisplayAttribute.DisplayType.Text)]
        public string DisplayName { get; private set; }
        [DisplayAttribute("Date", DisplayAttribute.DisplayType.Text)]
        public DateTime StartDateTime { get; private set; }
        [DisplayAttribute("Location", DisplayAttribute.DisplayType.Text)]
        public EventLocation Location { get; private set; }
        [DisplayAttribute("Type", DisplayAttribute.DisplayType.Text)]
        public string Type { get; private set; }
        [DefaultSortColumn]
        [DisplayAttribute("Popularity", DisplayAttribute.DisplayType.Text)]
        public double Popularity { get; private set; }
        public string Uri { get; private set; }


        public Event (JsonObject jsonObject)
        {
            Id = jsonObject.Get <int> ("id");
            DisplayName = jsonObject.Get <String> ("displayName");
            Type = jsonObject.Get <String> ("type");
            Popularity = jsonObject.Get <Double> ("popularity");
            Uri = jsonObject.Get<String> ("uri");
            Location = new EventLocation (jsonObject.Get<JsonObject> ("location"));

            var start = jsonObject.Get<JsonObject> ("start");
            StartDateTime = getDateTime (
                start.Get <String> ("date"),
                start.Get <String> ("time"));
        }

        private DateTime getDateTime (String dateString, string timeString)
        {
            // TODO: add timezone
            // this can be achieved by parsing "datetime" (e.g. "2012-04-18T20:00:00-0800")
            DateTime date;
            DateTime.TryParse (String.Format("{0} {1}", dateString, timeString), out date);
            return date;
        }

        public class EventLocation : IComparable, IComparable<EventLocation> , IComparer<EventLocation>, IComparer
        {
            public String Name { get; private set; }
            public double Latitude { get; private set; }
            public double Longitude { get; private set; }

            public EventLocation (JsonObject jsonLocation) 
            {
                // example:
                // {"city":"San Francisco, CA, US","lng":-122.4332937,"lat":37.7842398}
                Name = jsonLocation.Get<String> ("city");
                Latitude= jsonLocation.Get<double> ("lat");
                Longitude= jsonLocation.Get<double> ("lng");
            }

            public override string ToString ()
            {
                return Name;
            }

            #region IComparable implementation

            int IComparable<EventLocation>.CompareTo (EventLocation other)
            {
                return this.Name.CompareTo(other.Name);
            }

            int IComparable.CompareTo(object y)
            {
                if (y is EventLocation) {
                    return (this as IComparable<EventLocation>).CompareTo(y as EventLocation);
                } else {
                    throw new InvalidOperationException("EventLocation can be Compared only to EventLocation");
                }
            }

            #endregion

            #region IComparer implementation

            int IComparer<EventLocation>.Compare (EventLocation a, EventLocation b)
            {
                return a.Name.CompareTo (b.Name);
            }

            int IComparer.Compare (object x, object y)
            {
                if ((x is EventLocation) && (y is EventLocation)) {
                    return (this as IComparer<EventLocation>).Compare(x as EventLocation, y as EventLocation);
                } else {
                    throw new InvalidOperationException("EventLocation can be Compared only to EventLocation");
                }
            }

            #endregion
        }
    }
}

