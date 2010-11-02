//
// WeightedRandom.cs
//
// Author:
//   Raimo Radczewski <raimoradczewski@googlemail.com>
//
// Copyright (c) 2010 Raimo Radczewski
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
using System.Text;
namespace Banshee.RandomByLastfm
{
    public class WeightedRandom<T>
    {
        private Dictionary<T, double> weighting;
        private Random random;

        public WeightedRandom ()
        {
            weighting = new Dictionary<T, double> ();
            random = new Random (Convert.ToInt32 (DateTime.Now.Ticks % Int32.MaxValue));
        }

        public int Count {
            get { return weighting.Count; }
        }

        public double Total {
            get {
                double tmp = 0d;
                foreach (double cVal in weighting.Values) {
                    tmp += cVal;
                }
                return tmp;
            }
        }

        public double InvertedTotal {
            get {
                double tmp = 0d;
                foreach (double cVal in weighting.Values) {
                    tmp += 1d / cVal;
                }
                return tmp;
            }
        }

        public double Value (T aKey)
        {
            if (!Contains (aKey))
                return 0d;
            return weighting[aKey];
        }

        public double RelativeValue (T aKey)
        {
            if (!Contains (aKey))
                return 0d;
            return Value (aKey) / Total;
        }

        public double InvertedValue (T aKey)
        {
            if (!Contains (aKey))
                return 0d;
            return 1d / Value (aKey);
        }

        public double InvertedRelativeValue (T aKey)
        {
            if (!Contains (aKey))
                return 0d;
            return InvertedValue (aKey) / InvertedTotal;
        }

        public T GetRandom ()
        {
            double rand = random.NextDouble () * Total;
            double cPoint = 0d;

            foreach (T cObj in weighting.Keys) {
                cPoint += Value (cObj);
                if (cPoint >= rand)
                    return cObj;
            }
            return default(T);
        }

        public T GetInvertedRandom ()
        {
            double rand = random.NextDouble () * InvertedTotal;
            double cPoint = 0d;

            foreach (T cObj in weighting.Keys) {
                cPoint += InvertedValue (cObj);
                if (cPoint >= rand)
                    return cObj;
            }
            return default (T);
        }

        public void Add (T aNewKey, double aWeighting)
        {
            Add (aNewKey, aWeighting, default(T));
        }

        public void Add (T aNewKey, double aWeighting, T aParent)
        {
            if (Contains (aNewKey))
                return;
            
            if (weighting.ContainsKey (aParent)) {
                // Relative Value of Parent multiplied for generation weighting
                // Needs some tweaking for RandomByLastfmSimilarArtists
                aWeighting *= RelativeValue (aParent);
            }
            weighting.Add (aNewKey, aWeighting);
        }

        public void AddRange (Dictionary<T, double> aNewWeightings)
        {
            AddRange (aNewWeightings, default(T));
        }

        public void AddRange (Dictionary<T, double> aNewWeightings, T aParent)
        {
            foreach (T newKey in aNewWeightings.Keys) {
                Add (newKey, aNewWeightings[newKey], aParent);
            }
        }

        public void Remove (T aKey)
        {
            if (!Contains (aKey))
                return;
            weighting.Remove (aKey);
        }

        public bool Contains (T aKey)
        {
            return weighting.ContainsKey (aKey);
        }

        public void Clear ()
        {
            weighting.Clear ();
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder ();
            sb.AppendLine (String.Format ("Total Weight: {0:0.000} (Inv: {1:0.000})", Total, InvertedTotal));
            sb.AppendLine (String.Format ("Object Count: {0}", Count));
            foreach (T cKey in weighting.Keys) {
                sb.AppendLine (String.Format (" {0}: {1:0.000} (Inv: {2:0.000}, Rel: {3:0.000})", cKey.ToString (), Value (cKey), InvertedValue (cKey), RelativeValue (cKey)));
            }
            return sb.ToString ();
        }
    }
}

