// 
// OpenVPSourceContentProvider.cs
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
using Banshee.Sources;
using Banshee.Sources.Gui;
using Gtk;

namespace Banshee.OpenVP
{
    public class OpenVPSourceContentProvider : SourceContentProvider, IDisposable
    {
        private OpenVPSourceContents contents = null;
        
        public OpenVPSourceContentProvider()
        {
            Id = "now-playing-openvp";
            Name = "OpenVP (music visualizer)";
        }
        
        public override bool Supports (Source source)
        {
            // This is lame but I'm too lazy to tell upstream to provide a .pc
            // for Banshee.NowPlaying.
            return source.GetType().Name == "NowPlayingSource";
        }
        
        public override ISourceContents CreateFor (Source source)
        {
            if (contents == null) {
                contents = new OpenVPSourceContents();
            }
            
            return contents;
        }
        
        void IDisposable.Dispose()
        {
            if (contents != null) {
	            contents.Destroy();
	            contents = null;
            }
        }
    }
    
    internal class OpenVPSourceContents : ISourceContents
    {
        private ISource source = null;
        
        private VisualizationDisplayWidget widget = new VisualizationDisplayWidget();
        
        internal OpenVPSourceContents()
        {
        }
        
        public void ResetSource ()
        {
            source = null;
        }
        
        public bool SetSource (ISource source)
        {
            this.source = source;
            return true;
        }
        
        public ISource Source {
            get { return source; }
        }
        
        public Widget Widget {
            get { return widget; }
        }
        
        public Widget HeaderExtension {
            get { return widget.HeaderExtension; }
        }
        
        public void Destroy()
        {
            if (widget != null) {
                widget.Destroy();
                widget.Dispose();
                widget = null;
            }
        }
    }
}
