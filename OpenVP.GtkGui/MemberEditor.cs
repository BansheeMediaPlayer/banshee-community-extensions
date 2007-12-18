// MemberEditor.cs
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
using System.Reflection;

namespace OpenVP.GtkGui {
	public abstract partial class MemberEditor : Gtk.Bin {
		private PropertyInfo mInfo;
		
		private object mObject;
		
		protected PropertyInfo PropertyInfo {
			get {
				return this.mInfo;
			}
		}
		
		protected object Object {
			get {
				return this.mObject;
			}
		}
		
		protected MemberEditor(object @object, PropertyInfo info) {
			this.Build();
			
			this.mObject = @object;
			this.mInfo = info;
		}
		
		public event EventHandler MadeDirty;
		
		public event EventHandler MadeClean;
		
		protected void FireMadeDirty() {
			if (this.MadeDirty != null)
				this.MadeDirty(this, EventArgs.Empty);
		}
		
		protected void FireMadeClean() {
			if (this.MadeClean != null)
				this.MadeClean(this, EventArgs.Empty);
		}
		
		public abstract void Apply();
		
		public abstract void Revert();
		
		private static readonly Dictionary<Type, Type> mEditorMap =
			new Dictionary<Type, Type>();
		
		static MemberEditor() {
			mEditorMap[typeof(string)] = typeof(MemberEditors.StringEditor);
			mEditorMap[typeof(bool)] = typeof(MemberEditors.BooleanEditor);
			
			mEditorMap[typeof(byte)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(sbyte)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(short)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(ushort)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(int)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(uint)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(long)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(ulong)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(float)] = typeof(MemberEditors.NumericEditor);
			mEditorMap[typeof(double)] = typeof(MemberEditors.NumericEditor);
			
			mEditorMap[typeof(OpenVP.Color)] = typeof(MemberEditors.ColorEditor);
			
			mEditorMap[typeof(OpenVP.Scripting.UserScript)] = typeof(MemberEditors.ScriptEditor);
		}
		
		public static MemberEditor Create(object @object, PropertyInfo info) {
			Type editor;
			
			if (!mEditorMap.TryGetValue(info.PropertyType, out editor)) {
				editor = null;
				
				// Look for subclasses/interfaces.
				foreach (KeyValuePair<Type, Type> i in mEditorMap) {
					if (i.Key.IsValueType)
						continue;
					
					if (i.Key.IsAssignableFrom(info.PropertyType)) {
						editor = i.Value;
						break;
					}
				}
				
				if (editor == null)
					editor = typeof(MemberEditors.UnknownEditor);
			}
			
			return (MemberEditor) Activator.CreateInstance(editor, @object,
			                                               info);
		}
	}
}
