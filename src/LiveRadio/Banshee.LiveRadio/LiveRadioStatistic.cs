//
// LiveRadioStatistic.cs
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
using Mono.Addins;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// A simple statistics object that implements a counter and calculates average and counts number of updates
    /// </summary>
    public class LiveRadioStatistic
    {
        private int count = 0;
        private string short_description;
        private string origin;
        private string long_description;
        private int counter = 0;
        private double average = 0;
        private LiveRadioStatisticType type;

        public LiveRadioStatistic (LiveRadioStatisticType type,
                                   string origin,
                                   string short_description,
                                   string long_description)
        {
            this.type = type;
            this.origin = origin;
            this.short_description = short_description;
            this.long_description = long_description;
        }

        public void AddCount(int count)
        {
            this.count += count;
            this.average = ((counter * average) + count) / (counter + 1);
            this.counter++;
        }

        public void Reset()
        {
            count = 0;
            counter = 0;
            average = 0;
        }

        public int Count
        {
            get { return count; }
        }

        public string ShortDescription
        {
            get { return short_description; }
        }

        public string LongDescription
        {
            get { return long_description; }
        }

        public double Average
        {
            get { return average; }
        }

        public int Updates
        {
            get { return counter; }
        }

        public string Origin
        {
            get { return origin; }
        }

        public LiveRadioStatisticType Type
        {
            get { return type; }
        }

        public string TypeName
        {
            get { return AddinManager.CurrentLocalizer.GetString (type.ToString ()); }
        }

    }

    public enum LiveRadioStatisticType {
        Message,
        Error
    }
}
