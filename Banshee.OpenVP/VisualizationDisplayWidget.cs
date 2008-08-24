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

		private Queue<ThreadStart> glCallQueue = new Queue<ThreadStart>();
		
		public VisualizationDisplayWidget()
		{
			this.Build();

			this.visualizationList.Model = this.visualizationStore;

			CellRendererText text = new CellRendererText();
			this.visualizationList.PackStart(text, true);
			this.visualizationList.SetCellDataFunc(text, VisualizationCellDataFunc);

			this.glWidget = new GLWidget();
			this.glWidget.DoubleBuffered = true;
			this.glWidget.Show();
			this.glAlignment.Add(this.glWidget);

			this.glWidget.Realized += this.InitGL;
			
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

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			((IController) this).Resize(allocation.Width, allocation.Height);
		}

		private void InitGL(object o, EventArgs e)
		{
			new Thread(this.RenderLoop).Start();
		}

		private bool loopRunning = true;
		
		private void RenderLoop()
		{
			Thread.Sleep(1000);
			
			this.playerData = new BansheePlayerData(ServiceManager.PlayerEngine.ActiveEngine);

			this.glWidget.MakeCurrent();

			gl.glShadeModel(gl.GL_SMOOTH);
			gl.glEnable(gl.GL_LINE_SMOOTH);
			gl.glHint(gl.GL_LINE_SMOOTH_HINT, gl.GL_NICEST);

			gl.glEnable(gl.GL_BLEND);
			gl.glBlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA);
			
			while (this.loopRunning) {
				lock (this.glCallQueue) {
					while (this.glCallQueue.Count > 1)
						this.glCallQueue.Dequeue()();
				}
				
				if (this.playerData.Update(500))
				    ((IController) this).RenderFrame();
			}

			this.DisposeRenderer();
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();

			this.loopRunning = false;
		}


		protected virtual void OnVisualizationListChanged (object sender, System.EventArgs e)
		{
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

		IBeatDetector IController.BeatDetector {
			get { return null; }
		}

		PlayerData IController.PlayerData {
			get { return this.playerData; }
		}
		
		void IController.RenderFrame()
		{
			if (this.GdkWindow == null)
				return;

			Console.WriteLine("rendering");
			IRenderer r = this.renderer;
			if (r != null)
				r.Render(this);

			gl.glFlush();
			this.glWidget.SwapBuffers();
		}

		void IController.Resize(int width, int height)
		{
			this.width = width;
			this.height = height;

			lock (this.glCallQueue) {
				this.glCallQueue.Enqueue(delegate {
					gl.glViewport(0, 0, width, height);
		
					gl.glMatrixMode(gl.GL_PROJECTION);
					gl.glLoadIdentity();
					
					gl.glMatrixMode(gl.GL_MODELVIEW);
					gl.glLoadIdentity();
				});
			}
		}
#endregion
	}
}
