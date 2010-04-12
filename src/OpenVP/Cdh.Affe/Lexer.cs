// Cdh.Affe: Affe language compiler.
// Copyright (C) 2007  Chris Howie
// 
// This library is free software; you can redistribute it and/or
// Modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// Version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// But WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Cdh.Affe.yyParser;

namespace Cdh.Affe {
	public class Lexer : yyInput {
		private static Dictionary<string, int> mKeywords;
		
		static Lexer() {
			mKeywords = new Dictionary<string, int>();
			
			mKeywords["break"] = Token.BREAK;
			mKeywords["continue"] = Token.CONTINUE;
			mKeywords["if"] = Token.IF;
			mKeywords["else"] = Token.ELSE;
			mKeywords["false"] = Token.FALSE;
			mKeywords["null"] = Token.NULL;
			mKeywords["return"] = Token.RETURN;
			mKeywords["true"] = Token.TRUE;
			mKeywords["while"] = Token.WHILE;
		}
		
		private object mValue = null;
		
		private int mToken = -1;
		
		private TextReader mReader;
		
		private int mTokenStartLocation = 0;
		
		public int TokenStartLocation {
			get {
				return this.mTokenStartLocation;
			}
		}
		
		private int mLocation = 0;
		
		public Lexer(TextReader r) {
			this.mReader = r;
		}
		
		public Lexer(string s) : this(new StringReader(s)) {
		}
		
		private int Peek() {
			return this.mReader.Peek();
		}
		
		private int Read() {
			int i = this.mReader.Read();
			if (i != -1)
				this.mLocation++;
			return i;
		}
		
		bool yyInput.advance() {
			this.mTokenStartLocation = this.mLocation;
			
			if (this.mToken == Token.EOF)
				return false;
			
			this.EatWhitespace();
			
			this.mTokenStartLocation = this.mLocation;
			
			if (this.Peek() == -1) {
				this.mToken = Token.EOF;
				this.mValue = this.mTokenStartLocation;
				return true;
			}
			
			if (this.CheckString())
				return true;
			
			if (this.CheckIdentifier())
				return true;
			
			if (this.CheckNumber())
				return true;
			
			this.mToken = this.CheckOther();
			this.mValue = this.mTokenStartLocation;
			
			if (this.mToken == Token.COMMENT) {
				int r;
				do {
					r = this.Read();
				} while (r != -1 && r != '\n');
				
				return ((yyInput) this).advance();
			}
			
			return true;
		}
		
		private void EatWhitespace() {
			for (;;) {
				int c = this.Peek();
				
				if (c == -1)
					return;
				
				if (!char.IsWhiteSpace((char) c))
					return;
				
				this.Read();
			}
		}
		
		internal static bool IsLetter(char c) {
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
		}
		
		internal static bool IsDigit(char c) {
			return c >= '0' && c <= '9';
		}
		
		private bool CheckIdentifier() {
			int c = this.Peek();
			char ch = (char) c;
			
			if (!IsLetter(ch))
				return false;
			
			this.Read();
			
			StringBuilder sb = new StringBuilder();
			sb.Append(ch);
			
			for (;;) {
				c = this.Peek();
				ch = (char) c;
				
				if (c == -1 || (!IsLetter(ch) && !IsDigit(ch))) {
					string id = sb.ToString();
					
					if (mKeywords.TryGetValue(id, out c)) {
						this.mToken = c;
						this.mValue = this.mTokenStartLocation;
					} else {
						this.mToken = Token.IDENTIFIER;
						this.mValue = Loc(sb.ToString());
					}
					return true;
				}
				
				sb.Append(ch);
				this.Read();
			}
		}
		
