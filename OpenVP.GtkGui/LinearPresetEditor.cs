// LinearPresetEditor.cs
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
using Gtk;

namespace OpenVP.GtkGui {
	public partial class LinearPresetEditor : Gtk.Bin {
		private LinearPreset mPreset;
		
		private ListStore mEffectStore = new ListStore(typeof(Effect));
		
		private KeyEntry KeybindKeyEntry;
		
		public LinearPresetEditor(LinearPreset preset) {
			this.Build();
			
			this.mPreset = preset;
			
			this.SyncStore();
			
			this.EffectList.AppendColumn("Effect", new CellRendererText(),
			                             new TreeCellDataFunc(EffectFunc));
			this.EffectList.Model = this.mEffectStore;
			this.EffectList.Selection.Changed += this.OnSelectionChanged;
			
			this.KeybindKeyEntry = new KeyEntry();
			this.KeyEntryAlign.Add(this.KeybindKeyEntry);
			this.KeybindKeyEntry.Show();
		}
		
		private void OnSelectionChanged(object o, EventArgs e) {
			this.CheckMovementButtons();
			
			TreeIter i;
			
			if (this.EffectPane.Child != null)
				this.EffectPane.Child.Destroy();
			
			if (!this.EffectList.Selection.GetSelected(out i)) {
				this.RemoveEffect.Sensitive = false;
				return;
			}
			
			this.RemoveEffect.Sensitive = true;
			
			Effect effect = (Effect) this.mEffectStore.GetValue(i, 0);
			
			BrowsableEditor editor = new BrowsableEditor(effect);
			editor.Changed += this.OnEditorChanged;
			editor.Show();
			this.EffectPane.Add(editor);
		}
		
		private void CheckMovementButtons() {
			TreeIter i;
			
			if (!this.EffectList.Selection.GetSelected(out i)) {
				this.UpButton.Sensitive = false;
				this.DownButton.Sensitive = false;
				
				return;
			}
			
			TreePath path = this.mEffectStore.GetPath(i);
			
			this.UpButton.Sensitive = path.Indices[0] != 0;
			this.DownButton.Sensitive = this.mEffectStore.IterNext(ref i);
		}
		
		private void OnEditorChanged(object o, EventArgs e) {
			TreeIter i;
			
			if (this.EffectList.Selection.GetSelected(out i))
				this.mEffectStore.EmitRowChanged(this.mEffectStore.GetPath(i), i);
		}
		
		private void SyncStore() {
			this.mEffectStore.Clear();
			
			foreach (Effect i in this.mPreset.Effects)
				this.mEffectStore.AppendValues(i);
		}
		
		private static void EffectFunc(TreeViewColumn col, CellRenderer r,
		                               TreeModel m, TreeIter i) {
			Effect e = (Effect) m.GetValue(i, 0);
			
			string text;
			
			if (string.IsNullOrEmpty(e.Name))
				text = e.Title;
			else
				text = string.Format("{0} ({1})", e.Title, e.Name);
			
			((CellRendererText) r).Text = text;
		}
		
		protected virtual void OnAddEffectClicked(object sender, System.EventArgs e) {
			// EVIL.  This makes sure the core assembly is loaded.  We should do
			// this better and only once.
			new OpenVP.Core.ClearScreen();
			
			Registry.Update();
			
			EffectSelectorDialog dialog = new EffectSelectorDialog();
			dialog.Modal = true;
			dialog.TransientFor = (Window) this.Toplevel;
			
			if (dialog.Run() == (int) ResponseType.Ok) {
				Effect effect;
				
				try {
					effect = (Effect) Activator.CreateInstance(dialog.SelectedEffect);
				} catch (Exception ex) {
					Console.WriteLine(ex.ToString());
					dialog.Destroy();
					return;
				}
				
				lock (MainWindow.Singleton.RenderLock) {
					this.mPreset.Effects.Add(effect);
				}
				
				this.mEffectStore.AppendValues(effect);
				this.CheckMovementButtons();
			}
			
			dialog.Destroy();
		}

		protected virtual void OnRemoveEffectClicked(object sender, System.EventArgs e) {
			TreeIter i;
			
			if (this.EffectList.Selection.GetSelected(out i)) {
				TreePath path = this.mEffectStore.GetPath(i);
				
				int index = path.Indices[0];
				
				Effect eff = this.mPreset.Effects[index];
				
				MainWindow.Singleton.InvokeOnRenderLoop(eff.Dispose);
				
				lock (MainWindow.Singleton.RenderLock) {
					this.mPreset.Effects.RemoveAt(index);
				};
				
				this.mEffectStore.Remove(ref i);
			}
		}
		
		private void MoveSelectedEffect(int direction) {
			TreeIter i;
			if (!this.EffectList.Selection.GetSelected(out i))
				return;
			
			TreePath p = this.mEffectStore.GetPath(i);
			int index = p.Indices[0];
			
			lock (MainWindow.Singleton.RenderLock) {
				TreeIter prev;
				if (!this.mEffectStore.IterNthChild(out prev,
				                                    index + direction))
					return;
				
				if (direction == 1)
					this.mEffectStore.MoveAfter(i, prev);
				else
					this.mEffectStore.MoveBefore(i, prev);
				
				Effect effect = this.mPreset.Effects[index];
				
				this.mPreset.Effects.RemoveAt(index);
				this.mPreset.Effects.Insert(index + direction, effect);
			}
			
			this.CheckMovementButtons();
		}
		
		protected virtual void OnUpButtonClicked(object sender, System.EventArgs e) {
			this.MoveSelectedEffect(-1);
		}
		
		protected virtual void OnDownButtonClicked(object sender, System.EventArgs e) {
			this.MoveSelectedEffect(1);
		}

		protected virtual void OnKeybindEventCheckToggled (object sender, System.EventArgs e) {
			this.KeybindEventCheck.Label = this.KeybindEventCheck.Active ?
				"On press" : "On release";
		}
	}
}
