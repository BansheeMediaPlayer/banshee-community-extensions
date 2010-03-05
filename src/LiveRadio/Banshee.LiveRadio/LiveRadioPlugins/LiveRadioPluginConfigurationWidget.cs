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

using Mono.Addins;

using Hyena;

namespace Banshee.LiveRadio
{

    /// <summary>
    /// A basic Configuration Widget for a standard LiveRadio plugin
    ///
    /// contains configuration entries for
    /// - proxy
    /// - user credentials
    /// - HTTP timeout
    ///
    /// implements properties to easily access the configured elements
    /// </summary>
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

        /// <summary>
        /// Constructor -- builds the configuration widget according to parameters
        /// </summary>
        /// <param name="has_login">
        /// A <see cref="System.Boolean"/> -- whether to include user credentials configuration or not
        /// </param>
        public LiveRadioPluginConfigurationWidget (bool has_login) : base ()
        {
            proxy_url_label = new Label (AddinManager.CurrentLocalizer.GetString ("Proxy URL"));
            use_proxy = new CheckButton (AddinManager.CurrentLocalizer.GetString ("Enable using a HTTP proxy server"));
            use_credentials = new CheckButton (AddinManager.CurrentLocalizer.GetString ("Use Site Login"));
            credential_username_label = new Label (AddinManager.CurrentLocalizer.GetString ("Username"));
            credential_password_label = new Label (AddinManager.CurrentLocalizer.GetString ("Password"));
            http_timeout_label = new Label (AddinManager.CurrentLocalizer.GetString ("HTTP timeout in seconds"));

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
        }

        /// <summary>
        /// Ensures HTTP Timeout is always a number
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
        void OnTimeoutTextChanged (object sender, EventArgs e)
        {
            string text = timeout_seconds_text.Text;
            Regex objNotNaturalPattern=new Regex("[^0-9]");
            timeout_seconds_text.Text = objNotNaturalPattern.Replace(text, "#").Replace("#",null);
        }

        /// <summary>
        /// Toggles sensitivity of User Credential text fields according to activation of the
        /// corresponding checkbox
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
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

        /// <summary>
        /// Toggles sensitivity of the Proxy text field according to the activation of the
        /// corresponding checkbox
        /// </summary>
        /// <param name="sender">
        /// A <see cref="System.Object"/> -- not used
        /// </param>
        /// <param name="e">
        /// A <see cref="EventArgs"/> -- not used
        /// </param>
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
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    proxy_url_text.DeleteText(0, proxy_url_text.Text.Length);
                } else {
                    proxy_url_text.Text = value.Trim ();
                }
            }
        }

        public string HttpUsername
        {
            get { return credential_username_text.Text; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    credential_username_text.DeleteText(0, credential_username_text.Text.Length);
                } else {
                    credential_username_text.Text = value.Trim ();
                }
            }
        }

        public string HttpPassword
        {
            get { return credential_password_text.Text; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    credential_password_text.DeleteText(0, credential_password_text.Text.Length);
                } else {
                    credential_password_text.Text = value.Trim ();
                }
            }
        }

        /// <summary>
        /// Validate a timeout is given that makes sense and is a valid integer. Always at least 10 seconds.
        /// </summary>
        /// <param name="timeout">
        /// A <see cref="System.String"/> -- the timeout value entered by the user
        /// </param>
        /// <returns>
        /// A <see cref="System.Int32"/> -- a valid timeout integer value in seconds
        /// </returns>
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
            set { timeout_seconds_text.Text = ValidateTimeout (value.ToString ()).ToString (); }
        }

    }
}
