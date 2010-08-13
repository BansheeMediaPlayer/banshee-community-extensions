// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 19 "jay/affe-parser.jay"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Cdh.Affe.Tree;

namespace Cdh.Affe {
	public class AffeParser {
		public static int yacc_verbose_flag;
		
		private Lexer lexer;
		
		internal Lexer Lexer {
			get {
				return this.lexer;
			}
		}
		
		public AffeParser(Lexer l) {
			this.lexer = l;
		}
		
		public Block Parse() {
			return (Block) yyparse(this.lexer);
		}
		
		private T Loc<T>(int i, T n) where T : Node {
			n.SourceLocation = i;
			return n;
		}
		
		private T Loc<T>(object o, T n) where T : Node {
			n.SourceLocation = ((Node) o).SourceLocation;
			return n;
		}
		
		private T Loc<T>(TokenLocation l, T n) where T : Node {
			n.SourceLocation = l.Location;
			return n;
		}
#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }

  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
//t  internal yydebug.yyDebug debug;

  protected static  int yyFinal = 14;
//t // Put this array into a separate class so it is only initialized if debugging is actually used
//t // Use MarshalByRefObject to disable inlining
//t class YYRules : MarshalByRefObject {
//t  public static  string [] yyRule = {
//t    "$accept : body",
//t    "body : opt_statements EOF",
//t    "opt_statements :",
//t    "opt_statements : statements",
//t    "statements : statement",
//t    "statements : statements statement",
//t    "statement : assignment SEMI",
//t    "statement : call SEMI",
//t    "statement : call_invokation_expression SEMI",
//t    "statement : conditional",
//t    "statement : while_loop",
//t    "statement : BREAK SEMI",
//t    "statement : variable_declaration SEMI",
//t    "statement : CONTINUE SEMI",
//t    "statement : RETURN SEMI",
//t    "while_loop : WHILE LPAREN expression RPAREN statement_block",
//t    "statement_block : statement",
//t    "statement_block : LBRACE opt_statements RBRACE",
//t    "conditional : IF LPAREN expression RPAREN statement_block",
//t    "conditional : IF LPAREN expression RPAREN statement_block ELSE statement_block",
//t    "assignment : lvalue ASSIGN expression",
//t    "lvalue : data_invokation_expression",
//t    "lvalue : indexed_expression",
//t    "lvalue : identifier",
//t    "variable_declaration : identifier identifier ASSIGN expression",
//t    "variable_declaration : identifier NOT identifier",
//t    "identifier : IDENTIFIER",
//t    "expression : conditional_expression",
//t    "conditional_expression : boolean_or_expression",
//t    "conditional_expression : boolean_or_expression QUESTION expression COLON expression",
//t    "boolean_or_expression : boolean_and_expression",
//t    "boolean_or_expression : boolean_or_expression BOR boolean_and_expression",
//t    "boolean_and_expression : or_expression",
//t    "boolean_and_expression : boolean_and_expression BAND or_expression",
//t    "or_expression : and_expression",
//t    "or_expression : or_expression OR and_expression",
//t    "and_expression : eq_expression",
//t    "and_expression : and_expression AND eq_expression",
//t    "eq_expression : rel_expression",
//t    "eq_expression : eq_expression EQ rel_expression",
//t    "eq_expression : eq_expression NE rel_expression",
//t    "rel_expression : add_expression",
//t    "rel_expression : rel_expression LT add_expression",
//t    "rel_expression : rel_expression GT add_expression",
//t    "rel_expression : rel_expression LTE add_expression",
//t    "rel_expression : rel_expression GTE add_expression",
//t    "add_expression : mult_expression",
//t    "add_expression : add_expression ADD mult_expression",
//t    "add_expression : add_expression MINUS mult_expression",
//t    "mult_expression : unary_expression",
//t    "mult_expression : mult_expression MULTIPLY unary_expression",
//t    "mult_expression : mult_expression DIVIDE unary_expression",
//t    "mult_expression : mult_expression MOD unary_expression",
//t    "unary_expression : indexed_expression",
//t    "unary_expression : MINUS unary_expression",
//t    "unary_expression : NOT unary_expression",
//t    "unary_expression : LPAREN identifier RPAREN unary_expression",
//t    "unary_expression : LPAREN identifier RPAREN",
//t    "indexed_expression : invokation_expression",
//t    "indexed_expression : indexed_expression LBRACKET arguments RBRACKET",
//t    "invokation_expression : value_expression",
//t    "invokation_expression : call_invokation_expression",
//t    "invokation_expression : data_invokation_expression",
//t    "data_invokation_expression : invokation_expression PERIOD identifier",
//t    "data_invokation_expression : invokation_expression DOLLAR identifier",
//t    "call_invokation_expression : invokation_expression PERIOD identifier LPAREN opt_arguments RPAREN",
//t    "call_invokation_expression : invokation_expression DOLLAR identifier LPAREN opt_arguments RPAREN",
//t    "value_expression : identifier",
//t    "value_expression : FLOAT",
//t    "value_expression : INTEGER",
//t    "value_expression : STRING",
//t    "value_expression : TRUE",
//t    "value_expression : FALSE",
//t    "value_expression : NULL",
//t    "value_expression : call",
//t    "value_expression : LPAREN expression RPAREN",
//t    "call : identifier LPAREN opt_arguments RPAREN",
//t    "opt_arguments :",
//t    "opt_arguments : arguments",
//t    "arguments : expression",
//t    "arguments : arguments COMMA expression",
//t  };
//t public static string getRule (int index) {
//t    return yyRule [index];
//t }
//t}
  protected static  string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"'!'",null,null,"'$'","'%'","'&'",
    null,"'('","')'","'*'","'+'","','","'-'","'.'","'/'",null,null,null,
    null,null,null,null,null,null,null,"':'","';'","'<'","'='","'>'",
    "'?'",null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,"'['",null,"']'",null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,"'{'","'|'","'}'",null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,"NONE","EOF","ERROR","IDENTIFIER","INTEGER","FLOAT","STRING",
    "SEMI","ASSIGN","ADD","MINUS","DIVIDE","MULTIPLY","OR","AND","MOD",
    "NOT","LT","GT","LTE","\"<=\"","GTE","\">=\"","EQ","\"==\"","NE",
    "\"!=\"","BOR","\"||\"","BAND","\"&&\"","LPAREN","RPAREN","LBRACE",
    "RBRACE","COMMA","PERIOD","DOLLAR","QUESTION","COLON","LBRACKET",
    "RBRACKET","BREAK","CONTINUE","IF","ELSE","RETURN","WHILE","TRUE",
    "FALSE","NULL","COMMENT",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
//t  public static string yyname (int token) {
//t    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
//t    string name;
//t    if ((name = yyNames[token]) != null) return name;
//t    return "[unknown]";
//t  }

  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected string[] yyExpecting (int state) {
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];

    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }

