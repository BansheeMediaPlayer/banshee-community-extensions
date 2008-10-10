// OpenVPSource.cs created with MonoDevelop
// User: chris at 4:14 PM 8/22/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Mono.Unix;

namespace Banshee.OpenVP
{
    public class OpenVPSource : Source, IDisposable
    {
        private VisualizationDisplayWidget visualizationDisplay = null;

        public OpenVPSource() : base("openvp", Catalog.GetString("Visualization"), 10, "openvp")
        {
            this.visualizationDisplay = new VisualizationDisplayWidget();
            
            this.Properties.SetString("Icon.Name", "applications-multimedia");
            this.Properties.Set<bool>("Nereid.SourceContents.HeaderVisible", false);
            this.Properties.Set<ISourceContents>("Nereid.SourceContents", this.visualizationDisplay);

            ServiceManager.SourceManager.AddSource(this);
        }

        public void Dispose()
        {
            if (this.visualizationDisplay != null) {
                this.visualizationDisplay.Destroy();
                this.visualizationDisplay.Dispose();
                this.visualizationDisplay = null;
            }
        }
    }
}
