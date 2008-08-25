// VisualizationDisplayWidget.cs created with MonoDevelop
// User: chris at 3:30 PM 8/22/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

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
using gl = Tao.OpenGl.Gl;

namespace Banshee.OpenVP
{
    [System.ComponentModel.ToolboxItem(true)]
    public partial class VisualizationDisplayWidget : Gtk.Bin, ISourceContents, IController
    {
        private ListStore visualizationStore = new ListStore(typeof(VisualizationExtensionNode));

        private GLWidget glWidget = null;

        private ManualResetEvent renderLock = new ManualResetEvent(false);

        private object cleanupLock = new object();
        
        public VisualizationDisplayWidget()
        {
            this.Build();

            this.visualizationList.Model = this.visualizationStore;

            CellRendererText text = new CellRendererText();
            this.visualizationList.PackStart(text, true);
            this.visualizationList.SetCellDataFunc(text, VisualizationCellDataFunc);

            this.glWidget = new GLWidget();
            this.glWidget.DoubleBuffered = true;
            this.glWidget.Render += this.OnRender;
            this.glWidget.SizeAllocated += this.OnGlSizeAllocated;
            this.glWidget.Show();
            
            this.glAlignment.Add(this.glWidget);

            this.glWidget.Realized += delegate {
                if (!this.loopRunning) {
                    this.loopRunning = true;
                    new Thread(this.RenderLoop).Start();
                }

                this.ConnectVisualization();
            };

            this.glWidget.Unrealized += delegate {
                this.DisposeRenderer();
            };
            
            AddinManager.AddExtensionNodeHandler("/Banshee/OpenVP/Visualization", this.OnVisualizationChanged);
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
            this.playerData = new BansheePlayerData(ServiceManager.PlayerEngine.ActiveEngine);

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

            this.DisposeRenderer();
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

#region ISourceContents
        private ISource source;
        
        Widget ISourceContents.Widget {
            get { return this; }
        }

        ISource ISourceContents.Source {
            get { return this.source; }
        }

        void ISourceContents.ResetSource()
        {
        }

        bool ISourceContents.SetSource(ISource source)
        {
            this.source = source;
            return true;
        }
#endregion

#region IController
        private IRenderer renderer = null;

        private int height;

        private int width;

        private bool needsResize = true;

        private BansheePlayerData playerData = null;

        int IController.Height {
            get { return this.height; }
        }

        int IController.Width {
            get { return this.width; }
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
        
        void IController.RenderFrame()
        {
            gl.glShadeModel(gl.GL_SMOOTH);
            gl.glEnable(gl.GL_LINE_SMOOTH);
            gl.glHint(gl.GL_LINE_SMOOTH_HINT, gl.GL_NICEST);

            gl.glEnable(gl.GL_BLEND);
            gl.glBlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA);

            if (this.needsResize) {
                this.needsResize = false;
                
                gl.glViewport(0, 0, width, height);
    
                gl.glMatrixMode(gl.GL_PROJECTION);
                gl.glLoadIdentity();
                
                gl.glMatrixMode(gl.GL_MODELVIEW);
                gl.glLoadIdentity();
            }

            lock (this.cleanupLock) {
                IRenderer r = this.renderer;
                if (r != null) {
                    if (this.haveDataSlice) {
                        r.Render(this);
                        
                        this.haveDataSlice = false;
                        this.renderLock.Set();
                    }
                } else {
                    gl.glClearColor(0, 0, 0, 1);
                    gl.glClear(gl.GL_COLOR_BUFFER_BIT);
                }
            }

            gl.glFlush();
        }

        void IController.Resize(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.needsResize = true;
        }
#endregion
    }
}