    string [] result = new string[len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = yyNames[token];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
//t    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;			// initial size
    int yyState = 0;                                   // state stack ptr
    int [] yyStates = new int[yyMax];	                // state stack 
    Object yyVal = null;                               // value stack ptr
    Object [] yyVals = new Object[yyMax];	        // value stack
    int yyToken = -1;					// current input
    int yyErrorFlag = 0;				// #tks to shift

    /*yyLoop:*/ for (int yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        int[] i = new int[yyStates.Length+yyMax];
        yyStates.CopyTo (i, 0);
        yyStates = i;
        Object[] o = new Object[yyVals.Length+yyMax];
        yyVals.CopyTo (o, 0);
        yyVals = o;
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
//t      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ for (;;) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
//t            if (debug != null)
//t              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
//t            if (debug != null)
//t              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
//t              if (debug != null) debug.error("syntax error");
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
//t                  if (debug != null)
//t                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
//t                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
//t              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
//t                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
//t              if (debug != null)
//t                debug.discard(yyState, yyToken, yyname(yyToken),
//t  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
//t        if (debug != null)
//t          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 1:
#line 124 "jay/affe-parser.jay"
  {
		yyVal = new Block((List<Statement>) yyVals[-1+yyTop]);
	}
  break;
case 2:
#line 131 "jay/affe-parser.jay"
  {
		yyVal = new List<Statement>();
	}
  break;
case 4:
#line 139 "jay/affe-parser.jay"
  {
		List<Statement> list = new List<Statement>();
		list.Add((Statement) yyVals[0+yyTop]);
		yyVal = list;
	}
  break;
case 5:
#line 145 "jay/affe-parser.jay"
  {
		((List<Statement>) yyVals[-1+yyTop]).Add((Statement) yyVals[0+yyTop]);
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 6:
#line 153 "jay/affe-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 7:
#line 157 "jay/affe-parser.jay"
  {
		yyVal = Loc(yyVals[-1+yyTop], new CallStatement((CallExpression) yyVals[-1+yyTop]));
	}
  break;
case 8:
#line 161 "jay/affe-parser.jay"
  {
		yyVal = Loc(yyVals[-1+yyTop], new CallInvokationStatement((CallInvokationExpression) yyVals[-1+yyTop]));
	}
  break;
case 11:
#line 167 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new BreakStatement());
	}
  break;
case 12:
#line 171 "jay/affe-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 13:
#line 175 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new ContinueStatement());
	}
  break;
case 14:
#line 179 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new ReturnStatement());
	}
  break;
case 15:
#line 186 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-4+yyTop], new WhileLoop((Expression) yyVals[-2+yyTop], (Block) yyVals[0+yyTop]));
	}
  break;
