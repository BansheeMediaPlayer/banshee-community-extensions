// MainWindow.cs
//
//  Copyright (C) 2007-2008 Chris Howie
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
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
		
		private SDLController mController;
		
		public SDLController Controller {
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
		
		private bool mLastSaveBinary = false;
		
		private Queue<ThreadStart> mRenderLoopJoins = new Queue<ThreadStart>();
		
		private volatile int mFPS = 0;
		
		private FileFilter mBinaryFilter = new FileFilter();
		
		private FileFilter mSoapFilter = new FileFilter();
		
		private Thread mUpdaterThread;
		
		private AutoResetEvent mRenderSync = new AutoResetEvent(false);
		
		private AutoResetEvent mUpdateSync = new AutoResetEvent(false);
		
		private object mRenderLock = new object();
		
		public object RenderLock {
			get {
				return this.mRenderLock;
			}
		}
		
		public MainWindow() : base(Gtk.WindowType.Toplevel) {
			mSingleton = this;
			
			Build();
			
			this.mController = new SDLController();
			this.mController.Closed += delegate {
				this.Quit();
			};
			this.mController.PlayerData = new UDPPlayerData();
			
			new Thread(this.ControllerLoop).Start();
			
			this.mUpdaterThread = new Thread(this.UpdaterLoop);
			this.mUpdaterThread.Start();
			
			this.StatusBar.Push(0, "");
			
			GLib.Timeout.Add(1000, this.FPSLoop);
			
			this.mSoapFilter.Name = "OpenVP preset (XML)";
			this.mSoapFilter.AddPattern("*.ovp");
			
			this.mBinaryFilter.Name = "OpenVP preset (binary)";
			this.mBinaryFilter.AddPattern("*.ovpb");
			
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
		
		public void Quit() {
			this.mLoopRunning = false;
			this.mUpdaterThread.Abort();
			
			Application.Quit();
		}
		
		private bool FPSLoop() {
			this.StatusBar.Pop(0);
			this.StatusBar.Push(0, this.mFPS + " FPS");
			this.mFPS = 0;
			
			return this.mLoopRunning;
		}
		
		private void UpdaterLoop() {
			try {
				while (this.mLoopRunning) {
					// NullPlayerData carries the potential for infinite loops, so
					// just skip updating altogether if it's what we're using.
					if (!(this.mController.PlayerData is NullPlayerData)) {
						bool updated = this.mController.PlayerData.Update(this.mRequireDataUpdate ? -1 : 0);
						
						if (updated) {
							// We test mAllowMultipleUpdates every time since there is
							// the potential for an infinite loop.  Testing it every
							// time allows the user to break the loop by disabling the
							// option.
							while (this.mAllowMultipleUpdates &&
							       this.mController.PlayerData.Update(0));
						}
					}
					
					this.mUpdateSync.Set();
					
					this.mRenderSync.WaitOne();
				}
			} catch (ThreadAbortException) {
				this.mUpdateSync.Set();
			}
		}
		
		private void ControllerLoop() {
			this.mController.Initialize();
			
			this.mInitialized = true;
			
			try {
				while (this.mLoopRunning) {
					do {
						if (this.mRenderLoopJoins.Count != 0) {
							lock (this.mRenderLoopJoins) {
								while (this.mRenderLoopJoins.Count != 0) {
									this.mRenderLoopJoins.Dequeue()();
								}
							}
						}
					} while (!this.mUpdateSync.WaitOne(100, false));
					
					lock (this.mRenderLock) {
						this.mController.RenderFrame();
					}
					
					this.mRenderSync.Set();
					
					this.mFPS++;
				}
			} finally {
				this.mController.Destroy();
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
			
			this.save.Sensitive = true;
			this.saveAs.Sensitive = true;
			this.mLastSave = null;
			
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

		protected virtual void OnSaveAsActivated(object sender, System.EventArgs e) {
			FileChooserDialog dialog = new FileChooserDialog("Save As", this,
			                                                 FileChooserAction.Save,
			                                                 Stock.Cancel, ResponseType.Cancel,
			                                                 Stock.Save, ResponseType.Ok);
			
			dialog.AddFilter(this.mSoapFilter);
			dialog.AddFilter(this.mBinaryFilter);
			
			if (dialog.Run() == (int) ResponseType.Ok) {
				string name = dialog.Filename;
				
				bool binary = dialog.Filter == this.mBinaryFilter;
				
				if (!System.IO.Path.HasExtension(name)) {
					if (dialog.Filter == this.mBinaryFilter) {
						name = System.IO.Path.ChangeExtension(name, "ovpb");
					} else {
						name = System.IO.Path.ChangeExtension(name, "ovp");
					}
				}
				
				this.SavePreset(name, binary);
				this.mLastSave = name;
				this.mLastSaveBinary = binary;
			}
			
			dialog.Destroy();
		}
		
		private void SavePreset(string where, bool binary) {
			using (FileStream fs = new FileStream(where, FileMode.Create, FileAccess.Write)) {
				if (binary)
					new BinaryFormatter().Serialize(fs, this.mPreset);
				else
					new SoapFormatter().Serialize(fs, this.mPreset);
			}
		}
		
		protected virtual void OnOpenActivated(object sender, System.EventArgs e) {
			FileChooserDialog dialog = new FileChooserDialog("Open", this,
			                                                 FileChooserAction.Open, 
			                                                 Stock.Cancel, ResponseType.Cancel,
			                                                 Stock.Open, ResponseType.Ok);
			
			dialog.AddFilter(this.mSoapFilter);
			dialog.AddFilter(this.mBinaryFilter);
			
			if (dialog.Run() == (int) ResponseType.Ok) {
				string name = dialog.Filename;
				
				object o;
				
				bool binary = false;
				
				using (FileStream file = File.Open(name, FileMode.Open, FileAccess.Read)) {
					try {
						o = new BinaryFormatter().Deserialize(file);
						binary = true;
					} catch {
						file.Seek(0, SeekOrigin.Begin);
						
						try {
							o = new SoapFormatter().Deserialize(file);
						} catch (Exception ex) {
							Console.WriteLine(ex.ToString());
							o = null;
						}
					}
				}
				
				LinearPreset preset = o as LinearPreset;
				
				if (preset == null) {
					MessageDialog md = new MessageDialog(this, DialogFlags.Modal,
					                                     MessageType.Error,
					                                     ButtonsType.Ok,
					                                     "Unable to load that preset.");
					
					md.Run();
					md.Destroy();
				} else {
					// TODO: Handle different preset types.
					this.NewPreset(preset, new LinearPresetEditor(preset));
					
					this.mLastSave = name;
					this.mLastSaveBinary = binary;
				}
			}
			
			dialog.Destroy();
		}
		
		protected virtual void OnSaveActivated(object sender, System.EventArgs e) {
			if (this.mLastSave != null)
				this.SavePreset(this.mLastSave, this.mLastSaveBinary);
			else
				this.saveAs.Activate();
		}
	}
}