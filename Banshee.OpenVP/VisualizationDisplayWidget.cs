// 
// VisualizationDisplayWidget.cs
//  
// Author:
//       Chris Howie <cdhowie@gmail.com>
// 
// Copyright (c) 2009 Chris Howie
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
using System.Collections.Generic;
using System.Threading;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Mono.Addins;
using Gdk;
using Gtk;
using OpenVP;
using OpenVP.Core;
using Tao.OpenGl;

namespace Banshee.OpenVP
{
    public class VisualizationDisplayWidget : Gtk.Bin, IController
    {
        private HBox headerExtension;
        private CheckButton halfResolutionCheckbox;
        private ComboBox visualizationList;
        
        private ListStore visualizationStore = new ListStore(typeof(VisualizationExtensionNode));

        private GLWidget glWidget = null;

        private ManualResetEvent renderLock = new ManualResetEvent(false);

        private object cleanupLock = new object();

        private Thread RenderThread;
        
        public Widget HeaderExtension {
            get { return headerExtension; }
        }
        
        public VisualizationDisplayWidget()
        {
            Stetic.BinContainer.Attach(this);
            
            BuildHeaderExtension();

            this.visualizationList.Model = this.visualizationStore;

            CellRendererText text = new CellRendererText();
            this.visualizationList.PackStart(text, true);
            this.visualizationList.SetCellDataFunc(text, VisualizationCellDataFunc);

            this.glWidget = new GLWidget();
            this.glWidget.DoubleBuffered = true;
            this.glWidget.Render += this.OnRender;
            this.glWidget.SizeAllocated += this.OnGlSizeAllocated;
            this.glWidget.Show();
            
            this.Add(this.glWidget);
            this.Show();

            this.playerData = new BansheePlayerData(ServiceManager.PlayerEngine.ActiveEngine);
            this.playerData.Active = false;

            this.glWidget.Realized += delegate {
                if (!this.loopRunning) {
                    this.loopRunning = true;
                    this.RenderThread = new Thread(this.RenderLoop);
                    this.RenderThread.Start();
                }

                this.ConnectVisualization();
            };

            this.glWidget.Unrealized += delegate {
                this.DisposeRenderer();
            };
            
            AddinManager.AddExtensionNodeHandler("/Banshee/OpenVP/Visualization", this.OnVisualizationChanged);
        }
        
        private void BuildHeaderExtension()
        {
            headerExtension = new HBox(false, 6);
            
            halfResolutionCheckbox = new CheckButton("Low resolution");
            halfResolutionCheckbox.Toggled += OnHalfResolutionCheckboxToggled;
            
            visualizationList = new ComboBox();
            visualizationList.Changed += OnVisualizationListChanged;
            
            headerExtension.PackStart(halfResolutionCheckbox, false, false, 0);
            headerExtension.PackStart(visualizationList, false, false, 0);
            
            headerExtension.ShowAll();
        }

        private static void VisualizationCellDataFunc(CellLayout layout, CellRenderer r, TreeModel model, TreeIter iter)
        {
            VisualizationExtensionNode node = (VisualizationExtensionNode) model.GetValue(iter, 0);
            
            ((CellRendererText) r).Text = (node == null) ? "" : node.Label;
        }

        private void OnVisualizationChanged(object o, ExtensionNodeEventArgs args)
        {
            VisualizationExtensionNode node =
                (VisualizationExtensionNode) args.ExtensionNode;

            if (args.Change == ExtensionChange.Add) {
                this.visualizationStore.AppendValues(node);
                
                if (this.visualizationStore.IterNChildren() == 1)
                    this.visualizationList.Active = 0;
            } else {
                TreeIter i;

                if (this.visualizationStore.GetIterFirst(out i)) {
                    do {
                        if (this.visualizationStore.GetValue(i, 0) == node) {
                            this.visualizationStore.Remove(ref i);
                            break;
                        }
                    } while (this.visualizationStore.IterNext(ref i));
                }
            }
        }