case 16:
#line 193 "jay/affe-parser.jay"
  {
		List<Statement> list = new List<Statement>();
		list.Add((Statement) yyVals[0+yyTop]);
		yyVal = new Block(list);
	}
  break;
case 17:
#line 199 "jay/affe-parser.jay"
  {
		yyVal = new Block((List<Statement>) yyVals[-1+yyTop]);
	}
  break;
case 18:
#line 206 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-4+yyTop], new If((Expression) yyVals[-2+yyTop], (Block) yyVals[0+yyTop]));
	}
  break;
case 19:
#line 210 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-6+yyTop], new IfElse((Expression) yyVals[-4+yyTop], (Block) yyVals[-2+yyTop], (Block) yyVals[0+yyTop]));
	}
  break;
case 20:
#line 217 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new Assignment((Lvalue) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 23:
#line 226 "jay/affe-parser.jay"
  {
		yyVal = new IdentifierExpression((Identifier) yyVals[0+yyTop]);
	}
  break;
case 24:
#line 233 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new VariableDeclaration((Identifier) yyVals[-3+yyTop],
			(Identifier) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 25:
#line 238 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new PersistentVariableDeclaration((Identifier) yyVals[-2+yyTop],
			(Identifier) yyVals[0+yyTop]));
	}
  break;
case 26:
#line 246 "jay/affe-parser.jay"
  {
		TokenLocation l = (TokenLocation) yyVals[0+yyTop];
		
		yyVal = Loc(l, new Identifier((string) l.Value));
	}
  break;
case 29:
#line 260 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-3+yyTop], new TernaryConditionalExpression(
			(Expression) yyVals[-4+yyTop], (Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 31:
#line 269 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Bor,
			(Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 33:
#line 278 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Band,
			(Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 35:
#line 287 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Or, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 37:
#line 296 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.And, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 39:
#line 305 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Eq, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 40:
#line 310 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Ne, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 42:
#line 319 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Lt, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 43:
#line 324 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Gt, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 44:
#line 329 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Lte, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 45:
#line 334 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Gte, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 47:
#line 343 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Add, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 48:
#line 348 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Minus,
			(Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 50:
#line 357 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Multiply,
			(Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 51:
#line 362 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Divide,
			(Expression) yyVals[-2+yyTop], (Expression) yyVals[0+yyTop]));
	}
  break;
case 52:
#line 367 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new OperatorExpression(Operator.Mod, (Expression) yyVals[-2+yyTop],
			(Expression) yyVals[0+yyTop]));
	}
  break;
case 54:
#line 376 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new UnaryExpression(Operator.Neg, (Expression) yyVals[0+yyTop]));
	}
  break;
case 55:
#line 380 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new UnaryExpression(Operator.Not, (Expression) yyVals[0+yyTop]));
	}
  break;
case 56:
#line 384 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-3+yyTop], new CastExpression((Expression) yyVals[0+yyTop], (Identifier) yyVals[-2+yyTop]));
	}
  break;
case 57:
#line 388 "jay/affe-parser.jay"
  {
		/* This is a hack to make sure that something like (v) doesn't*/
		/* generate a parsing exception.*/
		yyVal = Loc(yyVals[-1+yyTop], new IdentifierExpression((Identifier) yyVals[-1+yyTop]));
	}
  break;
case 59:
#line 398 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-2+yyTop], new IndexedExpression((Expression) yyVals[-3+yyTop],
			(List<Expression>) yyVals[-1+yyTop]));
	}
  break;
case 63:
#line 412 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new DataInvokationExpression((Expression) yyVals[-2+yyTop],
			(Identifier) yyVals[0+yyTop]));
	}
  break;
case 64:
#line 417 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-1+yyTop], new LateBoundDataInvokationExpression(
			(Expression) yyVals[-2+yyTop], (Identifier) yyVals[0+yyTop]));
	}
  break;
