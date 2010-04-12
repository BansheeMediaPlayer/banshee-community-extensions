// IController.cs
//
//  Copyright (C) 2008 Chris Howie
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
    /// Interface for objects that can control the rendering process.
    /// </summary>
    /// <remarks>
    /// The controller is responsible for setting up an OpenGL context and for
    /// providing data about the rendering environment.
    /// </remarks>
    public interface IController : IDisposable {
        /// <value>
        /// The object that will render each frame.
        /// </value>
        /// <remarks>
        /// This property must be set to a non-<code>null</code> reference
        /// before calling <see cref="IController.RenderFrame"/>.
        /// </remarks>
        IRenderer Renderer { get; set; }
        
        /// <value>
        /// The media player data source.
        /// </value>
        /// <remarks>
        /// This property may not be <code>null</code>.  If no player data is
        /// available, an instance of <see cref="NullPlayerData"/> may be
        /// returned instead.
        /// </remarks>
        PlayerData PlayerData { get; }
        /// <value>
        /// A beat detector, if available.
        /// </value>
        /// <remarks>
        /// This property may not be <code>null</code>.  If no beat detector is
        /// available, an instance of <see cref="NullPlayerData"/> may be
        /// returned instead.
        /// </remarks>
        IBeatDetector BeatDetector { get; }
        
        /// <value>
        /// The width of the output in pixels.
        /// </value>
        int Width { get; }
        /// <value>
        /// The height of the output in pixels.
        /// </value>
        int Height { get; }
        
        /// <summary>
        /// Resize the output environment, if possible.
        /// </summary>
        /// <param name="width">
        /// The new width in pixels.
        /// </param>
        /// <param name="height">
        /// The new height in pixels.
        /// </param>
        /// <remarks>
        /// May throw a <see cref="NotSupportedException"/> if resizing of the
        /// output environment is not possible.  May also throw an
        /// <see cref="ArgumentException"/> if resizing is possible, but not
        /// with the specified values (for example, if one of the values is
        /// less than 1).
        /// </remarks>
        void Resize(int width, int height);
        
        /// <summary>
        /// Raised when the output environment has been closed by the user.
        /// </summary>
        /// <remarks>
        /// This event may not be applicable to all controllers.
        /// </remarks>
        event EventHandler Closed;
        
        /// <summary>
        /// Raised when a key has been pressed in the output environment.
        /// </summary>
        /// <remarks>
        /// This event may not be applicable to all controllers.
        /// </remarks>
        event KeyboardEventHandler KeyboardEvent;
        
        /// <summary>
        /// Renders one frame of output.
        /// </summary>
        void RenderFrame();
    }
}