        private void OnGlSizeAllocated (object o, SizeAllocatedArgs args)
        {
            Rectangle allocation = args.Allocation;
            
            ((IController) this).Resize(allocation.Width, allocation.Height);
        }

        private void OnRender(object o, EventArgs e)
        {
            ((IController) this).RenderFrame();
        }

        private bool loopRunning = false;

        private bool haveDataSlice = false;
        
        private void RenderLoop()
        {
            this.renderLock.Set();

            this.haveDataSlice = false;
            
            while (this.loopRunning) {
                if (this.renderLock.WaitOne(500, false) && this.playerData.Update(500)) {
                    this.haveDataSlice = true;
                    this.renderLock.Reset();
                    
                    Banshee.Base.ThreadAssist.ProxyToMain(this.glWidget.QueueDraw);
                }
            }
            
            lock (this.cleanupLock) {
                this.haveDataSlice = false;
                
                this.playerData.Dispose();
                this.playerData = null;
            }
        }

        protected override void OnDestroyed ()
        {
            base.OnDestroyed ();

            this.loopRunning = false;
            if (this.RenderThread != null) {
                this.RenderThread.Join();
            }

            this.DisposeRenderer();
            
            this.glWidget.Dispose();
            this.glWidget.Destroy();
        }
        
        protected override void OnMapped ()
        {
            base.OnMapped ();
            this.playerData.Active = true;
        }
        
        protected override void OnUnmapped ()
        {
            base.OnUnmapped();
            this.playerData.Active = false;
        }

        protected virtual void OnVisualizationListChanged (object sender, System.EventArgs e)
        {
            this.ConnectVisualization();
        }

        private void ConnectVisualization() {
            TreeIter i;

            this.DisposeRenderer();
            if (!this.visualizationList.GetActiveIter(out i)) {
                return;
            }

            VisualizationExtensionNode node =
                (VisualizationExtensionNode) this.visualizationStore.GetValue(i, 0);

            this.renderer = node.CreateObject();
        }

        private void DisposeRenderer()
        {
            IDisposable r = this.renderer as IDisposable;
            this.renderer = null;

            if (r != null) {
                r.Dispose();
            }
        }

        protected virtual void OnHalfResolutionCheckboxToggled(object sender, System.EventArgs e)
        {
            this.halfResolution = this.halfResolutionCheckbox.Active;
            this.needsResize = true;
        }

#region IController
        private IRenderer renderer = null;
        
        private int widgetHeight;
        
        private int widgetWidth;

        private int renderHeight;

        private int renderWidth;

        private bool needsResize = true;
        
        private bool halfResolution = false;

        private BansheePlayerData playerData = null;
        
        private TextureHandle resizeTexture = null;

        int IController.Height {
            get { return this.renderHeight; }
        }

        int IController.Width {
            get { return this.renderWidth; }
        }
        
        event EventHandler IController.Closed {
            add { }
            remove { }
        }

        event KeyboardEventHandler IController.KeyboardEvent {
            add { }
            remove { }
        }

        IRenderer IController.Renderer {
            get { return this.renderer; }
            set { this.renderer = value; }
        }

        private IBeatDetector beatDetector = new NullPlayerData();

        IBeatDetector IController.BeatDetector {
            get { return this.beatDetector; }
        }

        PlayerData IController.PlayerData {
            get { return this.playerData; }
        }
        
        private static int NearestPowerOfTwo(int v)
        {
            if (v < 0)
                throw new ArgumentOutOfRangeException("v < 0");
            
            if (v == 0)
                return v;
            
            int power = 0;
            
            while (v > 0) {
                power++;
                v >>= 1;
            }
            
            return 1 << (power - 1);
        }
        
