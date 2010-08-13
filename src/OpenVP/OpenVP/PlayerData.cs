// PlayerData.cs
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

using OpenVP.Metadata;

namespace OpenVP {
	/// <summary>
	/// Base class for objects that contain data from a media player.
	/// </summary>
	public abstract class PlayerData {
		/// <summary>
		/// Constructor.
		/// </summary>
		protected PlayerData() {
		}
		
		/// <summary>
		/// Returns PCM data for a variable number of channels.
		/// </summary>
		/// <param name="channels">
		/// An array of float arrays.
		/// </param>
		/// <remarks>
		/// <para>Each array will be filled with data, if available, or zeros.
		/// The channel order is implementation-dependant.  However, the
		/// recommended implementation will fill arrays as follows:</para>
		/// <para>If only one array is passed it will be filled with the PCM
		/// data for the center channel, which may be computed using the left
		/// and right channels.</para>
		/// <para>If two arrays are passed they will be filled with the PCM data
		/// for the left and right channels, respectively.</para>
		/// <para>If more than three are passed, the output is
		/// implementation-specific.</para>
		/// <para>If any of the elements of <c>channels</c> are <c>null</c>,
		/// they should be skipped.  This can be used to selectively obtain the
		/// data for only certain channels.</para>
		/// <para>Each array must be completely filled.  If the internal data
		/// is of a different size than any of the arrays, the data should be
		/// interpolated so that it fills the entire array.</para>
		/// </remarks>
		public abstract void GetPCM(float[][] channels);
		
		/// <summary>
		/// Fills <c>center</c> with the PCM data from the center channel.
		/// </summary>
		/// <param name="center">
		/// A float array to be filled.
		/// </param>
		/// <remarks>
		/// This method calls
		/// <see cref="OpenVP.PlayerData.GetPCM(System.Single[][])"/>.
		/// </remarks>
		public void GetPCM(float[] center) {
			this.GetPCM(new float[][] { center });
		}
		
		/// <summary>
		/// Fills <c>left</c> and <c>right</c> with PCM data from the left and
		/// right channels, respectively.
		/// </summary>
		/// <param name="left">
		/// A float array to be filled.
		/// </param>
		/// <param name="right">
		/// A float array to be filled.
		/// </param>
		/// <remarks>
		/// This method calls
		/// <see cref="OpenVP.PlayerData.GetPCM(System.Single[][])"/>.
		/// </remarks>
		public void GetPCM(float[] left, float[] right) {
			this.GetPCM(new float[][] { left, right });
		}
		
		/// <summary>
		/// Returns spectrum data for a variable number of channels.
		/// </summary>
		/// <param name="channels">
		/// An array of float arrays.
		/// </param>
		/// <remarks>
		/// See the remarks for
		/// <see cref="PlayerData.GetPCM(System.Single[][])"/>.  Behavior and
		/// expectations are identical, except the arrays should be filled with
		/// data from the spectrum analyzer.
		/// </remarks>
		public abstract void GetSpectrum(float[][] channels);
		
		/// <summary>
		/// Fills <c>center</c> with the spectrum data from the center channel.
		/// </summary>
		/// <param name="center">
		/// A float array to be filled.
		/// </param>
		/// <remarks>
		/// This method calls
		/// <see cref="OpenVP.PlayerData.GetSpectrum(System.Single[][])"/>.
		/// </remarks>
		public void GetSpectrum(float[] center) {
			this.GetSpectrum(new float[][] { center });
		}
		
		/// <summary>
		/// Fills <c>left</c> and <c>right</c> with spectrum data from the left
		/// and right channels, respectively.
		/// </summary>
		/// <param name="left">
		/// A float array to be filled.
		/// </param>
		/// <param name="right">
		/// A float array to be filled.
		/// </param>
		/// <remarks>
		/// This method calls
		/// <see cref="OpenVP.PlayerData.GetSpectrum(System.Single[][])"/>.
		/// </remarks>
		public void GetSpectrum(float[] left, float[] right) {
			this.GetSpectrum(new float[][] { left, right });
		}
		/// <summary>
		/// Requests that the player data be updated.
		/// </summary>
		/// <param name="timeout">
		/// The time to wait for an update in milliseconds.  If negative, the
		/// call should block until an update is read.
		/// </param>
		/// <returns>
		/// True if the player data was updated, false otherwise.
		/// </returns>
		/// <remarks>
		/// This method should return as quickly as possible, whether updated
		/// data was fetched or not.
		/// </remarks>
		public abstract bool Update(int timeout);
		
		/// <summary>
		/// The current position of the song in fractional seconds.
		/// </summary>
		/// <value>
		/// The current position of the song in fractional seconds.
		/// </value>
		[Browsable(false)]
		public abstract float SongPosition { get; }
		
		/// <summary>
		/// The current song title.
		/// </summary>
		/// <value>
		/// The current song title.
		/// </value>
		[Browsable(false)]
		public abstract string SongTitle { get; }
		
		/// <summary>
		/// Interpolates a set of float values.
		/// </summary>
		/// <param name="original">
		/// The original data.
		/// </param>
		/// <param name="resized">
		/// An array to fill with the resized data.
		/// </param>
		public static void Interpolate(float[] original, float[] resized) {
			if (resized.Length == 0) {
				return;
			} else if (original.Length == 0) {
				Array.Clear(resized, 0, resized.Length);
			} else if (original.Length == resized.Length) {
				Array.Copy(original, resized, original.Length);
			} else /* if (resized.Length < original.Length) */ {
				// Losing information anyway, use nearest-neighbor.
				
				for (int i = 0; i < resized.Length; i++) {
					int oi = (int) ((float) i / resized.Length * original.Length);
					resized[i] = original[oi];
				}
			}
		}
		
		/// <value>
		/// The length of the internal PCM data array.
		/// </value>
		/// <remarks>
		/// This value can be used when an effect would like to render, for
		/// example, a scope.  The most efficient approach would be to take the
		/// exact PCM data since using a smaller array would be of lower quality
		/// and using a larger array would not contain any more information (and
		/// may in fact contain less due to interpolation).
		/// </remarks>
		[Browsable(false)]
		public abstract int NativePCMLength { get; }
		
		/// <value>
		/// The length of the internal spectrum data array.
		/// </value>
		/// <remarks>
		/// This value can be used when an effect would like to render, for
		/// example, a scope.  The most efficient approach would be to take the
		/// exact spectrum data since using a smaller array would be of lower
		/// quality and using a larger array would not contain any more
		/// information (and may in fact contain less due to interpolation).
		/// </remarks>
		[Browsable(false)]
		public abstract int NativeSpectrumLength { get; }
	}
}
