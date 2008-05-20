// NumericEditor.cs
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
using Gtk;

using OpenVP.Metadata;

namespace OpenVP.GtkGui.MemberEditors {
	public class NumericEditor : MemberEditor {
		private SpinButton mSpin;
		
		public NumericEditor(object @object, PropertyInfo info) : base(@object, info) {
			RangeAttribute range = Util.GetAttribute<RangeAttribute>(info, false);
			
			double min, max;
			
			if (range == null) {
				FieldInfo field = info.PropertyType.GetField("MinValue",
				                                             BindingFlags.Public |
				                                             BindingFlags.Static);
				
				if (field == null)
					min = double.MinValue;
				else
					min = Convert.ToDouble(field.GetValue(null));
				
				field = info.PropertyType.GetField("MaxValue",
				                                   BindingFlags.Public |
				                                   BindingFlags.Static);
				
				if (field == null)
					max = double.MaxValue;
				else
					max = Convert.ToDouble(field.GetValue(null));
			} else {
				min = range.Minimum;
				max = range.Maximum;
			}
			
			this.mSpin = new SpinButton(min, max, 1);
			
			if (info.PropertyType == typeof(float) ||
			    info.PropertyType == typeof(double))
				this.mSpin.Digits = 5;
			else
				this.mSpin.Digits = 0;
			
			this.Revert();
			
			this.mSpin.Changed += this.OnSpinChanged;
			this.mSpin.ValueChanged += this.OnSpinChanged;
			
			this.mSpin.Show();
			this.Add(this.mSpin);
		}
		
		private void OnSpinChanged(object o, EventArgs e) {
			this.FireMadeDirty();
		}
		
		public override void Apply() {
			this.PropertyInfo.SetValue(this.Object, 
			                           Convert.ChangeType(this.mSpin.Value,
			                                              this.PropertyInfo.PropertyType),
			                           null);
			
			this.FireApplied();
			this.FireMadeClean();
		}
		
		public override void Revert() {
			this.mSpin.Value = Convert.ToDouble(this.PropertyInfo.GetValue(this.Object, null));
			
			this.FireMadeClean();
		}
	}
}
