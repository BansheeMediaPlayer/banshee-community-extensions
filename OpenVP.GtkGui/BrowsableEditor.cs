// EffectEditor.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Gtk;
using OpenVP;

namespace OpenVP.GtkGui {
	using EffectMember = BrowsableItem<PropertyInfo>;
	
	public partial class BrowsableEditor : Gtk.Bin {
		private object mEffect;
		
		private List<EffectMember> mMembers = new List<EffectMember>();
		
		private Tooltips mTooltips = new Tooltips();
		
		private List<MemberEditor> mEditors = new List<MemberEditor>();
		
		private List<MemberEditor> mDirtyEditors = new List<MemberEditor>();
		
		private bool mSuspendCleanProcessing = false;
		
		public BrowsableEditor(object effect) {
			this.Build();
			
			this.mEffect = effect;
			
			this.BuildMemberList();
			this.BuildInterface();
		}
		
		private void BuildMemberList() {
			Type type = this.mEffect.GetType();
			
			foreach (PropertyInfo i in type.GetProperties(BindingFlags.Instance |
			                                              BindingFlags.Public)) {
				EffectMember member = EffectMember.Create(i);
				
				if (member != null)
					this.mMembers.Add(member);
			}
			
			EffectMember.Sort(this.mMembers);
		}
		
		private void BuildInterface() {
			string category = null;
			
			uint count = 0;
			
			foreach (EffectMember i in this.mMembers) {
				if (i.Category != category) {
					category = i.Category;
					count++;
				}
				
				count++;
			}
			
			category = null;
			
			Table table = new Table(count, 2, false);
			
			uint row = 0;
			
			foreach (EffectMember i in this.mMembers) {
				Label l;
				
				if (i.Category != category) {
					category = i.Category;
					
					l = new Label();
					l.Markup = "<b>" + category + "</b>";
					l.Show();
					
					table.Attach(l, 0, 2, row, ++row,
					             AttachOptions.Expand | AttachOptions.Fill,
					             AttachOptions.Shrink, 5, 5);
				}
				
				l = new Label(i.DisplayName);
				l.Show();
				table.Attach(l, 0, 1, row, row + 1, AttachOptions.Fill,
				             AttachOptions.Shrink, 5, 5);
				
				MemberEditor editor = MemberEditor.Create(this.mEffect, i.Item);
				editor.Show();
				
				editor.MadeClean += this.OnEditorMadeClean;
				editor.MadeDirty += this.OnEditorMadeDirty;
				this.mEditors.Add(editor);
				
				if (!string.IsNullOrEmpty(i.Description))
					this.mTooltips.SetTip(editor, i.Description, null);
				
				table.Attach(editor, 1, 2, row, ++row,
				             editor.XAttachment, editor.YAttachment, 5, 5);
			}
			
			this.SheetPane.AddWithViewport(table);
			table.Show();
		}
		
		private void OnEditorMadeDirty(object o, EventArgs e) {
			MemberEditor editor = (MemberEditor) o;
			
			if (!this.mDirtyEditors.Contains(editor)) {
				this.mDirtyEditors.Add(editor);
				
				this.RevertButton.Sensitive = true;
				this.ApplyButton.Sensitive = true;
			}
		}
		
		private void OnEditorMadeClean(object o, EventArgs e) {
			if (this.mSuspendCleanProcessing)
				return;
			
			MemberEditor editor = (MemberEditor) o;
			
			if (this.mDirtyEditors.Remove(editor) &&
			    this.mDirtyEditors.Count == 0) {
				this.RevertButton.Sensitive = false;
				this.ApplyButton.Sensitive = false;
			}
		}
		
		protected override void OnDestroyed() {
			base.OnDestroyed();
			
			this.mTooltips.Destroy();
		}

		protected virtual void OnApplyButtonClicked(object sender, System.EventArgs e) {
			this.mSuspendCleanProcessing = true;
			
			lock (MainWindow.Singleton.RenderLock) {
				foreach (MemberEditor i in this.mDirtyEditors)
					i.Apply();
			}
			
			this.mDirtyEditors.Clear();
			this.ApplyButton.Sensitive = false;
			this.RevertButton.Sensitive = false;
			
			this.mSuspendCleanProcessing = false;
		}

		protected virtual void OnRevertButtonClicked(object sender, System.EventArgs e) {
			this.mSuspendCleanProcessing = true;
			
			foreach (MemberEditor i in this.mDirtyEditors)
				i.Revert();
			
			this.mDirtyEditors.Clear();
			this.ApplyButton.Sensitive = false;
			this.RevertButton.Sensitive = false;
			
			this.mSuspendCleanProcessing = false;
		}
	}
}
