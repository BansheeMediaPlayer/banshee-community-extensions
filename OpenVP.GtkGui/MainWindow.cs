using System;
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
		
		private volatile bool mInitialized = false;
		
		private volatile bool mLoopRunning = true;
		
		private volatile bool mRequireDataUpdate = true;
		
		private volatile bool mAllowMultipleUpdates = false;
		
		private object mPreset = null;
		
		private string mLastSave = null;
		
		public MainWindow() : base(Gtk.WindowType.Toplevel) {
			mSingleton = this;
			
			Build();
			
			this.mController = new Controller();
			this.mController.WindowClosed += delegate {
				this.Quit();
			};
			
			new Thread(this.ControllerLoop).Start();
			
			while (!this.mInitialized);
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
			this.mPreset = preset;
			
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