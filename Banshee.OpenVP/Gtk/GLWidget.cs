/*
 * Gtk GLWidget Sharp - Gtk OpenGL Widget for CSharp
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR
 * ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
 * DAMAGE.
 */

using System;
using System.Security;
using System.Runtime.InteropServices;
using Gtk;

namespace Gtk
{
    [System.ComponentModel.ToolboxItem(true)]
    public class GLWidget : DrawingArea, IDisposable
    {
        static PlatformID platformID = System.Environment.OSVersion.Platform;

        bool doubleBuffer = true;
        public new bool DoubleBuffered
        {
            get { return doubleBuffer; }
            set { doubleBuffer = value; }
        }

        byte colorBits = 24;
        public byte ColorBits
        {
            get { return colorBits; }
            set { colorBits = value; }
        }

        byte alphaBits = 0;
        public byte AlphaBits
        {
            get { return alphaBits; }
            set { alphaBits = value; }
        }

        byte depthBits = 32;
        public byte DepthBits
        {
            get { return depthBits; }
            set { depthBits = value; }
        }

        byte stencilBits = 0;
        public byte StencilBits
        {
            get { return stencilBits; }
            set { stencilBits = value; }
        }

        HandleRef renderingContextHandle;
        public HandleRef RenderingContextHandle
        {
            get { return renderingContextHandle; }
        }

        Gdk.Visual visual = null;
        static GLWidget globalSharedContextWidget = null;
        GLWidget sharedContextWidget;

        public GLWidget()
             : this(null)
        {
            if( globalSharedContextWidget == null)
            {
                globalSharedContextWidget = this;
            }
            else
            {
                sharedContextWidget = globalSharedContextWidget;
            }
        }

        public GLWidget(GLWidget sharedContextWidget)
        {
            base.DoubleBuffered = false;
            Realized += new EventHandler(HandleRealized);
            ExposeEvent += new ExposeEventHandler(HandleExposeEvent);
            
            this.sharedContextWidget = sharedContextWidget;
        }

        public override void Dispose()
        {
            base.Dispose();

            switch (platformID)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    wglDeleteContext(renderingContextHandle.Handle);
                    break;

                case PlatformID.Unix:
                default:
                    if (visual != null) {
                        glXDestroyContext(gdk_x11_display_get_xdisplay(visual.Screen.Display.Handle), renderingContextHandle);
                    }
                    break;
            }
        }

