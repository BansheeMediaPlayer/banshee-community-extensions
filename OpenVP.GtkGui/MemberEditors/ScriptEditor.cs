// ScriptEditor.cs
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
using System.Reflection;
using OpenVP.Scripting;
using Gtk;

namespace OpenVP.GtkGui.MemberEditors {
	public class ScriptEditor : MemberEditor {
		private TextView mEditor;
		
		public ScriptEditor(object @object, PropertyInfo info) : base(@object, info) {
			ScrolledWindow window = new ScrolledWindow();
			
			this.mEditor = new TextView();
			this.mEditor.Show();
			this.mEditor.Buffer.Changed += this.OnEditorChanged;
			
			window.Add(this.mEditor);
			window.Show();
			window.ShadowType = ShadowType.In;
			
			this.Add(window);
			
			this.Revert();
		}
		
		private void OnEditorChanged(object o, EventArgs e) {
			this.FireMadeDirty();
		}
		
		public override void Revert() {
			UserScript script = (UserScript) this.PropertyInfo.GetValue(this.Object, null);
			
			this.mEditor.Buffer.Text =
				script.Script == null ? "" : script.Script;
			
			this.FireMadeClean();
		}
		
		public override void Apply() {
			UserScript script = (UserScript) this.PropertyInfo.GetValue(this.Object, null);
			
			script.Script = this.mEditor.Buffer.Text;
			
			this.FireMadeClean();
		}
	}
}
