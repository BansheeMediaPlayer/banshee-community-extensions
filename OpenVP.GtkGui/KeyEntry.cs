// KeyEntry.cs
//
//  Copyright (C) 2007-2008 Chris Howie
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using System.Collections.Generic;
using System.Text;
using Gtk;
using Gdk;
using Tao.Sdl;

namespace OpenVP.GtkGui {
	using GKey = Gdk.Key;
	
	public class KeyEntry : Entry {
		private static Dictionary<GKey, int> mKeyToSDLK =
			new Dictionary<GKey, int>();
		
		private static Dictionary<int, GKey> mSDLKToKey =
			new Dictionary<int, GKey>();
		
		static KeyEntry() {
			mSDLKToKey[Sdl.SDLK_0] = GKey.Key_0;
			mSDLKToKey[Sdl.SDLK_1] = GKey.Key_1;
			mSDLKToKey[Sdl.SDLK_2] = GKey.Key_2;
			mSDLKToKey[Sdl.SDLK_3] = GKey.Key_3;
			mSDLKToKey[Sdl.SDLK_4] = GKey.Key_4;
			mSDLKToKey[Sdl.SDLK_5] = GKey.Key_5;
			mSDLKToKey[Sdl.SDLK_6] = GKey.Key_6;
			mSDLKToKey[Sdl.SDLK_7] = GKey.Key_7;
			mSDLKToKey[Sdl.SDLK_8] = GKey.Key_8;
			mSDLKToKey[Sdl.SDLK_9] = GKey.Key_9;
			
			mSDLKToKey[Sdl.SDLK_KP_DIVIDE] = GKey.KP_Divide;
			mSDLKToKey[Sdl.SDLK_KP_ENTER] = GKey.KP_Enter;
			mSDLKToKey[Sdl.SDLK_KP_EQUALS] = GKey.KP_Equal;
			mSDLKToKey[Sdl.SDLK_KP_MINUS] = GKey.KP_Subtract;
			mSDLKToKey[Sdl.SDLK_KP_MULTIPLY] = GKey.KP_Multiply;
			mSDLKToKey[Sdl.SDLK_KP_PERIOD] = GKey.KP_Decimal;
			mSDLKToKey[Sdl.SDLK_KP_PLUS] = GKey.KP_Add;
			
			mSDLKToKey[Sdl.SDLK_KP0] = GKey.KP_0;
			mSDLKToKey[Sdl.SDLK_KP1] = GKey.KP_1;
			mSDLKToKey[Sdl.SDLK_KP2] = GKey.KP_2;
			mSDLKToKey[Sdl.SDLK_KP3] = GKey.KP_3;
			mSDLKToKey[Sdl.SDLK_KP4] = GKey.KP_4;
			mSDLKToKey[Sdl.SDLK_KP5] = GKey.KP_5;
			mSDLKToKey[Sdl.SDLK_KP6] = GKey.KP_6;
			mSDLKToKey[Sdl.SDLK_KP7] = GKey.KP_7;
			mSDLKToKey[Sdl.SDLK_KP8] = GKey.KP_8;
			mSDLKToKey[Sdl.SDLK_KP9] = GKey.KP_9;
			
			mSDLKToKey[Sdl.SDLK_a] = GKey.a;
			mSDLKToKey[Sdl.SDLK_b] = GKey.b;
			mSDLKToKey[Sdl.SDLK_c] = GKey.c;
			mSDLKToKey[Sdl.SDLK_d] = GKey.d;
			mSDLKToKey[Sdl.SDLK_e] = GKey.e;
			mSDLKToKey[Sdl.SDLK_f] = GKey.f;
			mSDLKToKey[Sdl.SDLK_g] = GKey.g;
			mSDLKToKey[Sdl.SDLK_h] = GKey.h;
			mSDLKToKey[Sdl.SDLK_i] = GKey.i;
			mSDLKToKey[Sdl.SDLK_j] = GKey.j;
			mSDLKToKey[Sdl.SDLK_k] = GKey.k;
			mSDLKToKey[Sdl.SDLK_l] = GKey.l;
			mSDLKToKey[Sdl.SDLK_m] = GKey.m;
			mSDLKToKey[Sdl.SDLK_n] = GKey.n;
			mSDLKToKey[Sdl.SDLK_o] = GKey.o;
			mSDLKToKey[Sdl.SDLK_p] = GKey.p;
			mSDLKToKey[Sdl.SDLK_q] = GKey.q;
			mSDLKToKey[Sdl.SDLK_r] = GKey.r;
			mSDLKToKey[Sdl.SDLK_s] = GKey.s;
			mSDLKToKey[Sdl.SDLK_t] = GKey.t;
			mSDLKToKey[Sdl.SDLK_u] = GKey.u;
			mSDLKToKey[Sdl.SDLK_v] = GKey.v;
			mSDLKToKey[Sdl.SDLK_w] = GKey.w;
			mSDLKToKey[Sdl.SDLK_x] = GKey.x;
			mSDLKToKey[Sdl.SDLK_y] = GKey.y;
			mSDLKToKey[Sdl.SDLK_z] = GKey.z;
			
			mSDLKToKey[Sdl.SDLK_F1] = GKey.F1;
			mSDLKToKey[Sdl.SDLK_F2] = GKey.F2;
			mSDLKToKey[Sdl.SDLK_F3] = GKey.F3;
			mSDLKToKey[Sdl.SDLK_F4] = GKey.F4;
			mSDLKToKey[Sdl.SDLK_F5] = GKey.F5;
			mSDLKToKey[Sdl.SDLK_F6] = GKey.F6;
			mSDLKToKey[Sdl.SDLK_F7] = GKey.F7;
			mSDLKToKey[Sdl.SDLK_F8] = GKey.F8;
			mSDLKToKey[Sdl.SDLK_F9] = GKey.F9;
			mSDLKToKey[Sdl.SDLK_F10] = GKey.F10;
			mSDLKToKey[Sdl.SDLK_F11] = GKey.F11;
			mSDLKToKey[Sdl.SDLK_F12] = GKey.F12;
			mSDLKToKey[Sdl.SDLK_F13] = GKey.F13;
			mSDLKToKey[Sdl.SDLK_F14] = GKey.F14;
			mSDLKToKey[Sdl.SDLK_F15] = GKey.F15;
			
			mSDLKToKey[Sdl.SDLK_AMPERSAND] = GKey.ampersand;
			mSDLKToKey[Sdl.SDLK_ASTERISK] = GKey.asterisk;
			mSDLKToKey[Sdl.SDLK_AT] = GKey.at;
			mSDLKToKey[Sdl.SDLK_BACKQUOTE] = GKey.grave;
			mSDLKToKey[Sdl.SDLK_BACKSLASH] = GKey.backslash;
			mSDLKToKey[Sdl.SDLK_BACKSPACE] = GKey.BackSpace;
			mSDLKToKey[Sdl.SDLK_BREAK] = GKey.Break;
			mSDLKToKey[Sdl.SDLK_CAPSLOCK] = GKey.Caps_Lock;
			mSDLKToKey[Sdl.SDLK_CARET] = GKey.asciicircum;
			mSDLKToKey[Sdl.SDLK_CLEAR] = GKey.Clear;
			mSDLKToKey[Sdl.SDLK_COLON] = GKey.colon;
			mSDLKToKey[Sdl.SDLK_COMMA] = GKey.comma;
			mSDLKToKey[Sdl.SDLK_DELETE] = GKey.Delete;
			mSDLKToKey[Sdl.SDLK_DOLLAR] = GKey.dollar;
			mSDLKToKey[Sdl.SDLK_DOWN] = GKey.downarrow;
			mSDLKToKey[Sdl.SDLK_END] = GKey.End;
			mSDLKToKey[Sdl.SDLK_EQUALS] = GKey.equal;
			mSDLKToKey[Sdl.SDLK_ESCAPE] = GKey.Escape;
			mSDLKToKey[Sdl.SDLK_EURO] = GKey.EuroSign;
			mSDLKToKey[Sdl.SDLK_EXCLAIM] = GKey.exclam;
			mSDLKToKey[Sdl.SDLK_GREATER] = GKey.greater;
			mSDLKToKey[Sdl.SDLK_HASH] = GKey.numbersign;
			mSDLKToKey[Sdl.SDLK_HELP] = GKey.Help;
			mSDLKToKey[Sdl.SDLK_HOME] = GKey.Home;
			mSDLKToKey[Sdl.SDLK_INSERT] = GKey.Insert;
			mSDLKToKey[Sdl.SDLK_LEFT] = GKey.leftarrow;
			mSDLKToKey[Sdl.SDLK_LEFTBRACKET] = GKey.bracketleft;
			mSDLKToKey[Sdl.SDLK_LEFTPAREN] = GKey.parenleft;
			mSDLKToKey[Sdl.SDLK_LESS] = GKey.less;
			mSDLKToKey[Sdl.SDLK_MINUS] = GKey.minus;
			mSDLKToKey[Sdl.SDLK_NUMLOCK] = GKey.Num_Lock;
			mSDLKToKey[Sdl.SDLK_PAGEDOWN] = GKey.Page_Down;
			mSDLKToKey[Sdl.SDLK_PAGEUP] = GKey.Page_Up;
			mSDLKToKey[Sdl.SDLK_PAUSE] = GKey.Pause;
			mSDLKToKey[Sdl.SDLK_PERIOD] = GKey.period;
			mSDLKToKey[Sdl.SDLK_PLUS] = GKey.plus;
			mSDLKToKey[Sdl.SDLK_QUESTION] = GKey.question;
			mSDLKToKey[Sdl.SDLK_QUOTE] = GKey.apostrophe;
			mSDLKToKey[Sdl.SDLK_QUOTEDBL] = GKey.quotedbl;
			mSDLKToKey[Sdl.SDLK_RETURN] = GKey.Return;
			mSDLKToKey[Sdl.SDLK_RIGHT] = GKey.rightarrow;
			mSDLKToKey[Sdl.SDLK_RIGHTBRACKET] = GKey.bracketright;
			mSDLKToKey[Sdl.SDLK_RIGHTPAREN] = GKey.parenright;
			mSDLKToKey[Sdl.SDLK_SCROLLOCK] = GKey.Scroll_Lock;
			mSDLKToKey[Sdl.SDLK_SEMICOLON] = GKey.semicolon;
			mSDLKToKey[Sdl.SDLK_SLASH] = GKey.slash;
			mSDLKToKey[Sdl.SDLK_SPACE] = GKey.space;
			mSDLKToKey[Sdl.SDLK_SYSREQ] = GKey.Sys_Req;
			mSDLKToKey[Sdl.SDLK_TAB] = GKey.Tab;
			mSDLKToKey[Sdl.SDLK_UNDERSCORE] = GKey.underscore;
			mSDLKToKey[Sdl.SDLK_UP] = GKey.uparrow;
			
			foreach (KeyValuePair<int, GKey> i in mSDLKToKey)
				mKeyToSDLK[i.Value] = i.Key;
			
			// And also take care of capitals.
			mKeyToSDLK[GKey.A] = Sdl.SDLK_a;
			mKeyToSDLK[GKey.B] = Sdl.SDLK_b;
			mKeyToSDLK[GKey.C] = Sdl.SDLK_c;
			mKeyToSDLK[GKey.D] = Sdl.SDLK_d;
			mKeyToSDLK[GKey.E] = Sdl.SDLK_e;
			mKeyToSDLK[GKey.F] = Sdl.SDLK_f;
			mKeyToSDLK[GKey.G] = Sdl.SDLK_g;
			mKeyToSDLK[GKey.H] = Sdl.SDLK_h;
			mKeyToSDLK[GKey.I] = Sdl.SDLK_i;
			mKeyToSDLK[GKey.J] = Sdl.SDLK_j;
			mKeyToSDLK[GKey.K] = Sdl.SDLK_k;
			mKeyToSDLK[GKey.L] = Sdl.SDLK_l;
			mKeyToSDLK[GKey.M] = Sdl.SDLK_m;
			mKeyToSDLK[GKey.N] = Sdl.SDLK_n;
			mKeyToSDLK[GKey.O] = Sdl.SDLK_o;
			mKeyToSDLK[GKey.P] = Sdl.SDLK_p;
			mKeyToSDLK[GKey.Q] = Sdl.SDLK_q;
			mKeyToSDLK[GKey.R] = Sdl.SDLK_r;
			mKeyToSDLK[GKey.S] = Sdl.SDLK_s;
			mKeyToSDLK[GKey.T] = Sdl.SDLK_t;
			mKeyToSDLK[GKey.U] = Sdl.SDLK_u;
			mKeyToSDLK[GKey.V] = Sdl.SDLK_v;
			mKeyToSDLK[GKey.W] = Sdl.SDLK_w;
			mKeyToSDLK[GKey.X] = Sdl.SDLK_x;
			mKeyToSDLK[GKey.Y] = Sdl.SDLK_y;
			mKeyToSDLK[GKey.Z] = Sdl.SDLK_z;
		}
		
