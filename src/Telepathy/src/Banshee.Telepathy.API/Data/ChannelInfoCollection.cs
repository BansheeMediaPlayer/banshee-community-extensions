//
// ChannelInfoColletion.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
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

using Telepathy;

namespace Banshee.Telepathy.API.Data {

    public class ChannelInfoCollection : List <ChannelInfo>
    {
        public ChannelInfoCollection ()
        {
        }

        public IEnumerable <T> GetAll <T> () where T : ChannelInfo
        {
            foreach (ChannelInfo channel in this) {
                if (channel as T != null) {
                    yield return (T) channel;
                }
            }
        }

        public T GetChannelInfo <T> (string service_name) where T : IServiceProvidingChannel
        {
            foreach (IServiceProvidingChannel channel in this) {
                if (channel != null && channel.Service.Equals (service_name)) {
                    return (T) channel;
                }
            }

            return default (T);
        }
    }
}