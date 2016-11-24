using System;
using System.IO;

namespace Aegis2Cronus.Aegis
{
    internal class Scanner
    {
        public const char EOF = char.MaxValue;
        private string catcher = "";
        public char ch;

        public int col;
        public char last;
        private string lcatcher;
        public int line;
        public char nextCH;
        public TextReader tr;

        public Scanner(string text)
        {
            text = SubstNpcConst(text);
            line = 1;
            col = 0;

            tr = new StringReader(text);
            NextCh();
        }

        private string SubstNpcConst(string text)
        {
            string[] lines = File.ReadAllLines(@"./npcs.txt");

            foreach (string line in lines)
            {
                string[] parts = line.Split('\t');

                text = text.Replace(parts[0], parts[1]);
            }

            return text;
        }

        public Token Next()
        {
            SkipWhiteSpace();

            var tk = new Token();
            tk.Line = line;
            tk.Col = col;

            if (IsNumber(ch))
                ReadNumber(ref tk);
            else if (IsLetter(ch))
                ReadIdentifier(ref tk);
            else if (ch == '"')
                ReadString(ref tk);
            else
            {
                switch (ch)
                {
                    case '\n':
                        tk.Type = TokenType.NewLine;
                        NextCh();
                        break;
                    case '(':
                        tk.Type = TokenType.LPar;
                        NextCh();
                        break;
                    case '[':
                        Match('[');
                        string str = "";
                        while (IsAlpha(ch) || ch == '_' || ch == '\'')
                        {
                            str += ch;
                            NextCh();
                        }
                        tk.Type = TokenType.Var;
                        Match(']');
                        break;
                    case '{':
                        tk.Type = TokenType.LBrack;
                        NextCh();
                        break;
                    case ')':
                        tk.Type = TokenType.RPar;
                        NextCh();
                        break;
                    case ']':
                        tk.Type = TokenType.RCol;
                        NextCh();
                        break;
                    case '}':
                        tk.Type = TokenType.RBrack;
                        NextCh();
                        break;
                    case '+':
                        tk.Type = TokenType.Plus;
                        NextCh();
                        break;
                    case '-':
                        tk.Type = TokenType.Minus;
                        NextCh();
                        break;
                    case '/':
                        tk.Type = TokenType.Slash;
                        NextCh();
                        if (ch == '/')
                        {
                            int l = line;
                            while (l == line && ch != EOF)
                                NextCh();
                            return Next();
                        }
                        break;
                    case '*':
                        tk.Type = TokenType.Times;
                        NextCh();
                        break;
                    case '%':
                        tk.Type = TokenType.Remainder;
                        NextCh();
                        break;
                    case '>':
                        tk.Type = TokenType.GT;
                        NextCh();
                        if (ch == '=')
                        {
                            tk.Type = TokenType.GET;
                            NextCh();
                        }
                        break;
                    case '<':
                        tk.Type = TokenType.LT;
                        NextCh();
                        if (ch == '=')
                        {
                            tk.Type = TokenType.LET;
                            NextCh();
                        }
                        break;
                    case '&':
                        tk.Type = TokenType.AND;
                        NextCh();
                        if (ch == '&')
                            tk.Type = TokenType.ANDALSO;
                        break;
                    case '|':
                        tk.Type = TokenType.OR;
                        NextCh();
                        if (ch == '|')
                            tk.Type = TokenType.ORELSE;
                        break;
                    case '=':
                        tk.Type = TokenType.SET;
                        NextCh();
                        if (ch == '=')
                        {
                            tk.Type = TokenType.EQ;
                            NextCh();
                        }
                        break;
                    case '!':
                        NextCh();
                        if (ch == '=')
                        {
                            tk.Type = TokenType.DIF;
                            NextCh();
                        }
                        else
                        {
                            Error.Errors.Add("Unexpected char: '" + ch + "' at " + tk.Line.ToString() + ":" +
                                             tk.Col.ToString());
                            throw new Exception();
                        }
                        break;
                    case EOF:
                        tk.Type = TokenType.EOF;
                        break;
                    default:
                        Error.Errors.Add("Unexpected token: '" + ch + "' at " + tk.Line.ToString() + ":" +
                                         tk.Col.ToString());
                        throw new Exception();
                }
            }

            return tk;
        }

