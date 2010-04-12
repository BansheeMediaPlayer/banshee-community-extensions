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
        private PlayerEngine source;

        private DataSlice dataSlice = null;

        private Queue<DataSlice> dataQueue = new Queue<DataSlice>();

        private ManualResetEvent dataAvailableEvent = new ManualResetEvent(false);

        private static readonly TimeSpan skipThreshold = TimeSpan.FromSeconds(2.0 / 60.0);

        private static readonly TimeSpan sliceStride = TimeSpan.FromSeconds(0.9 / 60.0);

        private DateTime lastSliceTime = DateTime.MinValue;

        private bool active = false;

        public bool Active
        {
            get { return active; }
            set {
                if (value == active)
                    return;

                if (value)
                    ((IVisualizationDataSource) source).DataAvailable += OnDataAvailable;
                else
                    ((IVisualizationDataSource) source).DataAvailable -= OnDataAvailable;

                active = value;
            }
        }

        private DataSlice CurrentDataSlice
        {
            get {
                if (dataSlice == null)
                    throw new InvalidOperationException("No current data slice.");

                return dataSlice;
            }
        }

        public BansheePlayerData(PlayerEngine source)
        {
            if (!(source is IVisualizationDataSource))
                throw new ArgumentException("source is not an IVisualizationDataSource");

            this.source = source;
            Active = true;
        }

        private void OnDataAvailable(float[][] data, float[][] spectrum)
        {
            // Assume there is a backlog if 5 seconds are in the queue.  This
            // is reasonable since some codec decoders give us large chunks all
            // at once.
            if (dataQueue.Count >= 60 * 5)
                return;

            // Due to large chunk issue explained above, we need to adjust the
            // timestamps a bit.  If the last slice we looked at was more than
            // 1/60 of a second ago, then we can use the current time as the
            // timestamp.  Otherwise use the last slice timestamp + 1/60 of a
            // second.  This keeps the slice-dropping algorithm from throwing
            // out most of the slices as old.
            DateTime stamp = DateTime.Now;
            DateTime expected = lastSliceTime + sliceStride;

            if (stamp < expected)
                stamp = expected;

            DataSlice slice = new DataSlice((float) source.Position / 1000,
                                            source.CurrentTrack.DisplayTrackTitle,
                                            data, spectrum, stamp);

            lastSliceTime = stamp;

            lock (dataQueue) {
                dataQueue.Enqueue(slice);
            }

            dataAvailableEvent.Set();
        }

        public void Dispose()
        {
            Active = false;
            source = null;
        }

        public override int NativePCMLength
        {
            get { return CurrentDataSlice.PCMData[0].Length; }
        }

        public override int NativeSpectrumLength
        {
            get { return CurrentDataSlice.SpectrumData[0].Length; }
        }

        public override float SongPosition
        {
            get { return CurrentDataSlice.SongPosition; }
        }

        public override string SongTitle
        {
            get { return CurrentDataSlice.SongTitle; }
        }

        public override bool Update(int timeout)
        {
            if (!dataAvailableEvent.WaitOne(timeout, false))
                return false;

            DateTime now = DateTime.Now;

            if (dataQueue.Count > 0) {
                lock (dataQueue) {
                    DataSlice slice;

                    // Check if we're behind.  If so, dequeue everything to
                    // try and catch up.
                    do {
                        slice = dataQueue.Dequeue();
                    } while (now - slice.Timestamp > skipThreshold && dataQueue.Count > 0);

                    dataSlice = slice;

                    if (dataQueue.Count == 0)
                        dataAvailableEvent.Reset();
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
            GetData(channels, CurrentDataSlice.PCMData);
        }

        public override void GetSpectrum (float[][] channels)
        {
            GetData(channels, CurrentDataSlice.SpectrumData);
        }

        private class DataSlice
        {
            public readonly float SongPosition;

            public readonly string SongTitle;

            public readonly float[][] PCMData;

            public readonly float[][] SpectrumData;

            public readonly DateTime Timestamp;

            public DataSlice(float position, string title, float[][] pcm,
                             float[][] spectrum, DateTime timestamp)
            {
                SongPosition = position;
                SongTitle = title;
                PCMData = pcm;
                SpectrumData = spectrum;

                Timestamp = timestamp;
            }
        }
    }
}
