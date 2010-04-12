// LinearPreset.cs
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

namespace OpenVP {
	/// <summary>
	/// Preset that renders a list of effects in order.
	/// </summary>
	/// <remarks>
	/// Access to a LinearPreset should be synchronized with the controller
	/// if it is being used as the main IRenderer.
	/// </remarks>
	[Serializable]
	public class LinearPreset : IRenderer, IDisposable, IPreset {
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
		/// to handle <see cref="IController.KeyboardEvent"/>.
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
		/// A <see cref="IController"/>.
		/// </param>
		public void Render(IController controller) {
			foreach (Effect i in this.mEffects)
				i.Render(controller);
		}
		
		/// <summary>
		/// Disposes all contained effects.
		/// </summary>
		public void Dispose() {
			foreach (Effect i in this.mEffects)
				i.Dispose();
		}
		
		/// <summary>
		/// Returns an effect by name.
		/// </summary>
		/// <param name="name">
		/// The effect name to find.
		/// </param>
		/// <returns>
		/// The effect with the specified name, or null if no such effect was
		/// found.
		/// </returns>
		public Effect GetEffect(string name) {
			foreach (Effect i in this.mEffects)
				if (i.Name == name)
					return i;
			
			return null;
		}
	}
}
