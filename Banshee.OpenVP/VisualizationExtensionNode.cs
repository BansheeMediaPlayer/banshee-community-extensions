// VisualizationExtensionNode.cs created with MonoDevelop
// User: chris at 3:50 PM 8/22/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using Mono.Addins;
using OpenVP;

namespace Banshee.OpenVP
{
    public class VisualizationExtensionNode : ExtensionNode
    {
        [NodeAttribute]
        private string type = null;

        [NodeAttribute]
        private string label = null;

        private Type typeObject = null;

        public Type Type {
            get {
                if (this.typeObject == null) {
                    this.typeObject = this.Addin.GetType(this.type, true);
                }

                return this.typeObject;
            }
        }

        public string Label {
            get { return this.label; }
        }

        public IRenderer CreateObject()
        {
            return (IRenderer) Activator.CreateInstance(this.Type);
        }
    }
}
