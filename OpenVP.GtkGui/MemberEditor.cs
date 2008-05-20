// MemberEditor.cs
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
using System.Reflection;
using Gtk;
using OpenVP.GtkGui.MemberEditors;

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
		
		public virtual AttachOptions XAttachment {
			get {
				return AttachOptions.Fill | AttachOptions.Expand;
			}
		}
		
		public virtual AttachOptions YAttachment {
			get {
				return AttachOptions.Fill;
			}
		}
		
		protected MemberEditor(object @object, PropertyInfo info) {
			this.Build();
			
			this.mObject = @object;
			this.mInfo = info;
		}
		
		public event EventHandler MadeDirty;
		
		public event EventHandler MadeClean;
		
		public event EventHandler Applied;
		
		protected void FireMadeDirty() {
			if (this.MadeDirty != null)
				this.MadeDirty(this, EventArgs.Empty);
		}
		
		protected void FireMadeClean() {
			if (this.MadeClean != null)
				this.MadeClean(this, EventArgs.Empty);
		}
		
		protected void FireApplied() {
			if (this.Applied != null)
				this.Applied(this, EventArgs.Empty);
		}
		
		public abstract void Apply();
		
		public abstract void Revert();
		
		private static readonly Dictionary<Type, Type> mEditorMap =
			new Dictionary<Type, Type>();
		
		static MemberEditor() {
			mEditorMap[typeof(string)] = typeof(StringEditor);
			mEditorMap[typeof(bool)] = typeof(BooleanEditor);
			
			mEditorMap[typeof(byte)] = typeof(NumericEditor);
			mEditorMap[typeof(sbyte)] = typeof(NumericEditor);
			mEditorMap[typeof(short)] = typeof(NumericEditor);
			mEditorMap[typeof(ushort)] = typeof(NumericEditor);
			mEditorMap[typeof(int)] = typeof(NumericEditor);
			mEditorMap[typeof(uint)] = typeof(NumericEditor);
			mEditorMap[typeof(long)] = typeof(NumericEditor);
			mEditorMap[typeof(ulong)] = typeof(NumericEditor);
			mEditorMap[typeof(float)] = typeof(NumericEditor);
			mEditorMap[typeof(double)] = typeof(NumericEditor);
			
			mEditorMap[typeof(OpenVP.Color)] = typeof(ColorEditor);
			
			mEditorMap[typeof(OpenVP.Scripting.UserScript)] = typeof(ScriptEditor);
		}
		
		public static MemberEditor Create(object @object, PropertyInfo info) {
			Type editor;
			
			if (!mEditorMap.TryGetValue(info.PropertyType, out editor)) {
				editor = null;
				
				// Is it an enum?
				if (info.PropertyType.IsEnum) {
					editor = typeof(EnumEditor);
				} else {
					// Look for subclasses/interfaces.
					foreach (KeyValuePair<Type, Type> i in mEditorMap) {
						if (i.Key.IsValueType)
							continue;
						
						if (i.Key.IsAssignableFrom(info.PropertyType)) {
							editor = i.Value;
							break;
						}
					}
				}
				
				if (editor == null)
					editor = typeof(UnknownEditor);
			}
			
			return (MemberEditor) Activator.CreateInstance(editor, @object,
			                                               info);
		}
	}
}
