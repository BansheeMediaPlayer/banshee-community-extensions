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
using System.Linq;
using System.Threading;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Gui;
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
        private const string SELECT_VIS_ACTION = "SelectVisualizationAction";
        private const string LOW_RES_ACTION = "LowResVisualizationAction";
        private uint global_ui_id;
        
        private Menu visualizationMenu;
        private Dictionary<VisualizationExtensionNode, RadioMenuItem> visualizationMenuMap =
            new Dictionary<VisualizationExtensionNode, RadioMenuItem>();
        
        private VisualizationExtensionNode activeVisualization;
        private MenuItem noVisualizationsMenuItem;

        private GLWidget glWidget = null;

        private ManualResetEvent renderLock = new ManualResetEvent(false);

        private object cleanupLock = new object();

        private Thread RenderThread;
        
        public VisualizationDisplayWidget()
        {
            this.visualizationMenu = new Menu();
            noVisualizationsMenuItem = new MenuItem("No visualizations installed");
            noVisualizationsMenuItem.Sensitive = false;
            noVisualizationsMenuItem.Show();
            visualizationMenu.Add(noVisualizationsMenuItem);
            
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
            
            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            
            ias.GlobalActions.AddImportant(new ActionEntry(SELECT_VIS_ACTION,
                                                           null, "Select visualization",
                                                           null, null,
                                                           OnSelectVisualizationClicked));
            
            ias.GlobalActions.AddImportant(new ToggleActionEntry(LOW_RES_ACTION,
                                                           		 null, "Low resolution",
                                                                 null, null,
                                                                 OnHalfResolutionToggled, false));
            
            ias.GlobalActions.UpdateAction(SELECT_VIS_ACTION, false);
            ias.GlobalActions.UpdateAction(LOW_RES_ACTION, false);
            
            global_ui_id = ias.UIManager.AddUiFromResource("ActiveSourceUI.xml");
        }
        
        private void OnVisualizationChanged(object o, ExtensionNodeEventArgs args)
        {
            VisualizationExtensionNode node =
                (VisualizationExtensionNode) args.ExtensionNode;

            if (args.Change == ExtensionChange.Add) {
                RadioMenuItem group = visualizationMenu.Children.OfType<RadioMenuItem>().FirstOrDefault();
                RadioMenuItem menu = group != null ?
                    new RadioMenuItem(group, node.Label) :
                        new RadioMenuItem(node.Label);
                
                menu.Show();
                menu.Activated += delegate {
                    activeVisualization = node;
                    ConnectVisualization();
                };
                
                visualizationMenuMap[node] = menu;
                visualizationMenu.Add(menu);
                
                if (group == null)
                    menu.Activate();
                
                noVisualizationsMenuItem.Hide();
            } else {
                RadioMenuItem menu;
                if (visualizationMenuMap.TryGetValue(node, out menu)) {
                    visualizationMenu.Remove(menu);
                    visualizationMenuMap.Remove(node);
                }
                
                bool haveVis = visualizationMenu.Children.Length != 1;
                
                if (node == activeVisualization) {
                    if (haveVis) {
                        ((MenuItem) visualizationMenu.Children[1]).Activate();
                    } else {
                        activeVisualization = null;
                    }
                    
                    ConnectVisualization();
                }
                
                noVisualizationsMenuItem.Visible = !haveVis;
            }
        }
        
        private void OnSelectVisualizationClicked (object o, EventArgs e)
        {
            visualizationMenu.Popup();
        }
        
        protected override void OnSizeAllocated (Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
            
            this.Child.Allocation = allocation;
        }
        
        protected override void OnSizeRequested (ref Requisition requisition)
        {
            requisition = this.Child.SizeRequest();
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
                if (this.playerData.Update(500)) {
                    this.haveDataSlice = true;
                    
                    this.renderLock.Reset();
                    Banshee.Base.ThreadAssist.ProxyToMain(this.glWidget.QueueDraw);
                    this.renderLock.WaitOne(500, false);
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
            this.loopRunning = false;
            if (this.RenderThread != null) {
                this.RenderThread.Join();
            }

            this.DisposeRenderer();
            
            Remove (glWidget);
            this.glWidget.Destroy();
            this.glWidget.Dispose();
            
            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            ias.GlobalActions.Remove(SELECT_VIS_ACTION);
            ias.GlobalActions.Remove(LOW_RES_ACTION);
            ias.UIManager.RemoveUi(global_ui_id);
            
            base.OnDestroyed ();
        }
        
        protected override void OnMapped ()
        {
            base.OnMapped ();
            this.playerData.Active = true;
            
            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            ias.GlobalActions.UpdateAction(SELECT_VIS_ACTION, true);
            ias.GlobalActions.UpdateAction(LOW_RES_ACTION, true);
        }
        
        protected override void OnUnmapped ()
        {
            base.OnUnmapped();
            this.playerData.Active = false;
            
            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            ias.GlobalActions.UpdateAction(SELECT_VIS_ACTION, false);
            ias.GlobalActions.UpdateAction(LOW_RES_ACTION, false);
        }

        private void ConnectVisualization() {
            this.DisposeRenderer();
            
            if (activeVisualization != null)
            	this.renderer = activeVisualization.CreateObject();
        }

        private void DisposeRenderer()
        {
            IDisposable r = this.renderer as IDisposable;
            this.renderer = null;

            if (r != null) {
                lock (cleanupLock) {
                	r.Dispose();
                }
            }
        }

        protected virtual void OnHalfResolutionToggled(object sender, System.EventArgs e)
        {
            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService> ();
            
            this.halfResolution = ((ToggleAction)ias.GlobalActions[LOW_RES_ACTION]).Active;
            this.needsResize = true;
        }

#region IController
        private volatile IRenderer renderer = null;
        
        private volatile int widgetHeight;
        
        private volatile int widgetWidth;

        private volatile int renderHeight;

        private volatile int renderWidth;

        private volatile bool needsResize = true;
        
        private volatile bool halfResolution = false;

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
                    this.renderLock.Set();
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