case 65:
#line 425 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-4+yyTop], new CallInvokationExpression((Expression) yyVals[-5+yyTop],
			(Identifier) yyVals[-3+yyTop], (List<Expression>) yyVals[-1+yyTop]));
	}
  break;
case 66:
#line 430 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-4+yyTop], new LateBoundCallInvokationExpression(
			(Expression) yyVals[-5+yyTop], (Identifier) yyVals[-3+yyTop], (List<Expression>) yyVals[-1+yyTop]));
	}
  break;
case 67:
#line 438 "jay/affe-parser.jay"
  {
		yyVal = Loc(yyVals[0+yyTop], new IdentifierExpression((Identifier) yyVals[0+yyTop]));
	}
  break;
case 68:
#line 442 "jay/affe-parser.jay"
  {
		TokenLocation l = (TokenLocation) yyVals[0+yyTop];
		
		yyVal = Loc(l, new FloatExpression((float) l.Value));
	}
  break;
case 69:
#line 448 "jay/affe-parser.jay"
  {
		TokenLocation l = (TokenLocation) yyVals[0+yyTop];
		
		yyVal = Loc(l, new IntegerExpression((int) l.Value));
	}
  break;
case 70:
#line 454 "jay/affe-parser.jay"
  {
		TokenLocation l = (TokenLocation) yyVals[0+yyTop];
		
		yyVal = Loc(l, new StringExpression((string) l.Value));
	}
  break;
case 71:
#line 460 "jay/affe-parser.jay"
  {
		yyVal = new BooleanExpression(true);
	}
  break;
case 72:
#line 464 "jay/affe-parser.jay"
  {
		yyVal = new BooleanExpression(false);
	}
  break;
case 73:
#line 468 "jay/affe-parser.jay"
  {
		yyVal = new NullExpression();
	}
  break;
case 75:
#line 473 "jay/affe-parser.jay"
  {
		yyVal = yyVals[-1+yyTop];
	}
  break;
case 76:
#line 480 "jay/affe-parser.jay"
  {
		yyVal = Loc((int) yyVals[-2+yyTop], new CallExpression((Identifier) yyVals[-3+yyTop],
			(List<Expression>) yyVals[-1+yyTop]));
	}
  break;
case 77:
#line 488 "jay/affe-parser.jay"
  {
		yyVal = new List<Expression>();
	}
  break;
case 79:
#line 496 "jay/affe-parser.jay"
  {
		List<Expression> list = new List<Expression>();
		list.Add((Expression) yyVals[0+yyTop]);
		yyVal = list;
	}
  break;
