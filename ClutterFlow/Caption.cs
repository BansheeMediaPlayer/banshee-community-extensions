
using System;
using System.Runtime.InteropServices;
using Clutter;

namespace ClutterFlow
{
	
	
	public abstract class Caption : Clutter.Text
	{
		
		#region Fields
		public abstract string DefaultValue { get; set; }

		protected CoverManager coverManager;
		public abstract CoverManager CoverManager { get; set; }
		
		protected Animation aFade = null;
		#endregion
		
		public Caption (CoverManager coverManager, string font_name, Color color) : base (clutter_text_new ())
		{
			CoverManager = coverManager;
			Editable = false;
			Selectable = false;
			Activatable = false;
			CursorVisible = false;
			LineAlignment = Pango.Alignment.Center;
			FontName = font_name;
			SetColor (color);
		 	Value = DefaultValue;

			UpdatePosition ();
		}

		#region Methods
		[DllImport("libclutter-glx-1.0.so.0")]
		static extern IntPtr clutter_text_new ();
		
		public virtual void FadeOut ()
		{
			aFade = this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 0));
		}

		public virtual void FadeIn () 
		{
			EventHandler hFadeIn = delegate (object sender, EventArgs e) {
				this.Animatev ((ulong) AnimationMode.Linear.value__, (uint) (CoverManager.MaxAnimationSpan*0.5f), new string[] { "opacity" }, new GLib.Value ((byte) 255));
				aFade = null;
			};
			if (aFade!=null && aFade.Timeline.IsPlaying)
				aFade.Completed +=  hFadeIn;
			else
				hFadeIn (this, EventArgs.Empty);
		}

		public virtual void Update ()
		{
			UpdatePosition ();
		}
		
		public abstract void UpdatePosition ();
		#endregion
	}
}
