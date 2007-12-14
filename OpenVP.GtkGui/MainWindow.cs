using System;
using System.Collections.Generic;
using System.Threading;

using Gtk;
using OpenVP;

namespace OpenVP.GtkGui {
	public partial class MainWindow : Gtk.Window {
		private static MainWindow mSingleton = null;
		
		public static MainWindow Singleton {
			get {
				return mSingleton;
			}
		}
		
		private Controller mController;
		
		public Controller Controller {
			get {
				return this.mController;
			}
		}
		
		private volatile bool mInitialized = false;
		
		private volatile bool mLoopRunning = true;
		
		private volatile bool mRequireDataUpdate = true;
		
		private volatile bool mAllowMultipleUpdates = false;
		
		private object mPreset = null;
		
		private string mLastSave = null;
		
		private Queue<ThreadStart> mRenderLoopJoins = new Queue<ThreadStart>();
		
		public MainWindow() : base(Gtk.WindowType.Toplevel) {
			mSingleton = this;
			
			Build();
			
			this.mController = new Controller();
			this.mController.WindowClosed += delegate {
				this.Quit();
			};
			this.mController.PlayerData = new UDPPlayerData();
			
			new Thread(this.ControllerLoop).Start();
			
			while (!this.mInitialized);
		}
		
		public void InvokeOnRenderLoop(ThreadStart call) {
			lock (this.mRenderLoopJoins) {
				this.mRenderLoopJoins.Enqueue(call);
			}
		}
		
		public void InvokeOnRenderLoopAndWait(ThreadStart call) {
			ManualResetEvent wh = new ManualResetEvent(false);
			
			lock (this.mRenderLoopJoins) {
				this.mRenderLoopJoins.Enqueue(delegate {
					call();
					wh.Set();
				});
			}
			
			wh.WaitOne();
		}
		
		private void Quit() {
			this.mLoopRunning = false;
			
			Application.Quit();
		}
		
		private void ControllerLoop() {
			this.mController.Initialize();
			
			this.mInitialized = true;
			
			while (this.mLoopRunning) {
				// NullPlayerData carries the potential for infinite loops, so
				// just skip updating altogether if it's what we're using.
				if (!(this.mController.PlayerData is NullPlayerData)) {
					bool updated;
					
					if (this.mRequireDataUpdate) {
						this.mController.PlayerData.UpdateWait();
						updated = true;
					} else {
						updated = this.mController.PlayerData.Update();
					}
					
					if (updated) {
						// We test mAllowMultipleUpdates every time since there is
						// the potential for an infinite loop.  Testing it every
						// time allows the user to break the loop by disabling the
						// option.
						while (this.mAllowMultipleUpdates &&
						       this.mController.PlayerData.Update());
					}
				}
				
				if (this.mRenderLoopJoins.Count != 0) {
					lock (this.mRenderLoopJoins) {
						while (this.mRenderLoopJoins.Count != 0) {
							this.mRenderLoopJoins.Dequeue()();
						}
					}
				}
				
				this.mController.DrawFrame();
			}
		}
		
		protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
			this.Quit();
			a.RetVal = true;
		}
		
		protected virtual void OnQuitActivated(object sender, System.EventArgs e) {
			this.Quit();
		}
		
		private void NewPreset(IRenderer preset, Widget editor) {
			IDisposable old = this.mPreset as IDisposable;
			
			if (old != null) {
				this.InvokeOnRenderLoop(old.Dispose);
			}
			
			this.mPreset = preset;
			this.mController.Renderer = preset;
			
			if (this.PresetPane.Child != null)
				this.PresetPane.Child.Destroy();
			
			editor.Show();
			this.PresetPane.Add(editor);
		}
		
		protected virtual void OnLinearPresetActivated(object sender, System.EventArgs e) {
			LinearPreset preset = new LinearPreset();
			
			this.NewPreset(preset, new LinearPresetEditor(preset));
		}
		
		protected virtual void OnWaitForDataSliceToDrawToggled(object sender, System.EventArgs e) {
			this.mRequireDataUpdate = this.WaitForDataSliceToDraw.Active;
		}
		
		protected virtual void OnAllowSlicesToBeSkippedToggled(object sender, System.EventArgs e) {
			this.mAllowMultipleUpdates = this.AllowSlicesToBeSkipped.Active;
		}
	}
}