		private bool mAltDown = false;
		
		private bool mControlDown = false;
		
		private bool mShiftDown = false;
		
		public bool AltDown {
			get {
				return this.mAltDown;
			}
			set {
				this.mAltDown = value;
				this.UpdateDisplay();
			}
		}
		
		public bool ControlDown {
			get {
				return this.mControlDown;
			}
			set {
				this.mControlDown = value;
				this.UpdateDisplay();
			}
		}
		
		public bool ShiftDown {
			get {
				return this.mShiftDown;
			}
			set {
				this.mShiftDown = value;
				this.UpdateDisplay();
			}
		}
		
		private GKey mKey = 0;
		
		public GKey Key {
			get {
				return this.mKey;
			}
			set {
				this.mKey = value;
				this.UpdateDisplay();
			}
		}
		
		public KeyEntry() {
			this.IsEditable = false;
		}
		
		private void UpdateDisplay() {
			if (this.mKey == 0) {
				this.Text = "";
			} else {
				StringBuilder sb = new StringBuilder();
				
				if (this.ControlDown)
					sb.Append("Control+");
				
				if (this.AltDown)
					sb.Append("Alt+");
				
				if (this.ShiftDown)
					sb.Append("Shift+");
				
				sb.Append(this.mKey.ToString());
				
				this.Text = sb.ToString();
			}
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey evnt) {
			if (mKeyToSDLK.ContainsKey(evnt.Key)) {
				this.mKey = evnt.Key;
				
				ModifierType mods = evnt.State;
				
				this.mAltDown = (mods & ModifierType.Mod1Mask) == ModifierType.Mod1Mask;
				this.mControlDown = (mods & ModifierType.ControlMask) == ModifierType.ControlMask;
				this.mShiftDown = (mods & ModifierType.ShiftMask) == ModifierType.ShiftMask;
				
				this.UpdateDisplay();
			}
			
			return true;
		}
	}
}
