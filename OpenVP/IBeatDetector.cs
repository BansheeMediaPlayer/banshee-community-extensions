// IBeatDetector.cs
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
	/// Implemented by classes that can detect beats in music.
	/// </summary>
	public interface IBeatDetector {
		/// <value>
		/// True if there was a beat on the last update.
		/// </value>
		bool IsBeat { get; }
		
		/// <summary>
		/// Requests that the detector process the next slice of data.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="IController"/>.
		/// </param>
		/// <remarks>
		/// This method is free to use whatever technique it would like to check
		/// for a beat.  It may use <see cref="IController.PlayerData"/>, user
		/// input, or anything else.
		/// </remarks>
		void Update(IController controller);
	}
}
