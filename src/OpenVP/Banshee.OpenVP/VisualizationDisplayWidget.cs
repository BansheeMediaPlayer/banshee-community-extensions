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
        private bool restoreFromSchema = true;

        private GLWidget glWidget = null;

        private ManualResetEvent renderLock = new ManualResetEvent(false);

        private object cleanupLock = new object();

        private Thread renderThread;

        public VisualizationDisplayWidget()
        {
            visualizationMenu = new Menu();
            noVisualizationsMenuItem = new MenuItem(
                AddinManager.CurrentLocalizer.GetString ("No visualizations installed"));
            noVisualizationsMenuItem.Sensitive = false;
            noVisualizationsMenuItem.Show();
            visualizationMenu.Add(noVisualizationsMenuItem);

            glWidget = new GLWidget();
            glWidget.DoubleBuffered = true;
            glWidget.Render += OnRender;
            glWidget.SizeAllocated += OnGlSizeAllocated;
            glWidget.Show();

            Add(glWidget);
            Show();

            playerData = new BansheePlayerData(ServiceManager.PlayerEngine.ActiveEngine);
            playerData.Active = false;

            glWidget.Realized += delegate {
                if (!loopRunning) {
                    loopRunning = true;
                    renderThread = new Thread(RenderLoop);
                    renderThread.Start();
                }

                ConnectVisualization();
            };

            glWidget.Unrealized += delegate {
                DisposeRenderer();
            };

            AddinManager.AddExtensionNodeHandler("/Banshee/OpenVP/Visualization", OnVisualizationChanged);

            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();

            ias.GlobalActions.AddImportant(new ActionEntry(SELECT_VIS_ACTION,
                                                           null,
                                                           AddinManager.CurrentLocalizer.GetString ("Select visualization"),
                                                           null, null,
                                                           OnSelectVisualizationClicked));

            ias.GlobalActions.AddImportant(new ToggleActionEntry(LOW_RES_ACTION,
                                                                 null,
                                                                 AddinManager.CurrentLocalizer.GetString ("Low resolution"),
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

                visualizationMenuMap[node] = menu;
                visualizationMenu.Add(menu);

                bool force = false;

                if (restoreFromSchema && node.IsSchemaSelected) {
                    restoreFromSchema = false;
                    force = true;
                }

                if (force || group == null) {
                    menu.Active = true;
                    activeVisualization = node;
                    ConnectVisualization();
                }

                menu.Activated += delegate {
                    if (!menu.Active)
                        return;

                    activeVisualization = node;
                    ConnectVisualization();

                    restoreFromSchema = false;
                    Banshee.OpenVP.Settings.SelectedVisualizationSchema.Set(node.Type.FullName);
                };

                noVisualizationsMenuItem.Hide();
            } else {
                RadioMenuItem menu;
                if (visualizationMenuMap.TryGetValue(node, out menu)) {
                    visualizationMenu.Remove(menu);
                    visualizationMenuMap.Remove(node);
                }

                bool haveVis = visualizationMenu.Children.Length != 1;

                if (node == activeVisualization) {
                    restoreFromSchema = true;

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

        private void OnSelectVisualizationClicked(object o, EventArgs e)
        {
            visualizationMenu.Popup();
        }

        protected override void OnSizeAllocated(Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);

            Child.Allocation = allocation;
        }

        protected override void OnSizeRequested(ref Requisition requisition)
        {
            requisition = Child.SizeRequest();
        }

        private void OnGlSizeAllocated(object o, SizeAllocatedArgs args)
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
            renderLock.Set();

            haveDataSlice = false;

            while (loopRunning) {
                if (playerData.Update(500)) {
                    haveDataSlice = true;

                    renderLock.Reset();
                    Hyena.ThreadAssist.ProxyToMain(glWidget.QueueDraw);
                    renderLock.WaitOne(500, false);
                }
            }

            lock (cleanupLock) {
                haveDataSlice = false;

                playerData.Dispose();
                playerData = null;
            }
        }

        protected override void OnDestroyed()
        {
            loopRunning = false;
            if (renderThread != null) {
                renderThread.Join();
            }

            DisposeRenderer();

            Remove(glWidget);
            glWidget.Destroy();
            glWidget.Dispose();

            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            ias.GlobalActions.Remove(SELECT_VIS_ACTION);
            ias.GlobalActions.Remove(LOW_RES_ACTION);
            ias.UIManager.RemoveUi(global_ui_id);

            base.OnDestroyed();
        }

        protected override void OnMapped()
        {
            base.OnMapped();
            playerData.Active = true;

            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            ias.GlobalActions.UpdateAction(SELECT_VIS_ACTION, true);
            ias.GlobalActions.UpdateAction(LOW_RES_ACTION, true);
        }

        protected override void OnUnmapped()
        {
            base.OnUnmapped();
            playerData.Active = false;

            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();
            ias.GlobalActions.UpdateAction(SELECT_VIS_ACTION, false);
            ias.GlobalActions.UpdateAction(LOW_RES_ACTION, false);
        }

        private void ConnectVisualization()
        {
            DisposeRenderer();

            if (activeVisualization != null)
                renderer = activeVisualization.CreateObject();
        }

        private void DisposeRenderer()
        {
            IDisposable r = renderer as IDisposable;
            renderer = null;

            if (r != null) {
                lock (cleanupLock) {
                    r.Dispose();
                }
            }
        }

        protected virtual void OnHalfResolutionToggled(object sender, System.EventArgs e)
        {
            InterfaceActionService ias = ServiceManager.Get<InterfaceActionService>();

            halfResolution = ((ToggleAction)ias.GlobalActions[LOW_RES_ACTION]).Active;
            needsResize = true;
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

        int IController.Height
        {
            get { return renderHeight; }
        }

        int IController.Width
        {
            get { return renderWidth; }
        }

        event EventHandler IController.Closed
        {
            add { }
            remove { }
        }

        event KeyboardEventHandler IController.KeyboardEvent
        {
            add { }
            remove { }
        }

        IRenderer IController.Renderer
        {
            get { return renderer; }
            set { renderer = value; }
        }

        private IBeatDetector beatDetector = new NullPlayerData();

        IBeatDetector IController.BeatDetector
        {
            get { return beatDetector; }
        }

        PlayerData IController.PlayerData
        {
            get { return playerData; }
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
            if (renderWidth == widgetWidth &&
                renderHeight == widgetHeight) {
                if (resizeTexture != null) {
                    resizeTexture.Dispose();
                    resizeTexture = null;
                }

                return;
            }

            if (resizeTexture == null)
                resizeTexture = new TextureHandle();

            resizeTexture.SetTextureSize(renderWidth, renderHeight);

            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_TEXTURE_2D);

            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, resizeTexture.TextureId);
            Gl.glCopyTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, 0, 0,
                                renderWidth, renderHeight, 0);

            Gl.glViewport(0, 0, widgetWidth, widgetHeight);

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
            if (renderWidth == widgetWidth &&
                renderHeight == widgetHeight)
                return;

            if (resizeTexture == null)
                return;

            Gl.glPushAttrib(Gl.GL_ENABLE_BIT);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, resizeTexture.TextureId);

            Gl.glViewport(0, 0, renderWidth, renderHeight);

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

            lock (cleanupLock) {
                IRenderer r = renderer;
                if (r != null) {
                    if (needsResize) {
                        Gl.glClearColor(0, 0, 0, 1);
                        Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);

                        needsResize = false;

                        int w = widgetWidth;
                        int h = widgetHeight;

                        if (halfResolution) {
                            w >>= 1;
                            h >>= 1;
                        }

                        if (!Gl.IsExtensionSupported("GL_ARB_texture_non_power_of_two")) {
                            renderWidth = NearestPowerOfTwo(w);
                            renderHeight = NearestPowerOfTwo(h);
                        } else {
                            renderWidth = w;
                            renderHeight = h;
                        }
                    } else {
                        ResizeToRenderSize();
                    }

                    if (haveDataSlice) {
                        Gl.glViewport(0, 0, renderWidth, renderHeight);

                        Gl.glMatrixMode(Gl.GL_PROJECTION);
                        Gl.glLoadIdentity();

                        Gl.glMatrixMode(Gl.GL_MODELVIEW);
                        Gl.glLoadIdentity();

                        r.Render(this);

                        haveDataSlice = false;
                        renderLock.Set();

                        ResizeToWidgetSize();
                    }
                } else {
                    Gl.glClearColor(0, 0, 0, 1);
                    Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
                    renderLock.Set();
                }
            }

            Gl.glFlush();
        }

        void IController.Resize(int width, int height)
        {
            widgetWidth = width;
            widgetHeight = height;
            needsResize = true;
        }
#endregion
    }
}
