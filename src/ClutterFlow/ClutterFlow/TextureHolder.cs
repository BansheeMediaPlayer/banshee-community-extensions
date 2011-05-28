// 
// TextureHolder.cs
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

using Cogl;
using Gdk;

namespace ClutterFlow
{
    public delegate Cairo.ImageSurface NeedSurface ();

    public class TextureHolder : IDisposable {
        #region Fields
        private CoverManager cover_manager;
        public CoverManager CoverManager {
            get { return cover_manager; }
        }

        protected Cairo.ImageSurface default_surface;
        public Cairo.ImageSurface DefaultSurface {
            get {
                if (default_surface == null && GetDefaultSurface != null) {
                    default_surface = ClutterFlowActor.MakeReflection (GetDefaultSurface ());
                }
                return default_surface;
            }
        }

        protected IntPtr default_texture;
        public IntPtr DefaultTexture {
            get {
                if (default_texture == IntPtr.Zero) {
                    SetupDefaultTexture ();
                }
                return default_texture;
            }
        }

        protected IntPtr shade_texture;
        public IntPtr ShadeTexture {
            get {
                if (shade_texture == IntPtr.Zero) {
                    SetupShadeTexture ();
                }
                return shade_texture;
            }
        }

        public NeedSurface GetDefaultSurface;
        #endregion

        #region Initialisation
        public TextureHolder (CoverManager coverManager, NeedSurface getDefaultSurface)
        {
            this.cover_manager = coverManager;
            CoverManager.TextureSizeChanged += HandleTextureSizeChanged;
            this.GetDefaultSurface = getDefaultSurface;
            ReloadDefaultTextures ();
        }

        protected bool disposed = false;
        public virtual void Dispose ()
        {
            if (disposed) {
                return;
            }
            disposed = true;

            CoverManager.TextureSizeChanged -= HandleTextureSizeChanged;

            if (default_surface != null) {
                ((IDisposable) default_surface).Dispose ();
            }
            Cogl.Handle.Unref (default_texture);
            Cogl.Handle.Unref (shade_texture);
            default_texture = IntPtr.Zero;
            shade_texture = IntPtr.Zero;
        }

        public void ReloadDefaultTextures ()
        {
            if (default_surface != null) {
                ((IDisposable) default_surface).Dispose ();
            }
            default_surface = null; //reset this so it gets reloaded

            SetupDefaultTexture ();
            SetupShadeTexture ();
        }

        public void SetupDefaultTexture ()
        {
            if (default_texture==IntPtr.Zero) {
                if (DefaultSurface!=null) {
                    Cogl.PixelFormat fm;
                    if (DefaultSurface.Format == Cairo.Format.ARGB32) {
                        fm = PixelFormat.Argb8888Pre;
                    }
                    else //if (DefaultSurface.Format == Cairo.Format.RGB24)
                        fm = PixelFormat.Rgb888;

                    unsafe {

                        default_texture = ClutterHelper.cogl_texture_new_from_data((uint) DefaultSurface.Width, (uint) DefaultSurface.Height, Cogl.TextureFlags.None,
                                                                 fm, Cogl.PixelFormat.Any, (uint) DefaultSurface.Stride, DefaultSurface.DataPtr);
                    }
                } else {
                    default_texture = Cogl.Texture.NewWithSize ((uint) cover_manager.TextureSize, (uint) cover_manager.TextureSize,
                                                             Cogl.TextureFlags.None, Cogl.PixelFormat.Any);
                }
            }
        }

        public void SetupShadeTexture ()
        {
            if (shade_texture==IntPtr.Zero) {

                Gdk.Pixbuf finalPb = new Gdk.Pixbuf (Colorspace.Rgb, true, 8, cover_manager.TextureSize, cover_manager.TextureSize*2);

                unsafe {
                    int dst_rowstride = finalPb.Rowstride;
                    int dst_width = finalPb.Width;
                    int shd_width = (int) ((float) dst_width * 0.25f);
                    int dst_height = finalPb.Height;
                    byte * dst_byte = (byte *) finalPb.Pixels;
                    byte * dst_base = dst_byte;

                    for (int j = 0; j < dst_height; j++) {
                        dst_byte = ((byte *) dst_base) + j * dst_rowstride;
                        for (int i = 0; i < dst_width; i++) {
                            *dst_byte++ = 0x00;
                            *dst_byte++ = 0x00;
                            *dst_byte++ = 0x00;
                            if (i > shd_width)
                                *dst_byte++ = (byte) (255 * (float) (i - shd_width) / (float) (dst_width - shd_width));
                            else
                                *dst_byte++ = 0x00;
                        }
                    }

                    shade_texture = ClutterHelper.cogl_texture_new_from_data((uint) finalPb.Width, (uint) finalPb.Height, Cogl.TextureFlags.None,
                                                             PixelFormat.Rgba8888, Cogl.PixelFormat.Any, (uint) finalPb.Rowstride, finalPb.Pixels);
                }
            }
        }
        #endregion

        protected void HandleTextureSizeChanged (object sender, EventArgs e)
        {
            ReloadDefaultTextures ();
        }
    }
}

