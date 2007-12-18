// Controller.cs
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

using Tao.Sdl;
using Tao.OpenGl;

namespace OpenVP {
	/// <summary>
	/// OpenVP controller.
	/// </summary>
	/// <remarks>
	/// The controller is responsible for setting up the OpenGL
	/// context and raising events.
	/// </remarks>
	public class Controller : IDisposable {
		private IRenderer mRenderer = null;
		
		/// <value>
		/// The object that will be rendered when
		/// <see cref="OpenVP.Controller.DrawFrame"/> is called.
		/// </value>
		public IRenderer Renderer {
			get {
				return this.mRenderer;
			}
			set {
				this.mRenderer = value;
			}
		}
		
		private bool mInitialized = false;
		
		private string mWindowTitle = "Open Visualization Platform";
		
		/// <value>
		/// The text displayed in the titlebar of the output window.
		/// </value>
		/// <remarks>
		/// May not be <c>null</c>.
		/// </remarks>
		public string WindowTitle {
			get {
				return this.mWindowTitle;
			}
			set {
				if (value == null)
					throw new ArgumentNullException();
				
				this.mWindowTitle = value;
				
				if (this.mInitialized)
					Sdl.SDL_WM_SetCaption(this.mWindowTitle,
					                      this.mWindowTitle);
			}
		}
		
		private PlayerData mPlayerData;
		
		/// <value>
		/// An object that contains data received from a media player.
		/// </value>
		/// <remarks>
		/// <para>This property may not be set to null.</para>
		/// <para>The initial value is an instance of
		/// <see cref="NullPlayerData"/>.</para>
		/// </remarks>
		public PlayerData PlayerData {
			get {
				return this.mPlayerData;
			}
			set {
				if (value == null)
					throw new ArgumentNullException();
				
				this.mPlayerData = value;
			}
		}
		
		private IBeatDetector mBeatDetector;
		
		/// <value>
		/// An object that can detect beats in music.
		/// </value>
		/// <remarks>
		/// <para>This property may not be set to null.</para>
		/// <para>The initial value is an instance of
		/// <see cref="NullPlayerData"/>.</para>
		/// </remarks>
		public IBeatDetector BeatDetector {
			get {
				return this.mBeatDetector;
			}
			set {
				if (value == null)
					throw new ArgumentNullException();
				
				this.mBeatDetector = null;
			}
		}
		
		private int mWindowWidth;
		
		/// <value>
		/// The width of the render window in pixels.
		/// </value>
		public int WindowWidth {
			get {
				return this.mWindowWidth;
			}
		}
		
		private int mWindowHeight;
		
		/// <value>
		/// The height of the render window in pixels.
		/// </value>
		public int WindowHeight {
			get {
				return this.mWindowHeight;
			}
		}
		
		/// <summary>
		/// Fired when the end user clicks the close button on the output
		/// window.
		/// </summary>
		/// <remarks>
		/// The output window does not close automatically.  If this behavior
		/// is desired, the consumer of this class must attach an event handler
		/// that will close the output window and/or application manually.
		/// </remarks>
		public event EventHandler WindowClosed;
		
		/// <summary>
		/// Fired when the end user presses or releases a key in the output
		/// window.
		/// </summary>
		public event KeyboardEventHandler Keyboard;
		
		/// <summary>
		/// Creates a new controller.
		/// </summary>
		public Controller() {
			NullPlayerData data = new NullPlayerData();
			
			this.mPlayerData = data;
			this.mBeatDetector = data;
		}
		
		private void SdlThrow() {
			throw new InvalidOperationException("SDL error: " +
			                                    Sdl.SDL_GetError());
		}
		
		private void SdlTry(int ret) {
			if (ret == -1)
				this.SdlThrow();
		}
		
		private void SetVideoMode(int w, int h) {
			IntPtr surface = Sdl.SDL_SetVideoMode(w, h, 24, Sdl.SDL_OPENGL |
			                                      Sdl.SDL_RESIZABLE);
			
			if (surface == IntPtr.Zero)
				this.SdlThrow();
			
			this.mWindowWidth = w;
			this.mWindowHeight = h;
			
			Gl.glViewport(0, 0, w, h);
			
			Gl.glMatrixMode(Gl.GL_MODELVIEW);
			Gl.glLoadIdentity();
			
			Gl.glMatrixMode(Gl.GL_PROJECTION);
			Gl.glLoadIdentity();
		}
		
		/// <summary>
		/// Creates a window and a corresponding OpenGL context.
		/// </summary>
		public void Initialize() {
			this.SdlTry(Sdl.SDL_Init(Sdl.SDL_INIT_VIDEO));
			
			this.SdlTry(Sdl.SDL_GL_SetAttribute(Sdl.SDL_GL_DOUBLEBUFFER, 1));
			
			this.SetVideoMode(800, 600);
			
			Sdl.SDL_WM_SetCaption(this.mWindowTitle, this.mWindowTitle);
			
			Sdl.SDL_EnableUNICODE(1);
			
			Gl.glShadeModel(Gl.GL_SMOOTH);
			
			Gl.glEnable(Gl.GL_LINE_SMOOTH);
			Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
			
			Gl.glEnable(Gl.GL_BLEND);
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
			
			this.mInitialized = true;
		}
		
		/// <summary>
		/// Destroys the window and associated OpenGL context.
		/// </summary>
		public void Destroy() {
			this.mInitialized = false;
			
			Sdl.SDL_Quit();
		}
		
		void IDisposable.Dispose() {
			this.Destroy();
		}
		
		/// <summary>
		/// Renders one frame of output using
		/// <see cref="OpenVP.Controller.Renderer"/>.
		/// </summary>
		/// <remarks>
		/// Any pending events are raised synchronously during this call.
		/// </remarks>
		public void DrawFrame() {
			Sdl.SDL_Event ev;
			
			while (Sdl.SDL_PollEvent(out ev) != 0) {
				switch (ev.type) {
				case Sdl.SDL_VIDEORESIZE:
					this.SetVideoMode(ev.resize.w, ev.resize.h);
					break;
					
				case Sdl.SDL_QUIT:
					if (this.WindowClosed != null)
						this.WindowClosed(this, EventArgs.Empty);
					break;
					
				case Sdl.SDL_KEYDOWN:
				case Sdl.SDL_KEYUP:
					if (this.Keyboard == null)
						break;
					
					Sdl.SDL_keysym sym = ev.key.keysym;
					
					this.Keyboard(this, new KeyboardEventArgs(ev.key.state == Sdl.SDL_RELEASED,
					                                          sym.sym, sym.mod,
					                                          (char) sym.unicode));
					
					break;
				}
			}
			
			if (this.mRenderer != null)
				this.mRenderer.Render(this);
			
			Gl.glFlush();
			Sdl.SDL_GL_SwapBuffers();
		}
	}
}