case 80:
#line 502 "jay/affe-parser.jay"
  {
		((List<Expression>) yyVals[-2+yyTop]).Add((Expression) yyVals[0+yyTop]);
		yyVal = yyVals[-2+yyTop];
	}
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
//t          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
//t            if (debug != null)
//t               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
//t            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
//t        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: continue;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: continue;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

   static  short [] yyLhs  = {              -1,
    0,    1,    1,    2,    2,    3,    3,    3,    3,    3,
    3,    3,    3,    3,    8,   11,   11,    7,    7,    4,
   12,   12,   12,    9,    9,   15,   10,   16,   16,   17,
   17,   18,   18,   19,   19,   20,   20,   21,   21,   21,
   22,   22,   22,   22,   22,   23,   23,   23,   24,   24,
   24,   24,   25,   25,   25,   25,   25,   14,   14,   26,
   26,   26,   13,   13,    6,    6,   28,   28,   28,   28,
   28,   28,   28,   28,   28,    5,   29,   29,   27,   27,
  };
   static  short [] yyLen = {           2,
    2,    0,    1,    1,    2,    2,    2,    2,    1,    1,
    2,    2,    2,    2,    5,    1,    3,    5,    7,    3,
    1,    1,    1,    4,    3,    1,    1,    1,    5,    1,
    3,    1,    3,    1,    3,    1,    3,    1,    3,    3,
    1,    3,    3,    3,    3,    1,    3,    3,    1,    3,
    3,    3,    1,    2,    2,    4,    3,    1,    4,    1,
    1,    1,    3,    3,    6,    6,    1,    1,    1,    1,
    1,    1,    1,    1,    3,    4,    0,    1,    1,    3,
  };
   static  short [] yyDefRed = {            0,
   26,   69,   68,   70,    0,    0,    0,    0,    0,    0,
   71,   72,   73,    0,    0,    0,    4,    0,    0,    0,
    9,   10,    0,    0,    0,    0,    0,    0,   60,    0,
    0,    0,   74,   61,    0,   62,    0,    0,   27,    0,
    0,    0,    0,    0,    0,    0,    0,   49,   11,   13,
    0,   14,    0,    1,    5,    6,    7,    8,   12,    0,
    0,    0,    0,    0,    0,    0,   54,   55,    0,   75,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   20,   79,
    0,   25,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   51,   50,   52,    0,    0,    0,   59,   76,   24,
    0,    0,   56,    0,    0,   16,    0,   15,   80,    0,
    0,   29,    0,    0,   65,   66,   17,   19,
  };
  protected static  short [] yyDgoto  = {            14,
   15,   16,  126,   18,   33,   34,   21,   22,   23,   90,
  127,   24,   36,   37,   38,   39,   40,   41,   42,   43,
   44,   45,   46,   47,   48,   28,   93,   29,   94,
  };
  protected static  short [] yySindex = {         -202,
    0,    0,    0,    0, -116, -256, -238, -288, -217, -236,
    0,    0,    0,    0, -194, -202,    0, -180, -176, -171,
    0,    0, -169, -189,    0, -182, -188, -187,    0, -116,
 -116, -116,    0,    0, -160,    0, -182, -179,    0, -264,
 -153, -132, -131, -253, -184, -154, -224,    0,    0,    0,
 -116,    0, -116,    0,    0,    0,    0,    0,    0, -116,
 -116, -118, -116, -117, -118, -118,    0,    0, -130,    0,
 -116, -116, -116, -116, -116, -116, -116, -116, -116, -116,
 -116, -116, -116, -116, -116, -116, -139, -137,    0,    0,
 -268,    0, -138, -123, -116, -106, -104, -116, -153, -111,
 -132, -131, -253, -184, -184, -154, -154, -154, -154, -224,
 -224,    0,    0,    0, -250, -250, -116,    0,    0,    0,
 -116, -116,    0, -116, -202,    0, -129,    0,    0, -103,
 -101,    0,  -90, -250,    0,    0,    0,    0,
  };
  protected static  short [] yyRindex = {          -53,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, -252,    0,    0, -251, -183,
    0,    0,    0,    0,  -78,  -58,   62,   33,    0,    0,
    0,    0,    0,    0,    0,    0,   68,   -2,    0, -128,
  491,  468,  438,  402, -115,  229,  134,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  -80,    0,    0,    0,    0,    0,  537,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  -76,    0,    0,  -72,  -37,  101,  504,    0,
  481,  455,  425,  362,  385,  258,  287,  316,  345,  167,
  200,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  -80,  -80,    0,    0,  -73,    0, -226,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,
  };
  protected static  short [] yyGindex = {            0,
  111,    0,    2,    0,    3,    5,    0,    0,    0,   36,
 -112,    0,    7,    9,    1,    0,    0,  140,  169,  166,
  171,   86,  583,   93,  -15,    0,  183,    0,   57,
  };
  protected static  short [] yyTable = {            51,
   27,   17,   19,  128,   20,    3,   25,   49,   26,    1,
    2,    3,    4,   74,   67,   68,   27,   55,   19,   71,
   20,  138,   25,  117,   26,   50,   76,   64,   77,  118,
   72,   18,   69,   18,   18,   18,   18,    5,    3,  125,
   35,   74,   74,   84,   85,   74,   52,   86,    6,    7,
    8,   53,    9,   10,   11,   12,   13,    1,    2,    3,
    4,   18,   92,   54,   18,   96,   97,   35,  112,  113,
  114,    1,   18,   18,   18,   60,   18,   18,   18,   18,
   18,   61,  123,   56,   62,    5,   87,   57,   88,   78,
   79,   80,   58,   81,   59,   89,    6,    7,    8,   63,
    9,   10,   11,   12,   13,   65,   66,  100,   63,   61,
   61,   82,   83,   61,   61,   27,   27,   19,   19,   20,
   20,   25,   25,   26,   26,   27,   17,   19,   70,   20,
  120,   25,   73,   26,   27,   28,   19,   74,   20,   75,
   25,    1,   26,    1,    2,    3,    4,   95,   38,  115,
   30,  116,  129,  117,   38,   38,   31,   63,   98,  132,
   28,  104,  105,   28,   38,  119,   38,   28,   38,   28,
   38,   32,  134,   38,  110,  111,   38,  130,  131,   38,
   38,  121,   38,  122,  124,  135,   21,  136,   11,   12,
   13,   63,   63,   63,   63,   63,   63,   63,   63,   63,
  137,   63,   63,   63,    2,   63,   22,   63,   77,   63,
   99,   63,   78,   63,   62,   62,   63,    2,   62,   63,
   63,   63,   63,   63,   63,   63,   64,   64,   64,   64,
   64,   64,   64,   64,   64,  133,   64,   64,   64,  102,
   64,  101,   64,   91,   64,  103,   64,    0,   64,    0,
    0,   64,    0,    0,   64,   64,   64,   64,   64,   64,
   64,   67,    0,   67,   67,   67,   67,   67,   67,   67,
    0,   67,   67,   67,    0,   67,    0,   67,    0,   67,
    0,   67,    0,   67,    0,    0,   67,    0,    0,   67,
   67,   67,   67,   67,   67,   67,   58,   58,   58,   58,
   58,   58,   58,   58,   58,    0,   58,   58,   58,    0,
   58,    0,   58,    0,   58,    0,   58,    0,   58,    0,
    0,   58,    0,    0,   58,    0,   23,   58,   58,   58,
   58,   53,    0,   53,   53,   53,   53,   53,   53,   53,
    0,   53,   53,   53,    0,   53,    0,   53,    0,   53,
    0,   53,    0,   53,   67,   67,   53,    0,   67,   53,
    0,    0,   53,   53,   57,   53,   57,    0,   57,   57,
   57,   57,   57,    0,   57,   57,   57,    0,   57,    0,
   57,    0,   57,    0,   57,    0,   57,    0,    0,   57,
    0,    0,   57,    0,    0,   57,   57,   46,   57,   46,
   46,    0,    0,   46,   46,    0,    0,   46,   46,   46,
    0,   46,    0,   46,    0,   46,    0,   46,    0,   46,
    0,    0,   46,    0,    0,   46,    0,    0,   46,   46,
   47,   46,   47,   47,    0,    0,   47,   47,    0,    0,
   47,   47,   47,    0,   47,    0,   47,    0,   47,    0,
   47,    0,   47,    0,    0,   47,    0,    0,   47,    0,
    0,   47,   47,   48,   47,   48,   48,    0,    0,   48,
   48,    0,    0,   48,   48,   48,    0,   48,    0,   48,
    0,   48,    0,   48,    0,   48,    0,    0,   48,    0,
    0,   48,   41,    0,   48,   48,    0,   48,   41,   41,
    0,    0,   41,   41,   41,    0,   41,    0,   41,    0,
   41,    0,   41,    0,   41,    0,    0,   41,    0,    0,
   41,   42,    0,   41,   41,    0,   41,   42,   42,    0,
    0,   42,   42,   42,    0,   42,    0,   42,    0,   42,
    0,   42,    0,   42,    0,    0,   42,    0,    0,   42,
   43,    0,   42,   42,    0,   42,   43,   43,    0,    0,
   43,   43,   43,    0,   43,    0,   43,    0,   43,    0,
   43,    0,   43,    0,    0,   43,    0,    0,   43,   44,
    0,   43,   43,    0,   43,   44,   44,    0,    0,   44,
   44,   44,    0,   44,    0,   44,    0,   44,    0,   44,
    0,   44,    0,    0,   44,    0,    0,   44,   45,    0,
   44,   44,    0,   44,   45,   45,    0,    0,   45,   45,
   45,    0,   45,    0,   45,   39,   45,    0,   45,    0,
   45,   39,   39,   45,    0,    0,   45,    0,    0,   45,
   45,   39,   45,   39,    0,   39,    0,   39,   40,    0,
   39,    0,    0,   39,   40,   40,   39,   39,    0,   39,
  106,  107,  108,  109,   40,   36,   40,    0,   40,    0,
   40,   36,   36,   40,    0,    0,   40,    0,    0,   40,
   40,    0,   40,    0,    0,   36,    0,   36,   37,    0,
   36,    0,    0,   36,   37,   37,   36,   36,    0,   36,
    0,   34,    0,    0,    0,    0,    0,   34,   37,    0,
   37,    0,    0,   37,    0,    0,   37,    0,   35,   37,
   37,   34,   37,   34,   35,    0,   34,    0,    0,   34,
    0,   32,   34,   34,    0,   34,    0,    0,   35,    0,
   35,    0,    0,   35,   33,    0,   35,    0,    0,   35,
   35,   32,   35,   32,   30,    0,   32,    0,    0,   32,
    0,    0,   32,   32,   33,   32,   33,   31,    0,   33,
    0,    0,   33,    0,   30,   33,   33,    0,   33,   30,
    0,    0,   30,    0,    0,   30,   30,   31,   30,    0,
    0,    0,   31,    0,    0,   31,    0,    0,   31,   31,
    0,   31,   67,   67,   67,   67,   67,   67,   67,    0,
   67,   67,   67,    0,   67,    0,   67,    0,   67,    0,
   67,    0,   67,    0,    0,    0,    0,    0,    0,   67,
   67,   67,    0,   67,
  };
  protected static  short [] yyCheck = {           288,
    0,    0,    0,  116,    0,  258,    0,  264,    0,  260,
  261,  262,  263,  265,   30,   31,   16,   16,   16,  284,
   16,  134,   16,  292,   16,  264,  280,   27,  282,  298,
  295,  258,   32,  260,  261,  262,  263,  288,  291,  290,
    5,  293,  294,  268,  269,  297,  264,  272,  299,  300,
  301,  288,  303,  304,  305,  306,  307,  260,  261,  262,
  263,  288,   62,  258,  291,   65,   66,   32,   84,   85,
   86,  260,  299,  300,  301,  265,  303,  304,  305,  306,
  307,  265,   98,  264,  273,  288,   51,  264,   53,  274,
  275,  276,  264,  278,  264,   60,  299,  300,  301,  288,
  303,  304,  305,  306,  307,  293,  294,   72,  288,  293,
  294,  266,  267,  297,  297,  115,  116,  115,  116,  115,
  116,  115,  116,  115,  116,  125,  125,  125,  289,  125,
   95,  125,  286,  125,  134,  264,  134,  270,  134,  271,
  134,  260,  134,  260,  261,  262,  263,  265,  264,  289,
  267,  289,  117,  292,  270,  271,  273,  288,  289,  124,
  289,   76,   77,  292,  280,  289,  282,  296,  284,  298,
  286,  288,  302,  289,   82,   83,  292,  121,  122,  295,
  296,  288,  298,  288,  296,  289,  265,  289,  305,  306,
  307,  264,  265,  266,  267,  268,  269,  270,  271,  272,
  291,  274,  275,  276,  258,  278,  265,  280,  289,  282,
   71,  284,  289,  286,  293,  294,  289,  291,  297,  292,
  293,  294,  295,  296,  297,  298,  264,  265,  266,  267,
  268,  269,  270,  271,  272,  125,  274,  275,  276,   74,
  278,   73,  280,   61,  282,   75,  284,   -1,  286,   -1,
   -1,  289,   -1,   -1,  292,  293,  294,  295,  296,  297,
  298,  264,   -1,  266,  267,  268,  269,  270,  271,  272,
   -1,  274,  275,  276,   -1,  278,   -1,  280,   -1,  282,
   -1,  284,   -1,  286,   -1,   -1,  289,   -1,   -1,  292,
  293,  294,  295,  296,  297,  298,  264,  265,  266,  267,
  268,  269,  270,  271,  272,   -1,  274,  275,  276,   -1,
  278,   -1,  280,   -1,  282,   -1,  284,   -1,  286,   -1,
   -1,  289,   -1,   -1,  292,   -1,  265,  295,  296,  297,
  298,  264,   -1,  266,  267,  268,  269,  270,  271,  272,
   -1,  274,  275,  276,   -1,  278,   -1,  280,   -1,  282,
   -1,  284,   -1,  286,  293,  294,  289,   -1,  297,  292,
   -1,   -1,  295,  296,  264,  298,  266,   -1,  268,  269,
  270,  271,  272,   -1,  274,  275,  276,   -1,  278,   -1,
  280,   -1,  282,   -1,  284,   -1,  286,   -1,   -1,  289,
   -1,   -1,  292,   -1,   -1,  295,  296,  264,  298,  266,
  267,   -1,   -1,  270,  271,   -1,   -1,  274,  275,  276,
   -1,  278,   -1,  280,   -1,  282,   -1,  284,   -1,  286,
   -1,   -1,  289,   -1,   -1,  292,   -1,   -1,  295,  296,
  264,  298,  266,  267,   -1,   -1,  270,  271,   -1,   -1,
  274,  275,  276,   -1,  278,   -1,  280,   -1,  282,   -1,
  284,   -1,  286,   -1,   -1,  289,   -1,   -1,  292,   -1,
   -1,  295,  296,  264,  298,  266,  267,   -1,   -1,  270,
  271,   -1,   -1,  274,  275,  276,   -1,  278,   -1,  280,
   -1,  282,   -1,  284,   -1,  286,   -1,   -1,  289,   -1,
   -1,  292,  264,   -1,  295,  296,   -1,  298,  270,  271,
   -1,   -1,  274,  275,  276,   -1,  278,   -1,  280,   -1,
  282,   -1,  284,   -1,  286,   -1,   -1,  289,   -1,   -1,
  292,  264,   -1,  295,  296,   -1,  298,  270,  271,   -1,
   -1,  274,  275,  276,   -1,  278,   -1,  280,   -1,  282,
   -1,  284,   -1,  286,   -1,   -1,  289,   -1,   -1,  292,
  264,   -1,  295,  296,   -1,  298,  270,  271,   -1,   -1,
  274,  275,  276,   -1,  278,   -1,  280,   -1,  282,   -1,
  284,   -1,  286,   -1,   -1,  289,   -1,   -1,  292,  264,
   -1,  295,  296,   -1,  298,  270,  271,   -1,   -1,  274,
  275,  276,   -1,  278,   -1,  280,   -1,  282,   -1,  284,
   -1,  286,   -1,   -1,  289,   -1,   -1,  292,  264,   -1,
  295,  296,   -1,  298,  270,  271,   -1,   -1,  274,  275,
  276,   -1,  278,   -1,  280,  264,  282,   -1,  284,   -1,
  286,  270,  271,  289,   -1,   -1,  292,   -1,   -1,  295,
  296,  280,  298,  282,   -1,  284,   -1,  286,  264,   -1,
  289,   -1,   -1,  292,  270,  271,  295,  296,   -1,  298,
   78,   79,   80,   81,  280,  264,  282,   -1,  284,   -1,
  286,  270,  271,  289,   -1,   -1,  292,   -1,   -1,  295,
  296,   -1,  298,   -1,   -1,  284,   -1,  286,  264,   -1,
  289,   -1,   -1,  292,  270,  271,  295,  296,   -1,  298,
   -1,  264,   -1,   -1,   -1,   -1,   -1,  270,  284,   -1,
  286,   -1,   -1,  289,   -1,   -1,  292,   -1,  264,  295,
  296,  284,  298,  286,  270,   -1,  289,   -1,   -1,  292,
   -1,  264,  295,  296,   -1,  298,   -1,   -1,  284,   -1,
  286,   -1,   -1,  289,  264,   -1,  292,   -1,   -1,  295,
  296,  284,  298,  286,  264,   -1,  289,   -1,   -1,  292,
   -1,   -1,  295,  296,  284,  298,  286,  264,   -1,  289,
   -1,   -1,  292,   -1,  284,  295,  296,   -1,  298,  289,
   -1,   -1,  292,   -1,   -1,  295,  296,  284,  298,   -1,
   -1,   -1,  289,   -1,   -1,  292,   -1,   -1,  295,  296,
   -1,  298,  266,  267,  268,  269,  270,  271,  272,   -1,
  274,  275,  276,   -1,  278,   -1,  280,   -1,  282,   -1,
  284,   -1,  286,   -1,   -1,   -1,   -1,   -1,   -1,  293,
  294,  295,   -1,  297,
  };

