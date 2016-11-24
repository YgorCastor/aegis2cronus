using System.Collections.Generic;
using System.IO;

namespace Aegis2Cronus.Aegis
{
    internal abstract class Expr : AegisItem
    {
    }

    internal class ComposExpr : Expr
    {
        public Expr Exp;

        public override string ToString()
        {
            return "(" + Exp + ")";
        }
    }

    internal class StringLiteral : Expr
    {
        public string Value;

        public override string ToString()
        {
            return "\"" + Value + "\"";
        }
    }

    internal class IntLiteral : Expr
    {
        public int Value;

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class IdentLiteral : Expr
    {
        public string Value;

        public override string ToString()
        {
            if (!Variable.haxforever.Contains(Value.ToLower()))
                Variable.haxforever.Add(Value.ToLower());
            return ".@" + Value;
        }
    }

    internal class Variable : Expr
    {
        public static List<string> haxforever;
        private static readonly Dictionary<string, string> varsFromFile;
        public string Ident;
        public string type;

        static Variable()
        {
            haxforever = new List<string>();
            varsFromFile = new Dictionary<string, string>();

            string[] lines = File.ReadAllLines("./vars.txt");
            foreach (string lineR in lines)
            {
                string line = lineR.Trim();
                string[] parts = line.Split(':');

                if (parts.Length == 2)
                {
                    varsFromFile.Add(parts[0].ToLower(), parts[1]);
                }
                else
                    varsFromFile.Add(parts[0].ToLower(), "");
            }
        }

        public override string ToString()
        {
            if (type == "const")
                return GetVEspecial(Ident);
            else if (type == "var")
                return GetEspecial(Ident);
            else if (type == "item")
            {
                if (CodeGen.endLineComment != "")
                    CodeGen.endLineComment += ", " + CodeGen.GetItemName(CodeGen.GetItemId(Ident));
                else
                    CodeGen.endLineComment = CodeGen.GetItemName(CodeGen.GetItemId(Ident));

                return "countitem(" + CodeGen.GetItemId(Ident) + ")";
            }
            else
            {
                if (Ident == "input")
                    return ".@input";
                else if (Ident == "inputstr")
                    return ".@inputstr$";
                else if (haxforever.Contains(Ident.ToLower()))
                    return ".@" + Ident;
                else
                    return Ident;
            }
        }

        private static string GetEspecial(string s)
        {
            if (varsFromFile.ContainsKey(s.ToLower()))
                return varsFromFile[s.ToLower()];
            else if (s == "input")
                return ".@input";
            else if (s == "inputstr")
                return ".@inputstr$";
            else if (haxforever.Contains(s.ToLower()))
                return ".@" + s;
            else
                return s;
        }

        private static string GetVEspecial(string s)
        {
            if (varsFromFile.ContainsKey(s.ToLower()))
            {
                if (varsFromFile[s.ToLower()] != "")
                    return varsFromFile[s.ToLower()];
            }

            if (CodeGen.endLineComment != "")
                CodeGen.endLineComment += ", Fix Const";
            else
                CodeGen.endLineComment = "Fix Const";

            return "v[" + s + "]";
        }
    }

    internal class FuncCall : Expr
    {
        public List<Expr> Args;
        public string Name;

        public override string ToString()
        {
            string txt = "";

            foreach (Expr exp in Args)
                txt += exp.ToString();

            return txt;
        }
    }

    internal class Arith : Expr
    {
        public static string[] ArithChar = new[] {"+", "-", "*", "/"};
        public Expr Left;
        public ArithOp Op;
        public Expr Right;

        public override string ToString()
        {
            return Left + " " + ArithChar[(int) Op] + " " + Right;
        }
    }

    internal enum ArithOp
    {
        Add,
        Sub,
        Mul,
        Div
    }

    internal class Comp : Expr
    {
        public static string[] CompChar = new[] {"==", "!=", "&&", "||", ">", "<", ">=", "<=", "==", "!=", "&&", "||"};
        public Expr Left;
        public CompOp Op;
        public Expr Right;

        public override string ToString()
        {
            return Left + " " + CompChar[(int) Op] + " " + Right;
        }
    }

    internal enum CompOp
    {
        Equals = 0,
        Difer,
        And,
        Or,
        GT,
        LT,
        GET,
        LET,
        EQ,
        DIF,
        AND,
        OR
    }
}