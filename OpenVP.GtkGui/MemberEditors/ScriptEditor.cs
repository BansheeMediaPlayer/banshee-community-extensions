// ScriptEditor.cs
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
using System.Reflection;
using OpenVP.Scripting;
using Gtk;

namespace OpenVP.GtkGui.MemberEditors {
	public class ScriptEditor : MemberEditor {
		private TextView mEditor;
		
		private TextTag mErrorTag;
		
		private Label mErrorLabel;
		
		public override AttachOptions YAttachment {
			get {
				return AttachOptions.Fill | AttachOptions.Expand;
			}
		}
		
		public ScriptEditor(object @object, PropertyInfo info) : base(@object, info) {
			VBox box = new VBox(false, 3);
			
			ScrolledWindow window = new ScrolledWindow();
			
			this.mEditor = new TextView();
			this.mEditor.Show();
			this.mEditor.Buffer.Changed += this.OnEditorChanged;
			this.mEditor.ModifyFont(Pango.FontDescription.FromString("monospace"));
			
			this.mErrorTag = new TextTag("compile error");
			this.mErrorTag.Underline = Pango.Underline.Error;
			
			this.mEditor.Buffer.TagTable.Add(this.mErrorTag);
			
			window.Add(this.mEditor);
			window.Show();
			window.ShadowType = ShadowType.In;
			
			this.mErrorLabel = new Label();
			this.mErrorLabel.Selectable = true;
			this.mErrorLabel.LineWrap = true;
			this.mErrorLabel.Xalign = 0;
			
			this.mErrorLabel.Style.FontDescription.Weight = Pango.Weight.Bold;
			this.mErrorLabel.ModifyFont(this.mErrorLabel.Style.FontDescription);
			
			box.Add(window);
			box.Add(this.mErrorLabel);
			box.Show();
			
			box.SizeAllocated += this.OnBoxSizeAllocated;
			
			this.SetSizeRequest(1, -1);
			
			this.Add(box);
			
			this.Revert();
		}
		
		private void OnBoxSizeAllocated(object o, SizeAllocatedArgs e) {
			this.mErrorLabel.SetSizeRequest(e.Allocation.Width, -1);
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
				
			this.mEditor.Buffer.RemoveTag(this.mErrorTag,
			                              this.mEditor.Buffer.StartIter,
			                              this.mEditor.Buffer.EndIter);
			
			this.mErrorLabel.Hide();
			
			script.Script = this.mEditor.Buffer.Text;
			try {
				script.Recompile();
			} catch (ScriptCompileException ex) {
				TextIter start, end;
				
				if (ex.Position == -1) {
					start = this.mEditor.Buffer.StartIter;
					end = this.mEditor.Buffer.EndIter;
				} else {
					start = this.mEditor.Buffer.GetIterAtOffset(ex.Position);
					
					end = start;
					if (!end.ForwardChar())
						start.BackwardChar();
				}
				
				this.mEditor.Buffer.ApplyTag(this.mErrorTag, start, end);
				
				this.mErrorLabel.Text = ex.Message;
				this.mErrorLabel.Show();
			}
			
			this.FireApplied();
			this.FireMadeClean();
		}
	}
}
