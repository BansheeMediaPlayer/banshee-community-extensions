// KeyboardEvent.cs
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
	/// A keyboard event handler.
	/// </summary>
	/// <param name="sender">
	/// The controller.
	/// </param>
	/// <param name="args">
	/// A <see cref="KeyboardEventArgs"/> containing the details of the event.
	/// </param>
	[Serializable]
	public delegate void KeyboardEventHandler(object sender,
	                                          KeyboardEventArgs args);
	
	/// <summary>
	/// Represents the details of one keyboard event.
	/// </summary>
	[Serializable]
	public class KeyboardEventArgs : EventArgs, IEquatable<KeyboardEventArgs> {
		private bool mReleased;
		
		private int mKeysym;
		
		private int mKeymod;
		
		private char mCharacter;
		
		/// <value>
		/// True if the key was released, or false if it was pressed.
		/// </value>
		public bool Released {
			get {
				return this.mReleased;
			}
		}
		
		/// <value>
		/// The keysym that triggered the event.  See the SDL documentation for
		/// possible values, or use the SDLK* constants on
		/// <see cref="Tao.Sdl.Sdl"/>.
		/// </value>
		public int Keysym {
			get {
				return this.mKeysym;
			}
		}
		
		/// <value>
		/// The modifiers currently active.  See the SDL documentation for
		/// possible values, or use the KMOD* constants on
		/// <see cref="Tao.Sdl.Sdl"/>.
		/// </value>
		public int Keymod {
			get {
				return this.mKeymod;
			}
		}
		
		/// <value>
		/// The character that corresponds to the event.  The null character
		/// indicates that the event has no corresponding character.
		/// </value>
		public char Character {
			get {
				return this.mCharacter;
			}
		}
		
		/// <summary>
		/// Public constructor.
		/// </summary>
		/// <param name="released">
		/// True if the key was released, or false if it was pressed.
		/// </param>
		/// <param name="keysym">
		/// An SDL keysym.
		/// </param>
		/// <param name="keymod">
		/// An SDL keymod.
		/// </param>
		/// <param name="character">
		/// The character corresponding to the event.
		/// </param>
		public KeyboardEventArgs(bool released, int keysym, int keymod,
		                         char character) {
			this.mReleased = released;
			this.mKeysym = keysym;
			this.mKeymod = keymod;
			this.mCharacter = character;
		}
		
		/// <summary>
		/// Checks for equality with another KeyboardEventArgs.
		/// </summary>
		/// <param name="x">
		/// A <see cref="KeyboardEventArgs"/>.
		/// </param>
		/// <returns>
		/// True if and only if this object is equal to <c>x</c>.
		/// </returns>
		/// <remarks>
		/// The <see cref="KeyboardEventArgs.Character"/> property is not
		/// considered when checking for equality.  All other properties must be
		/// equal.
		/// </remarks>
		public bool Equals(KeyboardEventArgs x) {
			if (x == null)
				return false;
			
			return this.Released == x.Released &&
				this.Keysym == x.Keysym &&
					this.Keymod == x.Keymod;
		}
		
		/// <summary>
		/// Checks for equality with another object.
		/// </summary>
		/// <param name="o">
		/// An <see cref="System.Object"/>.
		/// </param>
		/// <returns>
		/// True if and only if the object is equal to this object.
		/// </returns>
		/// <remarks>
		/// To be considered equal, <c>o</c> must be an instance of
		/// KeyboardEventArgs and must meet the conditions specified in
		/// <see cref="KeyboardEventArgs.Equals(KeyboardEventArgs)"/>.
		/// </remarks>
		public override bool Equals(object o) {
			return this.Equals(o as KeyboardEventArgs);
		}
		
		/// <summary>
		/// Returns a hash code for this object.
		/// </summary>
		/// <returns>
		/// The hash code.
		/// </returns>
		public override int GetHashCode() {
			int hash = this.mKeysym ^ this.mKeymod;
			
			if (this.mReleased)
				hash ^= int.MaxValue;
			
			return hash;
		}
		
		/// <summary>
		/// Checks the parameters for equality.
		/// </summary>
		/// <param name="a">
		/// A <see cref="KeyboardEventArgs"/>.
		/// </param>
		/// <param name="b">
		/// A <see cref="KeyboardEventArgs"/>.
		/// </param>
		/// <returns>
		/// True if and only if both parameters are equal or are null.
		/// </returns>
		public static bool operator==(KeyboardEventArgs a,
		                              KeyboardEventArgs b) {
			if (a == null)
				return b == null;
			
			return a.Equals(b);
		}
		
		/// <summary>
		/// Checks the parameters for inequality.
		/// </summary>
		/// <param name="a">
		/// A <see cref="KeyboardEventArgs"/>.
		/// </param>
		/// <param name="b">
		/// A <see cref="KeyboardEventArgs"/>.
		/// </param>
		/// <returns>
		/// True if and only if the parameters are not equal, or if only one is
		/// null.
		/// </returns>
		public static bool operator!=(KeyboardEventArgs a,
		                              KeyboardEventArgs b) {
			if (a == null)
				return b != null;
			
			return !a.Equals(b);
		}
	}
}
