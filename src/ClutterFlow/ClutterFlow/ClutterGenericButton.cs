//
// ClutterGenericButton.cs
//
// Author:
//       Mathijs Dumon <mathijsken@hotmail.com>
//
// Copyright (c) 2010 Mathijs Dumon
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using Cairo;

using Clutter;

namespace ClutterFlow.Buttons
{
    public delegate void CreateTextureMethod (CairoTexture texture, int with_state);

    public class ClutterGenericButton : ClutterButton
    {
        CreateTextureMethod createTexture = null;

        public ClutterGenericButton (uint width, uint height, int state, CreateTextureMethod createTexture) : base (width, height, state, false)
        {
            this.createTexture = createTexture;
            Initialise ();
        }

        protected override void CreateTexture (Clutter.CairoTexture texture, int with_state)
        {
            if (createTexture != null) {
                createTexture (texture, with_state);
            } else {
                base.CreateTexture (texture, with_state);
            }
        }

    }
}