        private void ResizeToWidgetSize()
        {
            if (this.renderWidth == this.widgetWidth &&
                this.renderHeight == this.widgetHeight) {
                if (this.resizeTexture != null) {
                    this.resizeTexture.Dispose();
                    this.resizeTexture = null;
                }
                
                return;
            }
            
            if (this.resizeTexture == null)
                this.resizeTexture = new TextureHandle();
            
            this.resizeTexture.SetTextureSize(this.renderWidth, this.renderHeight);
            
            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.resizeTexture.TextureId);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                                this.renderWidth, this.renderHeight, 0);
            
            Gl.glViewport(0, 0, this.widgetWidth, this.widgetHeight);
                        
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
			
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            
            Gl.glColor4f(1, 1, 1, 1);
            
            Gl.glBegin(Gl.GL_QUADS);
            
            Gl.glTexCoord2f( 0,  0);
            Gl.glVertex2f(  -1, -1);
            
            Gl.glTexCoord2f( 0,  1);
            Gl.glVertex2f(  -1,  1);
            
            Gl.glTexCoord2f( 1,  1);
            Gl.glVertex2f(   1,  1);
            
            Gl.glTexCoord2f( 1,  0);
            Gl.glVertex2f(   1, -1);
            
            Gl.glEnd();
            
            Gl.glPopAttrib();
        }
        
        private void ResizeToRenderSize()
        {
            if (this.renderWidth == this.widgetWidth &&
                this.renderHeight == this.widgetHeight)
                return;
            
            if (this.resizeTexture == null)
                return;
            
            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, this.resizeTexture.TextureId);
            
            Gl.glViewport(0, 0, this.renderWidth, this.renderHeight);
                        
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
			
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            
            Gl.glColor4f(1, 1, 1, 1);
            
            Gl.glBegin(Gl.GL_QUADS);
            
            Gl.glTexCoord2f( 0,  0);
            Gl.glVertex2f(  -1, -1);
            
            Gl.glTexCoord2f( 0,  1);
            Gl.glVertex2f(  -1,  1);
            
            Gl.glTexCoord2f( 1,  1);
            Gl.glVertex2f(   1,  1);
            
            Gl.glTexCoord2f( 1,  0);
            Gl.glVertex2f(   1, -1);
            
            Gl.glEnd();
            
            Gl.glPopAttrib();
        }
        
        void IController.RenderFrame()
        {
            Gl.glShadeModel(Gl.GL_SMOOTH);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            lock (this.cleanupLock) {
                IRenderer r = this.renderer;
                if (r != null) {
		            if (this.needsResize) {
		                Gl.glClearColor(0, 0, 0, 1);
		                Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
		                
		                this.needsResize = false;
		                
		                int w = this.widgetWidth;
		                int h = this.widgetHeight;
		                
		                if (this.halfResolution) {
		                    w >>= 1;
		                    h >>= 1;
		                }
		                
		                if (!Gl.IsExtensionSupported("GL_ARB_texture_non_power_of_two")) {
		                    this.renderWidth = NearestPowerOfTwo(w);
		                    this.renderHeight = NearestPowerOfTwo(h);
		                } else {
		                    this.renderWidth = w;
		                    this.renderHeight = h;
		                }
		            } else {
		                this.ResizeToRenderSize();
		            }
                    
                    if (this.haveDataSlice) {
                        Gl.glViewport(0, 0, this.renderWidth, this.renderHeight);
                        
                        Gl.glMatrixMode(Gl.GL_PROJECTION);
		                Gl.glLoadIdentity();
						
		                Gl.glMatrixMode(Gl.GL_MODELVIEW);
		                Gl.glLoadIdentity();
                        
                        r.Render(this);
                        
                        this.haveDataSlice = false;
                        this.renderLock.Set();
                        
                        this.ResizeToWidgetSize();
                    }
                } else {
                    Gl.glClearColor(0, 0, 0, 1);
                    Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
                }
            }

            Gl.glFlush();
        }

        void IController.Resize(int width, int height)
        {
            this.widgetWidth = width;
            this.widgetHeight = height;
            this.needsResize = true;
        }
#endregion
    }
}
