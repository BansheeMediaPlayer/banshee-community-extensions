// Effect.cs
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
	/// The base class for OpenVP effects.
	/// </summary>
	/// <remarks>
	/// <para>An effect is some sequence of rendering steps.  Anything that is
	/// possible with OpenGL may be performed by an effect.</para>
	/// <para>If the effect is intended to be used from a designer interface
	/// it should have an <see cref="OpenVP.EffectTitleAttribute"/>.</para>
	/// <para>If an effect is not serializable, a GUI designer may refuse to use
	/// it since it would not be able to save the effect settings.</para>
	/// </remarks>
	[Serializable]
	public abstract class Effect : IRenderer, IDisposable {
		/// <summary>
		/// Returns the display name of an effect type.
		/// </summary>
		/// <param name="type">
		/// A <see cref="System.Type"/> that derives from this class.
		/// </param>
		/// <returns>
		/// The title of the effect.  If the type has an
		/// <see cref="OpenVP.EffectTitleAttribute"/> then it will be used to
		/// determine the title, otherwise the name of the runtime type will be
		/// used.
		/// </returns>
		public static string GetTitle(Type type) {
			object[] attrs = type.GetCustomAttributes(typeof(EffectTitleAttribute), false);
			
			if (attrs.Length == 0)
				return type.FullName;
			
			return ((EffectTitleAttribute) attrs[0]).Title;
		}
		
		private bool mEnabled = true;
		
		/// <value>
		/// Whether or not the effect is enabled.
		/// </value>
		public bool Enabled {
			get {
				return this.mEnabled;
			}
			set {
				this.mEnabled = value;
			}
		}
		
		private string mName = null;
		
		/// <value>
		/// The name of the effect.
		/// </value>
		/// <remarks>
		/// This value may be used for a variety of purposes.  For example, the
		/// value could be used to find a particular effect object in a list.
		/// </remarks>
		public string Name {
			get {
				return this.mName;
			}
			set {
				this.mName = value;
			}
		}
		
		/// <value>
		/// The title of this effect.
		/// </value>
		public string Title {
			get {
				return Effect.GetTitle(this.GetType());
			}
		}
		
		/// <summary>
		/// Compute any information required to render the next frame.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="Controller"/>.
		/// </param>
		/// <remarks>
		/// This method is always called each frame, before any call to
		/// <see cref="OpenVP.Effect.RenderFrame"/>.
		/// </remarks>
		public abstract void NextFrame(Controller controller);
		
		/// <summary>
		/// Render the next frame.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="Controller"/>.
		/// </param>
		/// <remarks>
		/// If <see cref="OpenVP.Effect.Enabled"/> is <c>false</c>, this method
		/// will not be called.  Otherwise it will be called immediately after
		/// a call to <see cref="OpenVP.Effect.NextFrame"/>.
		/// </remarks>
		public abstract void RenderFrame(Controller controller);
		
		/// <summary>
		/// Renders this effect.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="Controller"/>.
		/// </param>
		public void Render(Controller controller) {
			this.NextFrame(controller);
			
			if (this.mEnabled)
				this.RenderFrame(controller);
		}
		
		/// <summary>
		/// Returns a property sheet object.
		/// </summary>
		/// <returns>
		/// A property sheet of the indicated type, or <c>null</c> if there is
		/// no property sheet for this effect type.
		/// </returns>
		/// <exception cref="System.NotSupportedException">Thrown if the effect
		/// does not know how to produce a property sheet for the specified
		/// type.</exception>
		/// <remarks>
		/// <para>A property sheet is a widget in some windowing system that
		/// will present the options for this effect and allow them to be
		/// tweaked.</para>
		/// <para>The type of the type parameter can be used to determine which
		/// type of object to return.  See the core OpenVP effect library for an
		/// example of how to properly implement multiple widget types.</para>
		/// </remarks>
		public virtual T GetPropertySheet<T>() where T : class {
			return null;
		}
		
		/// <summary>
		/// Disposes the effect and any unmanaged resources.
		/// </summary>
		/// <remarks>
		/// Applications using OpenVP should be careful to call this method on
		/// the same thread as the controller loop since OpenGL is not
		/// threadsafe.
		/// </remarks>
		public virtual void Dispose() {
		}
	}
}
