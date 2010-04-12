//
// OpenVPService.cs
//
// Author:
//       Chris Howie <cdhowie@gmail.com>
//
// Copyright (c) 2009 Chris Howie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;

using Banshee.ServiceStack;
using Banshee.NowPlaying;
using Banshee.Sources;
using Gtk;
using Mono.Addins;

namespace Banshee.OpenVP
{
    public class OpenVPService : IExtensionService, IDisposable
    {
        private VisualizationDisplayWidget contents;
        private NowPlayingSource installedSource = null;

        public OpenVPService () { }

        #region IExtensionService implementation

        public void Initialize ()
        {
            // Hack alert!
            Addin visualizations = AddinManager.Registry.GetAddin("Banshee.OpenVP.Visualizations");
            if (visualizations != null)
                visualizations.Enabled = true;

            contents = new VisualizationDisplayWidget();

            ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            ServiceManager.SourceManager.SourceRemoved += OnSourceRemoved;

            InstallDisplay (ServiceManager.SourceManager.FindSources<NowPlayingSource>().FirstOrDefault());
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            InstallDisplay (args.Source as NowPlayingSource);
        }

        private void OnSourceRemoved (SourceEventArgs args)
        {
            if (args.Source == installedSource) {
                installedSource = null;
            }
        }

        private void InstallDisplay (NowPlayingSource nps)
        {
            if (installedSource == null && nps != null) {
                nps.SetSubstituteAudioDisplay (contents);
                installedSource = nps;
            }
        }

        #endregion

        #region IDisposable implementation

        public void Dispose ()
        {
            if (installedSource != null) {
                installedSource.SetSubstituteAudioDisplay (null);
                installedSource = null;
            }

            contents.Destroy ();
            contents.Dispose ();
            contents = null;
        }

        #endregion

        #region IService implementation

        public string ServiceName
        {
            get { return "OpenVPService"; }
        }

        #endregion
    }
}
