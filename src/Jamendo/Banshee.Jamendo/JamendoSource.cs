//
// JamendoSource.cs
//
// Authors:
//   Janez Troha <janez.troha@gmail.com>
//
// Copyright (C) 2010 Janez Troha
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

using Banshee.WebSource;

namespace Banshee.Jamendo
{
    public class JamendoSource : Banshee.WebSource.WebSource
    {
        public JamendoWebBrowserShell Shell { get; private set; }
        private JamendoView view;

        public JamendoSource () : base("Jamendo", 190, "Jamendo")
        {
            Properties.SetString ("Icon.Name", "jamendo");
        }

        // A count of 0 will be hidden in the source TreeView
        public override int Count {
            get { return 0; }
        }

        protected override WebBrowserShell GetWidget ()
        {
            view = new JamendoView ();
            return (Shell = new JamendoWebBrowserShell(view));
        }
    }
}
