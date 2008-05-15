// ProxyManager.cs created with MonoDevelop
// User: lilith at 22:00 08/11/2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using GConf;
using System.Net;

namespace Banshee.Plugins.Lyrics
{
	
	
	public class ProxyManager	{
		private static ProxyManager instance=null;
		private static GConf.Client gconf = new GConf.Client();
		
		private ProxyManager()
		{
		}
		
		public static ProxyManager getInstance(){
			if(instance == null){
				instance = new ProxyManager();
			}
			return instance;
		}
		
		public WebProxy getProxy(string url){
			string userName=null;
			string password=null;
			string proxyAddress=null;
			int proxyPort=0;
			try{
				userName=getProxyUserName();
				password=getProxyPassword();
				proxyAddress=getProxyAddress();
				proxyPort=getProxyPort();
			}
			catch(GConf.NoSuchKeyException){
				return null;
			}
			CredentialCache credcache = new CredentialCache();
			NetworkCredential netcred = new NetworkCredential(userName, password);
			credcache.Add(new Uri(url), "BASIC", netcred);
			WebProxy myProxy = new WebProxy(proxyAddress, proxyPort);
			myProxy.Credentials = credcache;
			return myProxy;
		}
		
		public bool isHttpProxy(){
			bool retval = false;
			try{
				retval= (bool)gconf.Get("/system/http_proxy/use_http_proxy");
			}catch(GConf.NoSuchKeyException){
			}
			return retval;			
		}
		
		private string getProxyAddress(){
			string retval="";
			retval= (string)gconf.Get("/system/http_proxy/host");
			return retval;
		}
		
		private int getProxyPort(){
			int retval=0;
			retval= (int)gconf.Get("/system/http_proxy/port");
			return retval;
		}
		
		private string getProxyUserName(){
			string retval="";
			retval= (string)gconf.Get("/system/http_proxy/authentication_user");
			return retval;
		}
		
		private string getProxyPassword(){
			string retval="";
			retval= (string)gconf.Get("/system/http_proxy/authentication_password");
			return retval;
		}
	}
}
