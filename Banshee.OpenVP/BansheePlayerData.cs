// 
// BansheePlayerData.cs
//  
// Author:
//       Chris Howie <cdhowie@gmail.com>
// 
// Copyright (c) 2009 Chris Howie
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
using System.Threading;

using OpenVP;

using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.OpenVP
{
    public class BansheePlayerData : PlayerData, IDisposable
    {
        private PlayerEngine mSource;
        
        private DataSlice mData = null;
        
        private Queue<DataSlice> mDataQueue = new Queue<DataSlice>();
        
        private ManualResetEvent mDataAvailableEvent = new ManualResetEvent(false);
        
        private static readonly TimeSpan SkipThreshold = TimeSpan.FromSeconds(2.0 / 60.0);

        private static readonly TimeSpan SliceStride = TimeSpan.FromSeconds(0.9 / 60.0);

        private DateTime mLastSlice = DateTime.MinValue;

        private bool active = false;

        public bool Active {
            get { return this.active; }
            set {
                if (value == this.active)
                    return;

                if (value)
                    ((IVisualizationDataSource) this.mSource).DataAvailable += this.OnDataAvailable;
                else
                    ((IVisualizationDataSource) this.mSource).DataAvailable -= this.OnDataAvailable;

                this.active = value;
            }
        }
        
        private DataSlice CurrentDataSlice {
            get {
                if (this.mData == null)
                    throw new InvalidOperationException("No current data slice.");
                
                return this.mData;
            }
        }
        
        public BansheePlayerData(PlayerEngine source) {
            if (!(source is IVisualizationDataSource))
                throw new ArgumentException("source is not an IVisualizationDataSource");
            
            this.mSource = source;
            this.Active = true;
        }
        
        private void OnDataAvailable(float[][] data, float[][] spectrum) {
            // Assume there is a backlog if 5 seconds are in the queue.  This
            // is reasonable since some codec decoders give us large chunks all
            // at once.
            if (this.mDataQueue.Count >= 60 * 5)
                return;

            // Due to large chunk issue explained above, we need to adjust the
            // timestamps a bit.  If the last slice we looked at was more than
            // 1/60 of a second ago, then we can use the current time as the
            // timestamp.  Otherwise use the last slice timestamp + 1/60 of a
            // second.  This keeps the slice-dropping algorithm from throwing
            // out most of the slices as old.
            DateTime stamp = DateTime.Now;
            DateTime expected = this.mLastSlice + SliceStride;
            
            if (stamp < expected)
                stamp = expected;
            
            DataSlice slice = new DataSlice((float) this.mSource.Position / 1000,
                                            this.mSource.CurrentTrack.DisplayTrackTitle,
                                            data, spectrum, stamp);

            this.mLastSlice = stamp;
            
            lock (this.mDataQueue) {
                this.mDataQueue.Enqueue(slice);
            }
            
            this.mDataAvailableEvent.Set();
        }
        
        public void Dispose() {
            this.Active = false;
            this.mSource = null;
        }
        
        public override int NativePCMLength {
            get { return this.CurrentDataSlice.PCMData[0].Length; }
        }
        
        public override int NativeSpectrumLength {
            get { return this.CurrentDataSlice.SpectrumData[0].Length; }
        }
        
        public override float SongPosition {
            get { return this.CurrentDataSlice.SongPosition; }
        }
        
        public override string SongTitle {
            get { return this.CurrentDataSlice.SongTitle; }
        }
        
        public override bool Update(int timeout) {
            if (!this.mDataAvailableEvent.WaitOne(timeout, false))
                return false;
            
            DateTime now = DateTime.Now;
            
            if (this.mDataQueue.Count > 0) {
                lock (this.mDataQueue) {
                    DataSlice slice;
                    
                    // Check if we're behind.  If so, dequeue everything to
                    // try and catch up.
                    do {
                        slice = this.mDataQueue.Dequeue();
                    } while (now - slice.Timestamp > SkipThreshold && this.mDataQueue.Count > 0);
                    
                    this.mData = slice;
                    
                    if (this.mDataQueue.Count == 0)
                        this.mDataAvailableEvent.Reset();
                }
                
                return true;
            }
            
            return false;
        }

        private static void Downmix (float[] center, float[] left, float[] right)
        {
            float[] target;
            
            if (center.Length == left.Length)
                target = center;
            else
                target = new float[left.Length];
            
            for (int i = 0; i < center.Length; i++)
                target[i] = (left[i] + right[i]) / 2;

            if (center.Length != left.Length)
                Interpolate(target, center);
        }

        private static void GetData (float[][] output, float[][] input)
        {
            // Assume this is a request for downmix of stereo.
            if (output.Length == 1 && input.Length >= 2) {
                Downmix(output[0], input[0], input[1]);
                return;
            }

            // Duplicate data if they request stereo data but we don't have it.
            if (output.Length == 2 && input.Length == 1) {
                Interpolate(input[0], output[0]);
                Interpolate(input[0], output[1]);
                return;
            }
            
            int count = Math.Min(output.Length, input.Length);
            
            for (int i = 0; i < count; i++) {
                Interpolate(input[i], output[i]);
            }
            
            for (int i = count; i < output.Length; i++) {
                Array.Clear(output[i], 0, output[i].Length);
            }
        }
        
        public override void GetPCM (float[][] channels)
        {
            GetData(channels, this.CurrentDataSlice.PCMData);
        }
        
        public override void GetSpectrum (float[][] channels)
        {
            GetData(channels, this.CurrentDataSlice.SpectrumData);
        }
        
        private class DataSlice {
            public readonly float SongPosition;
            
            public readonly string SongTitle;
            
            public readonly float[][] PCMData;
            
            public readonly float[][] SpectrumData;
            
            public readonly DateTime Timestamp;
            
            public DataSlice(float position, string title, float[][] pcm,
                             float[][] spectrum, DateTime timestamp) {
                this.SongPosition = position;
                this.SongTitle = title;
                this.PCMData = pcm;
                this.SpectrumData = spectrum;
                
                this.Timestamp = timestamp;
            }
        }
    }
}
