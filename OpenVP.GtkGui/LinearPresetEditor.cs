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
		
		public LinearPresetEditor(LinearPreset preset) {
			this.Build();
			
			this.mPreset = preset;
		}
		
		protected virtual void OnAddEffectClicked(object sender, System.EventArgs e) {
			new OpenVP.Core.ClearScreen();
			Registry.Update();
			
			EffectSelectorDialog dialog = new EffectSelectorDialog();
			dialog.Modal = true;
			dialog.TransientFor = (Window) this.Toplevel;
			
			if (dialog.Run() == (int) ResponseType.Ok) {
				Console.WriteLine(dialog.SelectedEffect.FullName);
			}
			
			dialog.Destroy();
		}
	}
}
