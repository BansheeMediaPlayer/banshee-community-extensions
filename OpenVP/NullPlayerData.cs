// NullPlayerData.cs
//
//  Copyright (C) 2007 Chris Howie
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

namespace OpenVP {
	/// <summary>
	/// A player data source that does not connect to any media player.
	/// </summary>
	public class NullPlayerData : PlayerData, IBeatDetector {
		/// <summary>
		/// Constructor.
		/// </summary>
		public NullPlayerData() {
		}
		
		/// <summary>
		/// Zeros the arrays contained in the parameter.
		/// </summary>
		/// <param name="channels">
		/// An array of float arrays.
		/// </param>
		public override void GetPCM(float[][] channels) {
			foreach (float[] i in channels)
				if (i != null)
					Array.Clear(i, 0, i.Length);
		}
		
		/// <summary>
		/// Zeros the arrays contained in the parameter.
		/// </summary>
		/// <param name="channels">
		/// An array of float arrays.
		/// </param>
		public override void GetSpectrum(float[][] channels) {
			this.GetPCM(channels);
		}
		
		/// <value>
		/// Returns 1.
		/// </value>
		public override int NativePCMLength {
			get {
				return 1;
			}
		}
		
		/// <value>
		/// Returns 1.
		/// </value>
		public override int NativeSpectrumLength {
			get {
				return 1;
			}
		}
		
		/// <value>
		/// Returns 0.
		/// </value>
		public override float SongPosition {
			get {
				return 0;
			}
		}
		
		/// <value>
		/// Returns the empty string.
		/// </value>
		public override string SongTitle {
			get {
				return "";
			}
		}
		
		/// <summary>
		/// Returns true.
		/// </summary>
		/// <returns>
		/// True.
		/// </returns>
		/// <remarks>
		/// Loops that try to consume all possible updates will never terminate.
		/// </remarks>
		public override bool Update() {
			return true;
		}
		
		/// <summary>
		/// Does nothing.
		/// </summary>
		public override void UpdateWait() {
		}
		
		/// <value>
		/// Returns false.
		/// </value>
		public bool IsBeat {
			get {
				return false;
			}
		}
		
		void IBeatDetector.Update(Controller controller) {
		}
	}
}
