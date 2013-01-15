using System;
using Banshee.Collection;
using System.Collections.Generic;
using System.Collections;
using Hyena;

namespace Banshee.CueSheets
{
	public class CS_ComposerInfo : CacheableItem
	{
		private string name;
		private string name_sort;
		
		public CS_ComposerInfo ()
		{
		}
		
		private CueSheet _sheet;
		
		public class Comparer : IComparer<CS_ComposerInfo> {
			private CaseInsensitiveComparer cmp=new CaseInsensitiveComparer();
		    public int Compare( CS_ComposerInfo a1,CS_ComposerInfo  a2 )  {
				return cmp.Compare (a1.Name,a2.Name);
		    }
		}
		
		public CS_ComposerInfo (CueSheet s) {
			_sheet=s;
			if (s!=null) {
				Name=s.composer ();
			} else {
				Name="<All Composers>";
			}
		}
		
		public CueSheet getCueSheet() {
			return _sheet;
		}		
		
		public virtual string Name {
            get { return name; }
            set { name = value; }
        }

        public virtual string NameSort {
            get { return name_sort; }
            set { name_sort = String.IsNullOrEmpty (value) ? null : value; }
        }

        public string DisplayName {
            get { return StringUtil.MaybeFallback (Name, "Unknown Composer"); }
        }		
	}
}