#line 509 "jay/affe-parser.jay"
}
#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int NONE = 257;
  public const int EOF = 258;
  public const int ERROR = 259;
  public const int IDENTIFIER = 260;
  public const int INTEGER = 261;
  public const int FLOAT = 262;
  public const int STRING = 263;
  public const int SEMI = 264;
  public const int ASSIGN = 265;
  public const int ADD = 266;
  public const int MINUS = 267;
  public const int DIVIDE = 268;
  public const int MULTIPLY = 269;
  public const int OR = 270;
  public const int AND = 271;
  public const int MOD = 272;
  public const int NOT = 273;
  public const int LT = 274;
  public const int GT = 275;
  public const int LTE = 276;
  public const int GTE = 278;
  public const int EQ = 280;
  public const int NE = 282;
  public const int BOR = 284;
  public const int BAND = 286;
  public const int LPAREN = 288;
  public const int RPAREN = 289;
  public const int LBRACE = 290;
  public const int RBRACE = 291;
  public const int COMMA = 292;
  public const int PERIOD = 293;
  public const int DOLLAR = 294;
  public const int QUESTION = 295;
  public const int COLON = 296;
  public const int LBRACKET = 297;
  public const int RBRACKET = 298;
  public const int BREAK = 299;
  public const int CONTINUE = 300;
  public const int IF = 301;
  public const int ELSE = 302;
  public const int RETURN = 303;
  public const int WHILE = 304;
  public const int TRUE = 305;
  public const int FALSE = 306;
  public const int NULL = 307;
  public const int COMMENT = 308;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
