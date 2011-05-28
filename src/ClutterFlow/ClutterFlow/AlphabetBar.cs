
using System;
using System.Collections.Generic;
using Clutter;
using ClutterFlow.Buttons;

namespace ClutterFlow.Alphabet
{

    public class AlphabetEventArgs : EventArgs
    {
        protected AlphabetChars letter = AlphabetChars.unknown;
        public AlphabetChars Letter {
            get { return letter; }
        }

        public AlphabetEventArgs (AlphabetChars letter) : base ()
        {
            this.letter = letter;
        }
    }


    public class AlphabetBar : Clutter.Group
    {

        #region Events
        public event EventHandler<AlphabetEventArgs> LetterClicked;
        protected void InvokeLetterClicked (AlphabetChars letter)
        {
            if (LetterClicked!=null) LetterClicked (this, new AlphabetEventArgs (letter));
        }
        #endregion

        #region Fields
        protected CairoTexture background;

        protected Dictionary<AlphabetChars, AlphabetButton> buttons = new Dictionary<AlphabetChars, AlphabetButton>(Enum.GetValues(typeof(AlphabetChars)).Length);
        public AlphabetButton this [AlphabetChars index] {
            get { return buttons[index]; }
        }

        protected double Margin {
            get { return Width * 0.075; }
        }

        #endregion Fields

        #region Initialisation
        public AlphabetBar (uint width, uint height)
        {
            this.SetSize (width, height);

            InitBackground ();
            InitButtons ();

            ShowAll ();
        }

        protected virtual void InitBackground ()
        {
            background = new CairoTexture ((uint) Width,(uint) Height);
            Add (background);

            SetupBackground ();
            background.Show ();
        }

        protected virtual void SetupBackground ()
        {
            background.Clear();
            Cairo.Context context = background.Create ();

            double lwidth = 1;
            double hlwidth = lwidth*0.5;

            double margin = Margin;

            //left curvature:
            context.MoveTo (-hlwidth, -hlwidth);
            context.CurveTo (margin*0.33, -hlwidth,
                             margin*0.5, Height*0.4,
                             margin*0.5, Height*0.5);
            context.CurveTo (margin*0.5, Height*0.6,
                             margin*0.66, Height-hlwidth,
                             margin-hlwidth, Height-hlwidth);


            //straight bottom:
            context.LineTo (Width-margin-hlwidth, Height-hlwidth);

            //right curvature:
            context.CurveTo (Width-margin*0.66, Height - hlwidth,
                             Width-margin*0.5, Height*0.6,
                             Width-margin*0.5, Height*0.5);
            context.CurveTo (Width-margin*0.5, Height*0.4,
                             Width-margin*0.33, -hlwidth,
                             Width-hlwidth, -hlwidth);

            //straight top:
            context.LineTo (-hlwidth, -hlwidth);
            context.ClosePath ();

            context.LineWidth = lwidth;
            context.SetSourceRGBA (1.0, 1.0, 1.0, 1.0);
            context.StrokePreserve ();
            context.SetSourceRGBA (1.0, 1.0, 1.0, 0.10);
            context.Fill ();

            ((IDisposable) context.Target).Dispose();
            ((IDisposable) context).Dispose();
        }

        protected virtual void InitButtons ()
        {
            Array values = Enum.GetValues(typeof(AlphabetChars));

            int x_step = (int) ((Width * 0.950) / values.Length);
            uint b_width = (uint) x_step;
            uint b_height = (uint) (Height - 2);

            int x = (int) (Margin*0.5f + x_step);
            int y = (int) (Height * 0.5)+2;

            foreach (AlphabetChars key in values) {
                buttons[key] = new AlphabetButton (b_width, b_height, key);
                buttons[key].ButtonReleaseEvent += HandleButtonReleaseEvent;
                Add (buttons[key]);
                buttons[key].SetAnchorPoint ((float) buttons[key].Width*0.5f, (float) buttons[key].Height*0.5f);
                buttons[key].SetPosition (x, y);
                x += x_step;
            }
        }
        #endregion

        #region Event Handling
        protected void HandleButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
        {
            if (o is  AlphabetButton) {
                InvokeLetterClicked ((o as AlphabetButton).Letter);
            }
        }
        #endregion
    }
}
