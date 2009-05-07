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
            Console.WriteLine("DISPOSED");
            contents.Destroy();
            contents = null;
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
