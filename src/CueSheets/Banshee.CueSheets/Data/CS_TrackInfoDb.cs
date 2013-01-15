using System;
using Banshee.Database;
using Hyena.Data.Sqlite;

namespace Banshee.CueSheets
{
	public class CS_TrackInfoDb
	{
		private BansheeDbConnection _con;
		
		private readonly HyenaSqliteCommand _sql_check;
		private readonly HyenaSqliteCommand _sql_get;
		private readonly HyenaSqliteCommand _sql_insert;
		private readonly HyenaSqliteCommand _sql_update;
		
		
		public CS_TrackInfoDb (BansheeDbConnection con) {
			_con=con;
			_sql_check=new HyenaSqliteCommand("SELECT COUNT(*) FROM cuesheet_info WHERE key=?");
			_sql_get=new HyenaSqliteCommand("SELECT type,value FROM cuesheet_info WHERE key=?");
			_sql_insert=new HyenaSqliteCommand("INSERT INTO cuesheet_info VALUES(?,?,?)");
			_sql_update=new HyenaSqliteCommand("UPDATE cuesheet_info SET type=?, value=? WHERE key=?");
			try {
				if (!_con.TableExists ("cuesheet_info")) {
					_con.Query ("CREATE TABLE cuesheet_info(key varchar,type varchar,value varchar)");
					_con.Query ("CREATE INDEX cuesheet_idx1 ON cuesheet_info(key)");
				}
				
			} catch (System.Exception ex) {
				Hyena.Log.Information (ex.ToString ());
			}
		}
		
		private void iSet(string key,string type,string val) {
			try {
				int cnt=_con.Query<int> (_sql_check,key);
				//int cnt=(int) Convert.ChangeType (rdr[0],typeof(int));
				if (cnt==0) {
					_con.Execute(_sql_insert,key,type,val);
				} else {
					_con.Execute(_sql_update,type,val,key);
				}
			} catch(System.Exception ex) {
				Hyena.Log.Information (ex.ToString ());
			}
		}
		
		private bool iGet(string key,out string type,out string val) {
			type="";
			val="";
			try {
				int cnt=_con.Query<int>(_sql_check,key);
				if (cnt==0) {
					return false;
				} else {
					IDataReader rdr=_con.Query(_sql_get,key);
					rdr.Read ();
					type=rdr.Get<string>("type");
					val=rdr.Get<string>("value");
					return true;
				}
			} catch (System.Exception ex) {
				Hyena.Log.Information (ex.ToString ());
				return false;
			}
		}
		
		public void Set(string key,int val) {
			iSet (key,"int",val.ToString ());
		}
		
		public void Get(string key,out int val,int _default) {
			string t,v;
			if (iGet (key,out t,out v)) {
				if (t=="int") { 
					val=Convert.ToInt32 (v);
				} else {
					val=_default;
				}
			} else {
				val=_default;
			}
		}
		
		public void Set(string key,bool val) {
			iSet (key,"bool",val.ToString ());
		}
		
		public void Get(string key,out bool val,bool _default) {
			string t,v;
			if (iGet (key,out t,out v)) {
				if (t=="bool") {
					val=Convert.ToBoolean (v);
				} else {
					val=_default;
				}
			} else {
				val=_default;
			}
		}
			
	}
}

