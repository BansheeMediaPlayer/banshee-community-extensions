//  
// Author:
//   Christian Martellini <christian.martellini@gmail.com>
//
// Copyright (C) 2009 Christian Martellini
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Net;

using GConf;

namespace Banshee.Lyrics.Network
{
    public class ProxyManager
    {
        private static GConf.Client gconf = new GConf.Client ();
        
        private static ProxyManager instance = new ProxyManager ();
        
        internal static ProxyManager Instance {
            get { return instance; }
        }
        
        public WebProxy getProxy (string url)
        {
            string userName = null;
            string password = null;
            string proxyAddress = null;
            int proxyPort = 0;
            
            try {
                userName = getProxyUserName ();
                password = getProxyPassword ();
                proxyAddress = getProxyAddress ();
                proxyPort = getProxyPort ();
            } catch (GConf.NoSuchKeyException) {
                return null;
            }
            
            CredentialCache credcache = new CredentialCache ();
            NetworkCredential netcred = new NetworkCredential (userName, password);
            credcache.Add (new Uri (url), "BASIC", netcred);
            WebProxy myProxy = new WebProxy (proxyAddress, proxyPort);
            myProxy.Credentials = credcache;
            
            return myProxy;
        }
        
        public bool isHttpProxy ()
        {
            bool retval = false;
            try {
                retval = (bool) gconf.Get ("/system/http_proxy/use_http_proxy");
            }
            catch (GConf.NoSuchKeyException) {
            }
            return retval;
        }
        
        private string getProxyAddress ()
        {
            string retval = "";
            retval = (string) gconf.Get ("/system/http_proxy/host");
            return retval;
        }
        
        private int getProxyPort ()
        {
            int retval = 0;
            retval = (int) gconf.Get ("/system/http_proxy/port");
            return retval;
        }
        
        private string getProxyUserName ()
        {
            string retval = "";
            retval = (string) gconf.Get ("/system/http_proxy/authentication_user");
            return retval;
        }
        
        private string getProxyPassword ()
        {
            string retval = "";
            retval = (string) gconf.Get ("/system/http_proxy/authentication_password");
            return retval;
        }
    }
}
