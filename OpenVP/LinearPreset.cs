// LinearPreset.cs
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
using System.Collections.Generic;

namespace OpenVP {
	/// <summary>
	/// Preset that renders a list of effects in order.
	/// </summary>
	/// <remarks>
	/// Access to a LinearPreset should be synchronized with the controller
	/// if it is being used as the main IRenderer.
	/// </remarks>
	[Serializable]
	public class LinearPreset : IRenderer {
		private List<Effect> mEffects = new List<Effect>();
		
		/// <value>
		/// The effects to render.
		/// </value>
		public List<Effect> Effects {
			get {
				return this.mEffects;
			}
		}
		
		private Dictionary<KeyboardEventArgs, KeyboardEventHandler> mKeybindings =
			new Dictionary<KeyboardEventArgs, KeyboardEventHandler>();
		
		/// <value>
		/// Keybindings that may be invoked by the user while the preset is
		/// rendering.
		/// </value>
		public Dictionary<KeyboardEventArgs, KeyboardEventHandler> Keybindings {
			get {
				return this.mKeybindings;
			}
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		public LinearPreset() {
		}
		
		/// <summary>
		/// Handle a keyboard event.
		/// </summary>
		/// <param name="sender">
		/// The sending object.
		/// </param>
		/// <param name="args">
		/// A <see cref="KeyboardEventArgs"/>.
		/// </param>
		/// <remarks>
		/// This method matches the signature required for
		/// <see cref="KeyboardEventHandler"/> so that it may be used directly
		/// to handle <see cref="Controller.Keyboard"/>.
		/// </remarks>
		public void HandleKeyboard(object sender, KeyboardEventArgs args) {
			KeyboardEventHandler handler;
			
			if (this.mKeybindings.TryGetValue(args, out handler))
				handler(this, args);
		}
		
		/// <summary>
		/// Renders the effect list.
		/// </summary>
		/// <param name="controller">
		/// A <see cref="Controller"/>.
		/// </param>
		public void Render(Controller controller) {
			foreach (Effect i in this.mEffects)
				i.Render(controller);
		}
	}
}
