using System;
using Banshee.Collection;
using Hyena.Collections;

namespace Banshee.CueSheets
{
	public class CS_PlayListsModel : BansheeListModel<CS_PlayList>
	{
		//private CueSheetsSource 	MySource;
		private CS_PlayListCollection  _pla;
		public delegate void 				Listener(CS_PlayList pls);
		private Listener					_listener=null;

        public CS_PlayListsModel (CS_PlayListCollection pls) {
			_pla=pls;
			Selection=new Hyena.Collections.Selection();
			Selection.Changed+=delegate(object sender,EventArgs args) {
				try {
					int index=((Selection) sender).FirstIndex;
					if (index>=0) {
						CS_PlayList pl=(CS_PlayList) this[index];
						Hyena.Log.Information ("playlist="+pl);
						OnPlayListSelect(pl);
					}
				} catch(System.Exception ex) {
					Hyena.Log.Error (ex.ToString ());
				}
			};
        }
		
		private void OnPlayListSelect(CS_PlayList pls) {
			if (_listener!=null) {
				_listener(pls);
			}
		}
		
		public void SetListener(Listener f) {
			_listener=f;
		}
		
		
		public CS_PlayListCollection Collection {
			get { return _pla; }
		}

        public override void Clear () {
			// does nothing 
        }
	
        public override void Reload () {
			_pla.Reload ();
			base.RaiseReloaded();
        }
	
        public override int Count {
            get { 
				return _pla.Count;
			}
        }
	
		public override CS_PlayList this[int index] {
			get {
				return _pla[index];
			} 	
		}
	}
}

