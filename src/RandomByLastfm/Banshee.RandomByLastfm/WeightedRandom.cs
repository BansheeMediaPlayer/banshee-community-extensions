//
// WeightedRandom.cs
// 
// Author:
//   raimo <${AuthorEmail}>
// 
// Copyright (c) 2010 raimo
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

        public void Add (T aNewKey, double aWeighting)
        {
            if (weighting.ContainsKey (aNewKey))
                return;
            weighting.Add (aNewKey, aWeighting);
        }

        public void Add (T aNewKey, double aWeighting, T aParent)
        {
            if (weighting.ContainsKey (aParent)) {
                aWeighting *= Value (aParent);
            }
            Add (aNewKey, aWeighting);
        }

        public T GetRandom ()
        {
            double rand = random.NextDouble () * Total;
            double cPoint = 0d;
            
            foreach (T cObj in weighting.Keys) {
                cPoint += weighting[cObj];
                if (cPoint >= rand)
                    return cObj;
            }
            return default(T);
        }

        public int Count {
            get { return weighting.Count; }
        }

        public T GetInvertedRandom ()
        {
            double total = InvertedTotal;
            double rand = random.NextDouble () * total;
            double cPoint = 0d;

            foreach (T cObj in weighting.Keys) {
                cPoint += (1d/weighting[cObj]);
                if (cPoint >= rand)
                    return cObj;
            }
            return default(T);
            
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

        public double RelativeValue (T aKey)
        {
            if (!weighting.ContainsKey (aKey))
                return 0d;
            return Value (aKey) / Total;
        }

        public double Value (T aKey)
        {
            if (!weighting.ContainsKey (aKey))
                return 0d;
            return weighting[aKey];
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

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder ();
            sb.AppendLine (String.Format ("Total Weight: {0:0.000} (Inv: {1:0.000})", Total, InvertedTotal));
            sb.AppendLine (String.Format ("Object Count: {0}", Count));
            foreach (T cKey in weighting.Keys) {
                sb.AppendLine (String.Format (" {0}: {1:0.000} (Inv: {2:0.000}, Rel: {3:0.000})", cKey.ToString (), Value (cKey), 1d/Value(cKey), RelativeValue (cKey)));
            }
            return sb.ToString ();
        }
    }
}

