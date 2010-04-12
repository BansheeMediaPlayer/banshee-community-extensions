// NullPlayerData.cs
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
		/// Does nothing.
		/// </summary>
		/// <returns>
		/// True if timeout is nonzero.
		/// </returns>
		/// <remarks>
		/// True is returned if timeout is nonzero so that applications that are
		/// blocking for an update will be released as soon as possible.  False
		/// is returned if timeout is zero so that applications trying to
		/// consume all available updates before entering a render loop will not
		/// loop infinitely.
		/// </remarks>
		public override bool Update(int timeout) {
			return timeout != 0;
		}
		
		/// <value>
		/// Returns false.
		/// </value>
		public bool IsBeat {
			get {
				return false;
			}
		}
		
		void IBeatDetector.Update(IController controller) {
		}
	}
}
