//
// LiveRadioPluginConfigurationWidget.cs
//
// Authors:
//   Frank Ziegler <funtastix@googlemail.com>
//
// Copyright (C) 2010 Frank Ziegler
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
using System.Text.RegularExpressions;

using Gtk;

using Mono.Unix;

using Hyena;

namespace Banshee.LiveRadio
{


    public class LiveRadioPluginConfigurationWidget : VBox
    {

        private Label proxy_url_label;
        private Label credential_username_label;
        private Label credential_password_label;
        private Label http_timeout_label;
        private CheckButton use_proxy;
        private CheckButton use_credentials;

        private Entry credential_password_text = new Entry ();
        private Entry credential_username_text = new Entry ();
        private Entry proxy_url_text = new Entry ();
        private Entry timeout_seconds_text = new Entry ();

        public LiveRadioPluginConfigurationWidget (bool has_login) : base ()
        {
            Log.Debug ("[LiveRadioPluginConfigurationWidget]<Constructor> START");
            proxy_url_label = new Label (Catalog.GetString ("Proxy URL"));
            use_proxy = new CheckButton (Catalog.GetString ("Enable using a HTTP proxy server"));
            use_credentials = new CheckButton (Catalog.GetString ("Use Site Login"));
            credential_username_label = new Label (Catalog.GetString ("Username"));
            credential_password_label = new Label (Catalog.GetString ("Password"));
            http_timeout_label = new Label (Catalog.GetString ("HTTP timeout in seconds"));

            use_credentials.Toggled += OnCredentialsToggled;
            use_proxy.Toggled += OnProxyToggled;
            timeout_seconds_text.Changed += OnTimeoutTextChanged;

            Table table;

            if (has_login)
                table = new Table (6, 2, true);
            else
                table = new Table (3, 2, true);

            table.Attach (use_proxy, 0, 2, 0, 1);
            table.Attach (proxy_url_label, 0, 1, 1, 2);
            table.Attach (proxy_url_text, 1, 2, 1, 2);

            table.Attach (http_timeout_label, 0, 1, 2, 3);
            table.Attach (timeout_seconds_text, 1, 2, 2, 3);

            if (has_login)
            {
                table.Attach (use_credentials, 0, 2, 3, 4);
                table.Attach (credential_username_label, 0, 1, 4, 5);
                table.Attach (credential_username_text, 1, 2, 4, 5);
                table.Attach (credential_password_label, 0, 1, 5, 6);
                table.Attach (credential_password_text, 1, 2, 5, 6);
            }

            PackStart(table, false, false, 10);

            proxy_url_text.Sensitive = use_proxy.Active;
            credential_password_text.Sensitive = use_credentials.Active;
            credential_username_text.Sensitive = use_credentials.Active;

            Log.Debug ("[LiveRadioPluginConfigurationWidget]<Constructor> END");

        }

        void OnTimeoutTextChanged (object sender, EventArgs e)
        {
            string text = timeout_seconds_text.Text;
            Regex objNotNaturalPattern=new Regex("[^0-9]");
            timeout_seconds_text.Text = objNotNaturalPattern.Replace(text, "#").Replace("#",null);
        }

        void OnCredentialsToggled (object sender, EventArgs e)
        {
            if (use_credentials.Active)
            {
                credential_password_text.Sensitive = true;
                credential_username_text.Sensitive = true;
            } else {
                credential_password_text.Sensitive = false;
                credential_username_text.Sensitive = false;
            }
        }

        void OnProxyToggled (object sender, EventArgs e)
        {
            if (use_proxy.Active)
            {
                proxy_url_text.Sensitive = true;
            } else {
                proxy_url_text.Sensitive = false;
            }
        }

        public bool UseProxy
        {
            get { return use_proxy.Active; }
            set { use_proxy.Active = value; }
        }

        public bool UseCredentials
        {
            get { return use_credentials.Active; }
            set { use_credentials.Active = value; }
        }

        public string ProxyUrl
        {
            get { return proxy_url_text.Text.Trim (); }
            set { proxy_url_text.Text = value.Trim (); }
        }

        public string HttpUsername
        {
            get { return credential_username_text.Text; }
            set { credential_username_text.Text = value; }
        }

        public string HttpPassword
        {
            get { return credential_password_text.Text; }
            set { credential_password_text.Text = value; }
        }

        private int ValidateTimeout(string timeout)
        {
            try
            {
                int result = Int32.Parse(timeout);
                if (result < 10) return 10;
                return result;
            } catch {
                return 10;
            }
        }

        public int HttpTimeout
        {
            get { return ValidateTimeout (timeout_seconds_text.Text); }
            set { timeout_seconds_text.Text = value.ToString (); }
        }

    }
}