        private void ReadString(ref Token tk)
        {
            NextCh();

            string txt = "";

            while (true)
            {
                char c = ch;

                if (ch == '"' || ch == '”')
                {
                    NextCh();

                    if (ch == '"' || ch == '”' && (nextCH == '\n' || nextCH == '\r'))
                    {
                        NextCh();
                        break;
                    }

                    break;
                }
                else if (ch == EOF)
                {
                    NextCh();

                    break;
                }
                else if (c == '\\')
                {
                    if (nextCH == '"')
                    {
                        NextCh();
                    }

                    txt += c;
                    NextCh();
                }
                else
                {
                    txt += c;
                    NextCh();
                }
            }

            tk.Type = TokenType.StringLiteral;
            tk.Value = txt;
        }

        private void SkipWhiteSpace()
        {
            while (ch == ' ' || ch == '\t')
                NextCh();
        }

        public void Match(char c)
        {
            if (ch == c)
            {
                NextCh();
                return;
            }

            Error.Errors.Add("Expected '" + c + "' found '" + ch + "' at " + line.ToString() + ":" + col.ToString());
            throw new Exception();
        }

        private void ReadIdentifier(ref Token tk)
        {
            string str = "";

            while (IsAlpha(ch) || ch == '_' || ch == '\'')
            {
                str += ch;
                NextCh();
            }

            tk.Type = GetIdentType(str);
            if (ch == ':')
            {
                tk.Type = TokenType.Label;
                NextCh();
            }
            else if (ch == '[')
            {
                Match('[');
                str = "";
                while (IsAlpha(ch) || ch == '_' || ch == '\'')
                {
                    str += ch;
                    NextCh();
                }
                tk.Type = TokenType.Var;
                Match(']');
            }

            tk.Value = str;
        }

        private TokenType GetIdentType(string str)
        {
            str = str.ToLower();
            if (str == "if")
                return TokenType.If;
            else if (str == "while")
                return TokenType.While;
            else if (str == "elseif")
                return TokenType.ElseIf;
            else if (str == "else")
                return TokenType.Else;
            else if (str == "choose")
                return TokenType.Choose;
            else if (str == "case")
                return TokenType.Case;
            else if (str == "endif")
                return TokenType.EndIf;
            else if (str == "npc")
                return TokenType.npc;
            else if (str == "trader")
                return TokenType.trader;
            else if (str == "var")
                return TokenType.VarDecl;
            else if (str == "endif")
                return TokenType.EndIf;
            else if (str == "endwhile")
                return TokenType.EndWhile;
            else if (str == "endchoose")
                return TokenType.EndChoose;
            else if (str == "return")
                return TokenType.Return;
            else if (str == "putmob")
                return TokenType.putmob;
            else if (str == "putboss")
                return TokenType.putboss;
            else if (str == "warp")
                return TokenType.warp;
            else if (str == "hiddenwarp")
                return TokenType.hiddenwarp;
            else if (str == "arenaguide")
                return TokenType.npc;
            else
                return TokenType.Identifier;
        }

        private void ReadNumber(ref Token tk)
        {
            string str = "";

            while (IsNumber(ch))
            {
                str += ch;
                NextCh();
            }

            if (IsLetter(ch) || ch == '_')
            {
                while (IsAlpha(ch) || ch == '_' || ch == '\'')
                {
                    str += ch;
                    NextCh();
                }

                tk.Type = GetIdentType(str);
                if (ch == ':')
                {
                    tk.Type = TokenType.Label;
                    NextCh();
                }
                else if (ch == '[')
                {
                    Match('[');
                    str = "";
                    while (IsAlpha(ch) || ch == '_' || ch == '\'')
                    {
                        str += ch;
                        NextCh();
                    }
                    tk.Type = TokenType.Var;
                    Match(']');
                }

                tk.Value = str;
                return;
            }

            tk.Type = TokenType.DecLiteral;
            tk.Value = str;
        }

        public bool IsNumber(char c)
        {
            return (c >= '0' && c <= '9');
        }

        public bool IsLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public bool IsAlpha(char c)
        {
            return (IsNumber(c) || IsLetter(c));
        }

        public void NextCh()
        {
            last = ch;

            try
            {
                ch = (char) tr.Read();

                if (ch != '\t' && ch != '\n' && ch != '\r')
                    catcher += ch;

                if (ch == '\n')
                {
                    line++;
                    col = 0;
                    lcatcher = catcher;
                    catcher = "";
                }
                else if (ch == '\r')
                {
                    NextCh();
                    return;
                }
                else
                    col++;
            }
            catch
            {
                ch = EOF;
            }

            try
            {
                nextCH = (char) tr.Peek();
            }
            catch
            {
                ch = EOF;
            }
        }

        internal string CatchToTheEnd()
        {
            int l = line;
            while (l == line && ch != EOF)
                NextCh();
            return lcatcher;
        }
    }
}