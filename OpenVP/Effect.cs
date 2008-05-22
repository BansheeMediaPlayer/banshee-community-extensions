// Effect.cs
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
using System.Runtime.Serialization;

using OpenVP.Metadata;

namespace OpenVP {
	/// <summary>
	/// The base class for OpenVP effects.
	/// </summary>
	/// <remarks>
	/// <para>An effect is some sequence of rendering steps.  Anything that is
	/// possible with OpenGL may be performed by an effect.</para>
	/// <para>If the effect is intended to be used from a designer interface
	/// it should have a
	/// <see cref="OpenVP.Metadata.DisplayNameAttribute"/>.</para>
	/// <para>If an effect is not serializable, a GUI designer may refuse to use
	/// it since it would not be able to save the effect settings.</para>
	/// </remarks>
	[Serializable]
	public abstract class Effect : IRenderer, IDisposable, IDeserializationCallback {
		/// <summary>
		/// Returns the display name of an effect type.
		/// </summary>
		/// <param name="type">
		/// A <see cref="System.Type"/> that derives from this class.
		/// </param>
		/// <returns>
		/// The title of the effect.  If the type has a
		/// <see cref="OpenVP.Metadata.DisplayNameAttribute"/> then it
		/// will be used to determine the title, otherwise the name of the
		/// runtime type will be used.
		/// </returns>
		public static string GetTitle(Type type) {
			object[] attrs = type.GetCustomAttributes(typeof(DisplayNameAttribute), false);
			
			if (attrs.Length == 0)
				return type.FullName;
			
			return ((DisplayNameAttribute) attrs[0]).DisplayName;
		}
		
		private bool mEnabled = true;
		
		/// <value>
		/// Whether or not the effect is enabled.
		/// </value>
		[Browsable(true), Category("Basic"), DefaultValue(true),
		 Description("Whether or not to render the effect.")]
		public bool Enabled {
			get {
				return this.mEnabled;
			}
			set {
				this.mEnabled = value;
			}
		}
		
		private string mName = "";
		
		/// <value>
		/// The name of the effect.
		/// </value>
		/// <remarks>
		/// This value may be used for a variety of purposes.  For example, the
		/// value could be used to find a particular effect object in a list.
		/// </remarks>
		[Browsable(true), Category("Basic"), DefaultValue(""),
		 Description("A name by which the effect may be referred to in scripts.")]
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
		[Browsable(false)]
		public string Title {
			get {
				return Effect.GetTitle(this.GetType());
			}
		}
		
		/// <summary>
		/// Compute any information required to render the next frame.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="IController"/>.
		/// </param>
		/// <remarks>
		/// This method is always called each frame, before any call to
		/// <see cref="OpenVP.Effect.RenderFrame"/>.
		/// </remarks>
		public abstract void NextFrame(IController controller);
		
		/// <summary>
		/// Render the next frame.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="IController"/>.
		/// </param>
		/// <remarks>
		/// If <see cref="OpenVP.Effect.Enabled"/> is <c>false</c>, this method
		/// will not be called.  Otherwise it will be called immediately after
		/// a call to <see cref="OpenVP.Effect.NextFrame"/>.
		/// </remarks>
		public abstract void RenderFrame(IController controller);
		
		/// <summary>
		/// Renders this effect.
		/// </summary>
		/// <param name="controller">
		/// The <see cref="IController"/>.
		/// </param>
		public void Render(IController controller) {
			this.NextFrame(controller);
			
			if (this.mEnabled)
				this.RenderFrame(controller);
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
        
        void IDeserializationCallback.OnDeserialization(object sender) {
            this.OnDeserialization(sender);
        }
        
        /// <summary>
        /// Called during deserialization.
        /// </summary>
        /// <param name="sender">
        /// Event sender.
        /// </param>
        /// <remarks>
        /// Subclasses can override this method to perform any needed
        /// initialization after object deserialization.  If this method is
        /// overridden, the base implementation must always be called first to
        /// allow superclasses to initialize themselves too.
        /// </remarks>
        protected virtual void OnDeserialization(object sender) {
        }
	}
}
