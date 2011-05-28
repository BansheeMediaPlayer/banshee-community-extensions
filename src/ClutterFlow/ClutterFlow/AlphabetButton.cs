
using System;

using Cairo;

using Clutter;
using ClutterFlow.Buttons;

namespace ClutterFlow.Alphabet
{


    public enum AlphabetChars {
        unknown=0x003F, A=0x0041, B=0x0042,
        C=0x0043, D=0x0044, E=0x0045,
        F=0x0046, G=0x0047, H=0x0048,
        I=0x0049, J=0x004A, K=0x004B,
        L=0x004C, M=0x004D, N=0x004E,
        O=0x004F, P=0x0050, Q=0x0051,
        R=0x0052, S=0x0053, T=0x0054,
        U=0x0055, V=0x0056, W=0x0057,
        X=0x0058, Y=0x0059, Z=0x005A
    }

    public class AlphabetButton : Buttons.ClutterButtonState
    {

        #region Fields
        protected override int MaxState {
            get { return 7; }
        }

        protected Text label;

        protected AlphabetChars letter = AlphabetChars.unknown;
        public virtual AlphabetChars Letter {
            get { return letter; }
            set {
                if (letter!=value) {
                    letter = value;
                    label.Value = ((char) letter).ToString ();
                    Update ();
                }
            }
        }

        public virtual bool Disabled {
            get { return (State & 4) == 4; }
            set {
                if (value) {
                    State |= 4;
                    Reactive = false;
                    BubbleEvents = false;
                } else {
                    State &= ~4;
                    Reactive = true;
                    BubbleEvents = true;
                }
                Update ();
            }
        }
        #endregion

        #region Initialization
        public AlphabetButton (uint width, uint height, AlphabetChars letter)
        {
            this.SetSize (width, height);
            this.letter = letter;

            Initialise ();
        }

        protected override void Initialise ()
        {
            base.Initialise ();
            this.Reactive = !Disabled;
            this.BubbleEvents = !Disabled;

            label = new Text(GetFontName (), ((char) letter).ToString (), GetFontColor ());
            Add (label);
            Update ();
        }

        #endregion

        #region Update
        public override void Update ()
        {
            this.Reactive = !Disabled;
            this.BubbleEvents = !Disabled;

            label.FontName = GetFontName ();
            label.SetAnchorPoint (label.Width*0.5f, label.Height*0.5f+2);
            label.SetPosition (this.Width*0.5f,this.Height*0.5f);
            label.SetColor (GetFontColor ());
            label.ShowAll();
        }

        Clutter.Color[] colors;

        protected string GetFontName ()
        {
            return "Sans " + ((state & 5)==1 ? "Bold" : "Normal") + " "  + ((int) ((float)Height*0.75f)).ToString ();
        }

        protected Clutter.Color GetFontColor ()
        {
            if (colors==null) {
                colors = new Clutter.Color[4];
                colors[0] = new Clutter.Color (1.0f, 1.0f, 1.0f, 0.6f);
                colors[1] = new Clutter.Color (1.0f, 1.0f, 1.0f, 0.75f);
                colors[2] = new Clutter.Color (1.0f, 1.0f, 1.0f, 0.9f);
                colors[3] = new Clutter.Color (0.0f, 0.0f, 0.0f, 0.75f);
            }
            if (state>=4)
                return colors[3];
            else if (state >= 2)
                return colors[2];
            else if (state == 1)
                return colors[1];
            else
                return colors[0];
        }
        #endregion
    }
}