		private bool CheckString() {
			int c = this.Peek();
			char ch = (char) c;
			
			if (ch != '"')
				return false;
			
			StringBuilder sb = new StringBuilder();
			this.Read();
			
			bool escape = false;
			
			for (;;) {
				c = this.Read();
				ch = (char) c;
				
				if (c == -1) {
					this.mToken = Token.ERROR;
					this.mValue = this.mTokenStartLocation;
					return true;
				}
				
				if (escape) {
					escape = false;
					
					switch (ch) {
					case '\\':
						sb.Append('\\');
						break;
						
					case 'n':
						sb.Append('\n');
						break;
						
					default:
						sb.Append(ch);
						break;
					}
					continue;
				}
				
				if (ch == '\\') {
					escape = true;
				} else if (ch == '"') {
					this.mToken = Token.STRING;
					this.mValue = Loc(sb.ToString());
					return true;
				} else {
					sb.Append(ch);
				}
			}
		}
		
		private bool CheckNumber() {
			int c = this.Peek();
			char ch = (char) c;
			
			if (!IsDigit(ch))
				return false;
			
			bool period = false;
			
			StringBuilder sb = new StringBuilder();
			sb.Append(ch);
			
			this.Read();
			
			for (;;) {
				c = this.Peek();
				ch = (char) c;
				
				if (c == -1 || (!IsDigit(ch) && c != '.')) {
					if (c != -1 && IsLetter(ch)) {
						this.mToken = Token.ERROR;
						this.mValue = this.mTokenStartLocation;
						return true;
					}
					
					if (period) {
						this.mToken = Token.FLOAT;
						this.mValue = Loc(float.Parse(sb.ToString()));
					} else {
						this.mToken = Token.INTEGER;
						int v;
						if (!int.TryParse(sb.ToString(), out v))
							throw new yyException("Integer overflow in constant.");
						this.mValue = Loc(v);
					}
					
					return true;
				}
				
				if (ch == '.') {
					if (period) {
						this.mToken = Token.ERROR;
						this.mValue = this.mTokenStartLocation;
						return true;
					}
					
					period = true;
				}
				
				sb.Append(ch);
				
				this.Read();
			}
		}
		
		private TokenLocation Loc(object o) {
			return new TokenLocation(this.mTokenStartLocation, o);
		}
		
		private bool NextCharIs(char ch) {
			int c = this.Peek();
			
			if (c == -1 || (char) c != ch)
				return false;
			
			this.Read();
			
			return true;
		}
		
		private int CheckOther() {
			char c = (char) this.Read();
			
			switch (c) {
			case ';':
				return Token.SEMI;
			
			case '=':
				if (this.NextCharIs('='))
					return Token.EQ;
				
				return Token.ASSIGN;
				
			case '+':
				return Token.ADD;
			
			case '-':
				return Token.MINUS;
			
			case '/':
				if (this.NextCharIs('/'))
					return Token.COMMENT;
				
				return Token.DIVIDE;
			
			case '*':
				return Token.MULTIPLY;
			
			case '%':
				return Token.MOD;
				
			case '!':
				if (this.NextCharIs('='))
					return Token.NE;
				
				return Token.NOT;
			
			case '|':
				if (this.NextCharIs('|'))
					return Token.BOR;
				
				return Token.OR;
			
			case '&':
				if (this.NextCharIs('&'))
					return Token.BAND;
				
				return Token.AND;
			
			case '(':
				return Token.LPAREN;
			
			case ')':
				return Token.RPAREN;
			
			case '{':
				return Token.LBRACE;
			
			case '}':
				return Token.RBRACE;
			
			case ',':
				return Token.COMMA;
				
			case '<':
				if (this.NextCharIs('='))
					return Token.LTE;
				
				return Token.LT;
				
			case '>':
				if (this.NextCharIs('='))
					return Token.GTE;
				
				return Token.GT;
			
			case '.':
				return Token.PERIOD;
				
			case '$':
				return Token.DOLLAR;
				
			case '?':
				return Token.QUESTION;
				
			case ':':
				return Token.COLON;
				
			case '[':
				return Token.LBRACKET;
				
			case ']':
				return Token.RBRACKET;
				
			default:
				return Token.ERROR;
			}
		}
		
		int yyInput.token() {
			return this.mToken;
		}
		
		object yyInput.value() {
			return this.mValue;
		}
	}
	
	internal class TokenLocation {
		public int Location;
		public object Value;
		
		public TokenLocation(int location, object value) {
			this.Location = location;
			this.Value = value;
		}
	}
}
