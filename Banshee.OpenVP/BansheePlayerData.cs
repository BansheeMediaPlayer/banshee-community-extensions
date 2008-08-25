// BansheePlayerData.cs
//
//  Copyright (C) 2008 Chris Howie
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

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
		
		private static readonly TimeSpan SkipThreshold = TimeSpan.FromSeconds(6 / 60);
		
		private DataSlice CurrentDataSlice {
			get {
				if (this.mData == null)
					throw new InvalidOperationException("No current data slice.");
				
				return this.mData;
			}
		}
		
		public BansheePlayerData(PlayerEngine source) {
			IVisualizationDataSource dsource = source as IVisualizationDataSource;
			
			if (dsource == null)
				throw new ArgumentException("source is not an IVisualizationDataSource");
			
			this.mSource = source;
			
			dsource.DataAvailable += this.OnDataAvailable;
		}
		
		private void OnDataAvailable(float[][] data, float[][] spectrum) {
			// Assume there is a backlog if over 10 slices are in the queue.
			// Let the visualizer catch up.
			if (this.mDataQueue.Count >= 10)
				return;
			
			DataSlice slice = new DataSlice((float) this.mSource.Position / 1000,
			                                this.mSource.CurrentTrack.DisplayTrackTitle,
			                                data,
			                                spectrum);
			
			lock (this.mDataQueue) {
				this.mDataQueue.Enqueue(slice);
			}
			
			this.mDataAvailableEvent.Set();
		}
        
        public void Dispose() {
            ((IVisualizationDataSource) this.mSource).DataAvailable -= this.OnDataAvailable;
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
					DataSlice slice = this.mDataQueue.Dequeue();
					
					// Check if we're behind.  If so, dequeue everything to
					// try and catch up.
					if (now - slice.Timestamp > SkipThreshold) {
						while (this.mDataQueue.Count > 1)
							slice = this.mDataQueue.Dequeue();
					}
					
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
				center[i] = (left[i] + right[i]) / 2;

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
			
			public DataSlice(float position, string title, float[][] pcm, float[][] spectrum) {
				this.SongPosition = position;
				this.SongTitle = title;
				this.PCMData = pcm;
				this.SpectrumData = spectrum;
				
				this.Timestamp = DateTime.Now;
			}
		}
	}
}