        void HandleRealized(object sender, EventArgs eventArgs)
        {
            if (renderingContextHandle.Handle != IntPtr.Zero)
                return;
            
            switch (platformID)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    LoadLibrary("opengl32.dll");

                    IntPtr windowHandle = gdk_win32_drawable_get_handle(GdkWindow.Handle);
                    IntPtr deviceContext = GetDC(windowHandle);

                    PIXELFORMATDESCRIPTOR pixelFormatDescriptor = new PIXELFORMATDESCRIPTOR();
                    pixelFormatDescriptor.nSize = (short)System.Runtime.InteropServices.Marshal.SizeOf(pixelFormatDescriptor);
                    pixelFormatDescriptor.nVersion = 1;
                    pixelFormatDescriptor.iPixelType = PFD_TYPE_RGBA;
                    pixelFormatDescriptor.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL;
                    if (doubleBuffer) pixelFormatDescriptor.dwFlags |= PFD_DOUBLEBUFFER;
                    pixelFormatDescriptor.cColorBits = colorBits;
                    pixelFormatDescriptor.cAlphaBits = alphaBits;
                    pixelFormatDescriptor.cDepthBits = depthBits;
                    pixelFormatDescriptor.cStencilBits = stencilBits;

                    int pixelFormat = ChoosePixelFormat(deviceContext, ref pixelFormatDescriptor);
                    if (!SetPixelFormat(deviceContext, pixelFormat, ref pixelFormatDescriptor)) throw new Exception("Cannot SetPixelFormat!");

                    IntPtr renderingContext = wglCreateContext(deviceContext);
                    renderingContextHandle = new HandleRef(this, renderingContext);

                    ReleaseDC(windowHandle, deviceContext);

                    if (sharedContextWidget != null)
                    {
                        GLWidget primaryWidget = sharedContextWidget;
                        while (primaryWidget.sharedContextWidget != null) primaryWidget = primaryWidget.sharedContextWidget;

                        if (primaryWidget.RenderingContextHandle.Handle != IntPtr.Zero)
                        {
                            wglShareLists(primaryWidget.RenderingContextHandle.Handle, RenderingContextHandle.Handle);
                        }
                        else
                        {
                            this.sharedContextWidget = null;
                            primaryWidget.sharedContextWidget = this;
                        }
                    }

                    break;

                case PlatformID.Unix:
                default:
                    int[] attributeList = new int[24];
                    int attributeIndex = 0;

                    attributeList[attributeIndex++] = GLX_RGBA;
                    if (doubleBuffer) attributeList[attributeIndex++] = GLX_DOUBLEBUFFER;

                    attributeList[attributeIndex++] = GLX_RED_SIZE;
                    attributeList[attributeIndex++] = 1;

                    attributeList[attributeIndex++] = GLX_GREEN_SIZE;
                    attributeList[attributeIndex++] = 1;

                    attributeList[attributeIndex++] = GLX_BLUE_SIZE;
                    attributeList[attributeIndex++] = 1;

                    if (alphaBits != 0)
                    {
                        attributeList[attributeIndex++] = GLX_ALPHA_SIZE;
                        attributeList[attributeIndex++] = 1;
                    }

                    if (depthBits != 0)
                    {
                        attributeList[attributeIndex++] = GLX_DEPTH_SIZE;
                        attributeList[attributeIndex++] = 1;
                    }

                    if (stencilBits != 0)
                    {
                        attributeList[attributeIndex++] = GLX_STENCIL_SIZE;
                        attributeList[attributeIndex++] = 1;
                    }

                    attributeList[attributeIndex++] = GLX_NONE;

                    IntPtr xDisplay = gdk_x11_display_get_xdisplay(Screen.Display.Handle);
                    IntPtr visualIntPtr = IntPtr.Zero;

                    try
                    {
                        visualIntPtr = glXChooseVisual(xDisplay, Screen.Number, attributeList);
                    }
                    catch (DllNotFoundException e)
                    {
                        throw new Exception("OpenGL dll not found!", e);
                    }
                    catch (EntryPointNotFoundException enf)
                    {
                        throw new Exception("Glx entry point not found!", enf);
                    }

                    if (visualIntPtr == IntPtr.Zero)
                    {
                        throw new Exception("Visual");
                    }

                    XVisualInfo xVisualInfo = (XVisualInfo)Marshal.PtrToStructure(visualIntPtr, typeof(XVisualInfo));

                    IntPtr xRenderingContext = IntPtr.Zero;


                    if (sharedContextWidget != null)
                    {
                        GLWidget primaryWidget = sharedContextWidget;
                        while (primaryWidget.sharedContextWidget != null) primaryWidget = primaryWidget.sharedContextWidget;

                        if (primaryWidget.RenderingContextHandle.Handle != IntPtr.Zero)
                        {
                            xRenderingContext = glXCreateContext(xDisplay, visualIntPtr, primaryWidget.RenderingContextHandle, true);
                        }
                        else
                        {
                            xRenderingContext = glXCreateContext(xDisplay, visualIntPtr, new HandleRef(null, IntPtr.Zero), true);
                            this.sharedContextWidget = null;
                            primaryWidget.sharedContextWidget = this;
                        }
                    }
                    else
                    {
                        xRenderingContext = glXCreateContext(xDisplay, visualIntPtr, new HandleRef(null, IntPtr.Zero), true);
                    }

                    if (xRenderingContext == IntPtr.Zero)
                    {
                        throw new Exception("Unable to create rendering context");
                    }

                    renderingContextHandle = new HandleRef(this, xRenderingContext);

                    visual = (Gdk.Visual)GLib.Object.GetObject(gdk_x11_screen_lookup_visual(Screen.Handle, xVisualInfo.visualid));

                    if (visualIntPtr != IntPtr.Zero)
                    {
                        XFree(visualIntPtr);
                    }
                    break;
            }
        }

        void HandleExposeEvent(object o, ExposeEventArgs args)
        {
            MakeCurrent();

            OnRender();

            SwapBuffers();
        }
        
        public event EventHandler Render;
        public virtual void OnRender()
        {
            EventHandler render = Render;
            
            if( render != null )
            {
                render(this, EventArgs.Empty);
            }
        }
        
        public bool MakeCurrent()
        {
            switch (platformID)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    IntPtr windowHandle = gdk_win32_drawable_get_handle(GdkWindow.Handle);
                    IntPtr deviceContext = GetDC(windowHandle);
                    wglMakeCurrent(deviceContext, renderingContextHandle.Handle);
                    ReleaseDC(windowHandle, deviceContext);
                    return true;

                case PlatformID.Unix:
                default:
                    return glXMakeCurrent(gdk_x11_display_get_xdisplay(GdkWindow.Display.Handle), gdk_x11_drawable_get_xid(GdkWindow.Handle), renderingContextHandle);
            }
        }

        public void SwapBuffers()
        {
            switch (platformID)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    IntPtr windowHandle = gdk_win32_drawable_get_handle(GdkWindow.Handle);
                    IntPtr deviceContext = GetDC(windowHandle);
                    SwapBuffers(deviceContext);
                    ReleaseDC(windowHandle, deviceContext);
                    break;

                case PlatformID.Unix:
                default:
                    glXSwapBuffers(gdk_x11_display_get_xdisplay(GdkWindow.Display.Handle), gdk_x11_drawable_get_xid(GdkWindow.Handle));
                    break;
            }
        }

        #region WINDOWS
        public const int PFD_STEREO = 0x00000002;
        public const int PFD_DOUBLEBUFFER = 0x00000001;
        public const int PFD_DRAW_TO_WINDOW = 0x00000004;
        public const int PFD_SUPPORT_OPENGL = 0x00000020;
        public const int PFD_TYPE_RGBA = 0x00000000;

        [SuppressUnmanagedCodeSecurity, DllImport("libgdk-win32-2.0-0.dll")]
        public static extern IntPtr gdk_win32_drawable_get_handle(IntPtr d);

        [SuppressUnmanagedCodeSecurity, DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string fileName);

        [SuppressUnmanagedCodeSecurity, DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr windowHandle);

        [SuppressUnmanagedCodeSecurity, DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr windowHandle, IntPtr deviceContext);

        [SuppressUnmanagedCodeSecurity, DllImport("gdi32.dll")]
        public static extern int ChoosePixelFormat(IntPtr deviceContext, ref PIXELFORMATDESCRIPTOR pixelFormatDescriptor);

        [SuppressUnmanagedCodeSecurity, DllImport("gdi32.dll")]
        public static extern bool SetPixelFormat(IntPtr deviceContext, int pixelFormat, ref PIXELFORMATDESCRIPTOR pixelFormatDescriptor);

        [SuppressUnmanagedCodeSecurity, DllImport("gdi32.dll")]
        public static extern bool SwapBuffers(IntPtr deviceContext);

        [SuppressUnmanagedCodeSecurity, DllImport("opengl32.dll")]
        public static extern IntPtr wglCreateContext(IntPtr deviceContext);

        [SuppressUnmanagedCodeSecurity, DllImport("opengl32.dll")]
        public static extern bool wglDeleteContext(IntPtr renderingContext);

        [SuppressUnmanagedCodeSecurity, DllImport("opengl32.dll")]
        public static extern bool wglMakeCurrent(IntPtr deviceContext, IntPtr renderingContext);

        [SuppressUnmanagedCodeSecurity, DllImport("opengl32.dll")]
        public static extern bool wglShareLists(IntPtr hglrc1, IntPtr hglrc2);

        [StructLayout(LayoutKind.Sequential)]
        public struct PIXELFORMATDESCRIPTOR
        {
            public short nSize;
            public short nVersion;
            public int dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public byte iLayerType;
            public byte bReserved;
            public int dwLayerMask;
            public int dwVisibleMask;
            public int dwDamageMask; 
        }
        #endregion

        #region X

        const string linux_libgl_name = "libGL.so.1";
        const string linux_libx11_name = "libX11.so.6";
        const string linux_libgdk_x11_name = "libgdk-x11-2.0.so.0";

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libx11_name)]
        static extern void XFree(IntPtr handle);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libx11_name)]
        internal static extern uint XVisualIDFromVisual(IntPtr visual);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgl_name)]
        static extern IntPtr glXCreateContext(IntPtr display, IntPtr visualInfo, HandleRef shareList, bool direct);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgl_name)]
        static extern IntPtr glXChooseVisual(IntPtr display, int screen, int[] attr);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgl_name)]
        static extern void glXDestroyContext(IntPtr display, HandleRef ctx);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgl_name)]
        static extern bool glXMakeCurrent(IntPtr display, uint xdrawable, HandleRef ctx);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgl_name)]
        static extern void glXSwapBuffers(IntPtr display, uint drawable);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgdk_x11_name)]
        static extern uint gdk_x11_drawable_get_xid(IntPtr d);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgdk_x11_name)]
        static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr d);

        //[SuppressUnmanagedCodeSecurity, DllImport(linux_libgdk_x11_name)]
        //static extern IntPtr gdk_x11_visual_get_xvisual(IntPtr d);

        [SuppressUnmanagedCodeSecurity, DllImport(linux_libgdk_x11_name)]
        static extern IntPtr gdk_x11_screen_lookup_visual(IntPtr screen, uint xvisualid);

        const int GLX_NONE = 0;
        const int GLX_USE_GL = 1;
        const int GLX_BUFFER_SIZE = 2;
        const int GLX_LEVEL = 3;
        const int GLX_RGBA = 4;
        const int GLX_DOUBLEBUFFER = 5;
        const int GLX_STEREO = 6;
        const int GLX_AUX_BUFFERS = 7;
        const int GLX_RED_SIZE = 8;
        const int GLX_GREEN_SIZE = 9;
        const int GLX_BLUE_SIZE = 10;
        const int GLX_ALPHA_SIZE = 11;
        const int GLX_DEPTH_SIZE = 12;
        const int GLX_STENCIL_SIZE = 13;
        const int GLX_ACCUM_RED_SIZE = 14;
        const int GLX_ACCUM_GREEN_SIZE = 15;
        const int GLX_ACCUM_BLUE_SIZE = 16;
        const int GLX_ACCUM_ALPHA_SIZE = 17;

        [StructLayout(LayoutKind.Sequential)]
        private struct XVisualInfo
        {
            public IntPtr visual;
            public uint visualid;
            public int screen;
            public int depth;
            public int c_class;
            public uint red_mask;
            public uint blue_mask;
            public uint green_mask;
            public int colormap_size;
            public int bits_per_rgb;
        }
        #endregion
    }
}
