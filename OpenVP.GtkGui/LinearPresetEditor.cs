// LinearPresetEditor.cs
//
//  Copyright (C) 2007 Chris Howie
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

using System;
using Gtk;

namespace OpenVP.GtkGui {
	public partial class LinearPresetEditor : Gtk.Bin {
		private LinearPreset mPreset;
		
		private ListStore mEffectStore = new ListStore(typeof(Effect));
		
		public LinearPresetEditor(LinearPreset preset) {
			this.Build();
			
			this.mPreset = preset;
			
			this.SyncStore();
			
			this.EffectList.AppendColumn("Effect", new CellRendererText(),
			                             new TreeCellDataFunc(EffectFunc));
			this.EffectList.Model = this.mEffectStore;
			this.EffectList.Selection.Changed += this.OnSelectionChanged;
		}
		
		private void OnSelectionChanged(object o, EventArgs e) {
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
			editor.Show();
			this.EffectPane.Add(editor);
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
				
				MainWindow.Singleton.InvokeOnRenderLoopAndWait(delegate {
					this.mPreset.Effects.Add(effect);
				});
				
				this.mEffectStore.AppendValues(effect);
			}
			
			dialog.Destroy();
		}

		protected virtual void OnRemoveEffectClicked(object sender, System.EventArgs e) {
			TreeIter i;
			
			if (this.EffectList.Selection.GetSelected(out i)) {
				TreePath path = this.mEffectStore.GetPath(i);
				
				int index = path.Indices[0];
				
				Effect eff = this.mPreset.Effects[index];
				
				MainWindow.Singleton.InvokeOnRenderLoopAndWait(delegate {
					eff.Dispose();
					this.mPreset.Effects.RemoveAt(index);
				});
				
				this.mEffectStore.Remove(ref i);
			}
		}
	}
}
