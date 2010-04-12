// UDPPlayerData.cs
//
//  Copyright (C) 2007-2008 Chris Howie
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

using OpenVP.Metadata;

namespace OpenVP {
	/// <summary>
	/// Reads player data from a listening UDP socket.
	/// </summary>
	public class UDPPlayerData : PlayerData {
		private enum MessageType : int {
			PositionUpdate = 1,
			TitleUpdate = 2,
			
			PCMUpdate = 3,
			SpectrumUpdate = 4,
			
			SliceComplete = 5,
		}
		
		private UdpClient mClient;
		
		private int mPort;
		
		/// <value>
		/// The port number to listen on.
		/// </value>
		[Browsable(true), Category("Connection"), DefaultValue(40507),
		 Description("The UDP port to listen on for incoming data.")]
		public int Port {
			get {
				return this.mPort;
			}
			set {
				if (value < 1 || value > 65535)
					throw new ArgumentOutOfRangeException("Port numbers range from 1-65535.");
				
				this.mPort = value;
				
				this.RecreateClient();
			}
		}
		
		/// <summary>
		/// Creates a new UDP-based player data source.
		/// </summary>
		/// <remarks>
		/// This constructor uses the default port, 40507.
		/// </remarks>
		public UDPPlayerData() : this(40507) {
		}
		
		/// <summary>
		/// Creates a new UDP-based player data source.
		/// </summary>
		/// <param name="port">
		/// The port number to listen on.
		/// </param>
		public UDPPlayerData(int port) {
			this.mPort = port;
			
			this.RecreateClient();
		}
		
		private void RecreateClient() {
			if (this.mClient != null)
				this.mClient.Close();
			
			this.mClient = null;
			
			try {
				this.mClient = new UdpClient(this.mPort,
				                             AddressFamily.InterNetworkV6);
			} catch {
				this.mClient = new UdpClient(this.mPort,
				                             AddressFamily.InterNetwork);
			}
		}
		
		private float mSongPosition = 0;
		
		/// <value>
		/// The current song's position in fractional seconds.
		/// </value>
		public override float SongPosition {
			get {
				return this.mSongPosition;
			}
		}
		
		private string mSongTitle = null;
		
		/// <value>
		/// The current song's title.
		/// </value>
		public override string SongTitle {
			get {
				return this.mSongTitle;
			}
		}
		
		private static float[] mZeroArray = new float[] { 0 };
		
		private bool mDataInitialized = false;
		
		private float[][] mPCMData = new float[][] { mZeroArray, mZeroArray };
		
		private float[][] mSpecData = new float[][] { mZeroArray, mZeroArray };
		
		private Queue<byte[]> mLazyUpdateQueue = new Queue<byte[]>();
		
		private bool GetMessageType(byte[] data, out MessageType type) {
			if (data.Length < 4) {
				type = (MessageType) 0;
				
				return false;
			}
			
			type = (MessageType) BitConverter.ToInt32(data, 0);
			
			return true;
		}
		
		private void ProcessUpdate(byte[] data) {
			MessageType type;
			
			if (!this.GetMessageType(data, out type))
				return;
			
			switch (type) {
			case MessageType.PositionUpdate:
				if (data.Length < 8)
					break;
				
				this.mSongPosition = BitConverter.ToSingle(data, 4);
				break;
				
			case MessageType.TitleUpdate:
				this.mSongTitle = Encoding.ASCII.GetString(data, 4,
				                                           data.Length - 4);
				break;
				
			case MessageType.PCMUpdate:
				this.FillData(ref this.mPCMData, data);
				break;
				
			case MessageType.SpectrumUpdate:
				this.FillData(ref this.mSpecData, data);
				break;
			}
		}
		
		private void FillData(ref float[][] data, byte[] message) {
			// Divide by 2 since we have a left and right channel, and divide by
			// 4 since we are taking floats.
			int bytelength = (message.Length - 4) / 2;
			int length = bytelength / 4;
			
			if (!this.mDataInitialized || data[0].Length != length) {
				data = new float[][] { new float[length], new float[length] };
				this.mDataInitialized = true;
			}
			
			Buffer.BlockCopy(message, 4,              data[0], 0, bytelength);
			Buffer.BlockCopy(message, 4 + bytelength, data[1], 0, bytelength);
		}
		
		private void ProcessLazyUpdateQueue() {
			while (this.mLazyUpdateQueue.Count != 0)
				this.ProcessUpdate(this.mLazyUpdateQueue.Dequeue());
		}
		
		/// <summary>
		/// Updates the player data, blocking until successful.
		/// </summary>
		public override bool Update(int timeout) {
			byte[] data;
			
			MessageType type;
			
			List<Socket> socket = new List<Socket>();
			
			for (;;) {
				IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
				
				socket.Add(this.mClient.Client);
				Socket.Select(socket, null, null, timeout * 1000);
				
				if (!socket.Contains(this.mClient.Client))
					break;
				
				data = this.mClient.Receive(ref remote);
				
				if (this.GetMessageType(data, out type)) {
					if (type == MessageType.SliceComplete) {
						this.ProcessLazyUpdateQueue();
						return true;
					}
					
					this.mLazyUpdateQueue.Enqueue(data);
				}
				
				socket.Clear();
			}
			
			return false;
		}
		
		private void GetCenter(float[][] data, float[] output) {
			if (output == null)
				return;
			
			float[] center = new float[data[0].Length];
			
			for (int i = 0; i < center.Length; i++)
				center[i] = data[0][i] / 2 + data[1][i] / 2;
			
			PlayerData.Interpolate(center, output);
		}
		
		private void GetChannels(float[][] data, float[][] channels) {
			if (channels.Length == 0)
				return;
			
			if (channels.Length == 1) {
				this.GetCenter(data, channels[0]);
				return;
			}
			
			if (channels[0] != null)
				PlayerData.Interpolate(data[0], channels[0]);
			
			if (channels[1] != null)
				PlayerData.Interpolate(data[1], channels[1]);
			
			for (int i = 2; i < channels.Length; i++)
				if (channels[i] != null)
					Array.Clear(channels[i], 0, channels[i].Length);
		}
		
		/// <summary>
		/// Returns PCM data.
		/// </summary>
		/// <param name="channels">
		/// An array of arrays to fill.
		/// </param>
		public override void GetPCM(float[][] channels) {
			this.GetChannels(this.mPCMData, channels);
		}
		
		/// <summary>
		/// Returns spectrum analyzer data.
		/// </summary>
		/// <param name="channels">
		/// An array of arrays to fill.
		/// </param>
		public override void GetSpectrum(float[][] channels) {
			this.GetChannels(this.mSpecData, channels);
		}
		
		/// <value>
		/// The length of the internal PCM data array.
		/// </value>
		public override int NativePCMLength {
			get {
				return this.mPCMData[0].Length;
			}
		}
		
		/// <value>
		/// The length of the internal spectrum data array.
		/// </value>
		public override int NativeSpectrumLength {
			get {
				return this.mSpecData[0].Length;
			}
		}
	}
}
