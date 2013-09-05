//
// IDictionaryUtils.cs
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
using System.Collections.Generic;

namespace Utils
{
    public static class Utils
    {
        public static bool TryGetValueAndLog<K, V> (this IDictionary<K, V> dict, K key, out V value, string logMsg)
        {
            bool succeded = dict.TryGetValue (key, out value);

            if (!succeded) 
            {
                Hyena.Log.Warning(String.Format("TryGetValueAndLog failed to assign value. Message: {0}",logMsg));
            }

            return succeded;               
        }

        public static V GetValueAndLogOnfFailure<K, V> (this IDictionary<K, V> dict, K key, string logMsg)
        {
            try {
                V value = dict[key];
                return value;
            }
            catch (KeyNotFoundException) {
                Hyena.Log.Warning(String.Format("GetValueAndLogOnFailure failed to get value. Message: {0}", logMsg));
                return default(V);
            }                  
        }

        public static T Get<T> (this IDictionary<string, object> item, string key)
        {
            return item.Get <T> (key, default(T));
        }

        // based on InternetArchive.JsonExtensions:
        public static T Get<T> (this IDictionary<string, object> item, string key, T defaultValue)
        {
            if (item == null)
                return defaultValue;

            object result;
            if (item.TryGetValue (key, out result)) {
                try {
                    if (result is T) {
                        result = (T)result;
                    } else if (result is string) {
                        var type = typeof(T);
                        string i = result as string;
                        if (type == typeof(Int32)) {
                            result = Int32.Parse (i);
                        } else if (type == typeof(Int64)) {
                            result = Int64.Parse (i);
                        } else if (type == typeof(double)) {
                            result = Double.Parse (i);
                        } else if (type == typeof(TimeSpan)) {
                            int h = 0, m = 0, s = 0;
                            var bits = i.Split (':');

                            if (bits.Length > 0)
                                s = Int32.Parse (bits[bits.Length - 1]);

                            if (bits.Length > 1)
                                m = Int32.Parse (bits[bits.Length - 2]);

                            if (bits.Length > 2)
                                h = Int32.Parse (bits[bits.Length - 3]);

                            result = new TimeSpan (h, m, s);
                        }
                    } else {
                        result = defaultValue;
                    }

                    return (T)result;
                } catch {
                    Hyena.Log.Warning (String.Format("SongKick: Couldn't cast {0} ({1}) as {2} for key {3}", result, result.GetType (), typeof(T), key));
                }
            }

            return defaultValue;
        }
    }
}

