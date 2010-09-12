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

using Gtk;
using System;
using System.Text;

using Mono.Addins;
using System.Threading;
using Banshee.ServiceStack;
using Banshee.WebBrowser;

namespace Banshee.Lyrics.Gui
{
    public delegate void AddLinkClickedHandler (object o, EventArgs e);

    public class LyricsBrowser : OssiferWebView
    {
        private string lyrics_text;

        public event AddLinkClickedHandler AddLinkClicked;

        private bool insert_mode_available = true;

        public bool InsertModeAvailable {
            get { return insert_mode_available; }
            set { insert_mode_available = value; }
        }

        private bool IsValidUri (string uri)
        {
            if (uri == null || uri.Equals ("about:blank")) {
                return false;
            }
            return true;
        }

        protected override OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            if (!IsValidUri (uri)) {
                return OssiferNavigationResponse.Unhandled;
            }
            if (uri == AddinManager.CurrentLocalizer.GetString ("add")) {
                AddLinkClicked (this, null);
            } else {
                LyricsManager.Instance.FetchLyricsFromLyrc (uri);
            }
            return OssiferNavigationResponse.Accept;
        }

        public void OnRefresh ()
        {
            LyricsManager.Instance.RefreshLyrics (ServiceManager.PlayerEngine.CurrentTrack);
        }

        private const int TextType = 1;
        private const int UTF8_FORMAT = 8;
        private static readonly Gdk.Atom CLIPBOARD_ATOM = Gdk.Atom.Intern ("CLIPBOARD", false);

        public void CopyLyricsToClipboard ()
        {
            Clipboard clipboard = Clipboard.Get (CLIPBOARD_ATOM);
            var targets = new TargetList ();
            targets.AddTextTargets (TextType);
            clipboard.SetWithData ((Gtk.TargetEntry[])targets, ClipboardGetFunc, ClipboardClearFunc);
        }

        public void ClipboardGetFunc (Clipboard clipboard, SelectionData selection_data, uint info)
        {
            if (selection_data == null)
                return;
            switch (info) {
                case TextType:
                    selection_data.Text = Utils.ToNormalString (lyrics_text);
                    break;
            }
        }

        public void ClipboardClearFunc (Clipboard clipboard)
        { }

        public void LoadString (object o, LoadFinishedEventArgs args)
        {
            String browser_str;

            if (args.error != null) {
                browser_str = args.error;
            } else if (args.suggestion != null) {
                browser_str = GetSuggestionString (Utils.ToHtml (args.suggestion));
            } else if (args.lyrics != null) {
                browser_str = Utils.ToHtml(args.lyrics);
            } else {
                browser_str = AddinManager.CurrentLocalizer.GetString ("No lyrics found.");
            }

            LoadString (browser_str);
        }

        private string GetSuggestionString (string lyrics_suggestion)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append ("<b>" + AddinManager.CurrentLocalizer.GetString ("No lyrics found.") + "</b>");
            if (InsertModeAvailable) {
                sb.Append ("<br> <a href=\"" + AddinManager.CurrentLocalizer.GetString ("add") + "\">");
                sb.Append (AddinManager.CurrentLocalizer.GetString ("Click here to manually add a new lyric"));
                sb.Append ("</a>");
            }
            sb.Append ("<br><br>");
            sb.Append (AddinManager.CurrentLocalizer.GetString ("Suggestions:"));
            sb.Append (lyrics_suggestion);

            return sb.ToString ();
        }

        public void OnLoading (object o, EventArgs args)
        {
            String str = "<div style=\"valign:center;float:middle;font-weight:bold;font-size:13px\">"
                + AddinManager.CurrentLocalizer.GetString ("Loading...") + "</div>";
            LoadString(str);
        }

        public void LoadString (string str)
        {
            lyrics_text = str;

            if (str == null) {
                str = " ";
            }
            str = "<div style=\"margin-left:5px;font-size:12px\">" +  str + "</div>";
            LoadString (str, "text/html", "UTF-8", null);
        }
    }
}