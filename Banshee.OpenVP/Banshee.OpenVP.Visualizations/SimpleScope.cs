// SimpleScope.cs created with MonoDevelop
// User: chris at 5:48 PM 8/22/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using gl = Tao.OpenGl.Gl;
using OpenVP;
using OpenVP.Core;

namespace Banshee.OpenVP.Visualizations
{
	public class SimpleScope : LinearPreset
	{
		public SimpleScope()
		{
			ClearScreen clear = new ClearScreen();
			clear.ClearColor = new Color(0, 0, 0, 0.075f);

			this.Effects.Add(clear);

			Scope scope = new Scope();
			scope.Color = new Color(0.25f, 0.5f, 1, 1);
			scope.LineWidth = 3;
			scope.Circular = true;

			this.Effects.Add(scope);

			Mirror mirror = new Mirror();

			this.Effects.Add(mirror);
		}
	}
}
