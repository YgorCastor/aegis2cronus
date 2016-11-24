using System;
using System.Collections.Generic;
using System.Linq;

namespace Aegis2Cronus.Aegis
{
    internal class Parser
    {
        private readonly Stack<AegisItem> sai = new Stack<AegisItem>();

        public TokenType[] ValuesExprs = new[]
                                             {
                                                 TokenType.DecLiteral, TokenType.StringLiteral, TokenType.Identifier,
                                                 TokenType.Var
                                             };

        private AegisItem curNode;
        private bool fallback;
        private bool m_canfunc;

        public Scanner s;
        public Token tk;

        public Parser(Scanner s)
        {
            this.s = s;
            tk = s.Next();
        }

        private AegisItem MatchPutB()
        {
            var an = new AegisPutMob();

            Match(TokenType.putboss);

            an.Map = MatchString();
            an.x1 = MatchNumber();
            an.y1 = MatchNumber();
            an.x2 = MatchNumber();
            an.y2 = MatchNumber();
            an.amount = MatchNumber();
            an.name = MatchIdent();
            an.delay1 = MatchNumber();
            an.delay2 = MatchNumber();
            an.event_ = MatchNumber();

            return an;
        }

        private AegisItem MatchPutM()
        {
            var an = new AegisPutMob();

            Match(TokenType.putmob);

            an.Map = MatchString();
            an.x1 = MatchNumber();
            an.y1 = MatchNumber();
            an.x2 = MatchNumber();
            an.y2 = MatchNumber();
            an.amount = MatchNumber();
            an.name = MatchIdent();
            an.delay1 = MatchNumber();
            an.delay2 = MatchNumber();
            an.event_ = MatchNumber();
            return an;
        }


        private AegisItem MatchHiddenWarp()
        {
            var an = new AegisHiddenWarp();

            Match(TokenType.hiddenwarp);

            an.MapName = MatchString();
            an.name = MatchString();
            an.x = MatchNumber();
            an.y = MatchNumber();
            an.spanx = MatchNumber();
            an.spany = MatchNumber();
            if (tk.Type == TokenType.DecLiteral)
                an.unknow = MatchNumber();
            PushNode(an);

            MatchNewline();

            while (tk.Type == TokenType.Label)
            {
                MatchNewline();
                curNode.Items.Add(MatchLabel());
                MatchNewline();
            }

            an = (AegisHiddenWarp) PopNode();
            return an;
        }

        public AegisWarp MatchWarp()
        {
            var an = new AegisWarp();

            Match(TokenType.warp);

            an.MapName = MatchString();
            an.name = MatchString();
            an.x = MatchNumber();
            an.y = MatchNumber();
            an.spanx = MatchNumber();
            an.spany = MatchNumber();

            PushNode(an);

            MatchNewline();

            while (tk.Type == TokenType.Label)
            {
                MatchNewline();
                curNode.Items.Add(MatchLabel());
                MatchNewline();
            }

            an = (AegisWarp) PopNode();
            return an;
        }

        private AegisTrader MatchTrader()
        {
            var an = new AegisTrader();

            Match(TokenType.trader);

            an.Map = MatchString();
            an.Name = MatchString();
            an.Sprite = MatchNumberOrIdent();
            an.X = MatchNumber();
            an.Y = MatchNumber();
            an.Dir = MatchNumber();
            an.W = MatchNumber();
            an.H = MatchNumber();
            PushNode(an);

            MatchNewline();
            while (tk.Type == TokenType.Label)
            {
                curNode.Items.Add(MatchLabel());
                MatchNewline();
            }

            an = (AegisTrader) PopNode();
            return an;
        }

        private string MatchNumberOrIdent()
        {
            if (tk.Type == TokenType.DecLiteral)
                return MatchNumber();
            else
                return MatchIdent();
        }

        private AegisNpc MatchNpc()
        {
            var an = new AegisNpc();

            Match(TokenType.npc);

            an.Map = MatchString();
            an.Name = MatchString();
            an.Sprite = MatchNumberOrIdent();
            an.X = MatchNumber();
            an.Y = MatchNumber();
            an.Dir = MatchNumber();
            an.W = MatchNumber();
            an.H = MatchNumber();

            PushNode(an);
            MatchNewline();

            do
            {
                MatchNewline();
                curNode.Items.Add(MatchLabel());
                MatchNewline();
            } while (tk.Type == TokenType.Label);
            an = (AegisNpc) PopNode();

            return an;
        }

        private void MatchNewline()
        {
            while (tk.Type == TokenType.NewLine)
                Match(TokenType.NewLine);
        }

        private AegisLabel MatchLabel()
        {
            string n = tk.Value;
            Expr exp = null;
            Match(TokenType.Label);

            if (tk.Type == TokenType.NewLine)
                MatchNewline();
            else
            {
                exp = MatchExpr();

                MatchNewline();
            }

            var al = new AegisLabel();
            al.Exp = exp;
            al.Name = n;
            PushNode(al);

            curNode.Items = MatchBlock().Items;

            al = (AegisLabel) PopNode();

            return al;
        }

        private AegisItem MatchBlock()
        {
            var ai = new AegisItem();
            PushNode(ai);

            while (tk.Type != TokenType.Label && tk.Type != TokenType.npc && tk.Type != TokenType.trader &&
                   tk.Type != TokenType.warp && tk.Type != TokenType.putmob && tk.Type != TokenType.putboss &&
                   tk.Type != TokenType.hiddenwarp && tk.Type != TokenType.EOF)
            {
                MatchNewline();
                if (tk.Type == TokenType.Return)
                {
                    MatchNewline();
                    curNode.Items.Add(new AegisFunc {Name = "return", Items = new List<AegisItem>()});
                    Match(TokenType.Return);
                }
                else
                {
                    AegisItem aii = MatchStmt();
                    if (fallback)
                    {
                        fallback = false;
                        continue;
                    }
                    curNode.Items.Add(aii);
                }
                MatchNewline();
            }
            ai = PopNode();

            return ai;
        }

        private AegisItem MatchCaseBlock()
        {
            var ai = new AegisItem();

            PushNode(ai);
            while (tk.Type != TokenType.Label && tk.Type != TokenType.npc && tk.Type != TokenType.trader &&
                   tk.Type != TokenType.warp && tk.Type != TokenType.putmob && tk.Type != TokenType.putboss &&
                   tk.Type != TokenType.hiddenwarp && tk.Type != TokenType.EOF && tk.Type != TokenType.EndChoose &&
                   tk.Type != TokenType.Case && tk.Type != TokenType.EOF)
            {
                AegisItem aii = MatchStmt();
                if (fallback)
                {
                    fallback = false;
                    continue;
                }
                curNode.Items.Add(aii);
            }
            ai = PopNode();

            return ai;
        }

        private AegisItem MatchStmt()
        {
            if (tk.Type == TokenType.Identifier)
                return MatchFunc();
            else if (tk.Type == TokenType.If)
                return MatchIf();
            else if (tk.Type == TokenType.While)
                return MatchWhile();
            else if (tk.Type == TokenType.VarDecl)
                return MatchVarDecl();
            else if (tk.Type == TokenType.Choose)
                return MatchChoose();
            else
                Match(tk.Type);

            return null;
        }

        private AegisChoose MatchChoose()
        {
            Match(TokenType.Choose);

            Expr exp = MatchExpr();
            MatchNewline();

            var ac = new AegisChoose();
            ac.Exp = exp;

            while (tk.Type == TokenType.NewLine)
                MatchNewline();
            PushNode(ac);
            while (tk.Type == TokenType.Case)
            {
                curNode.Items.Add(MatchCase());
                while (tk.Type == TokenType.NewLine)
                    MatchNewline();
            }
            ac = (AegisChoose) PopNode();

            while (tk.Type == TokenType.NewLine)
                MatchNewline();
            Match(TokenType.EndChoose);

            return ac;
        }

        private AegisCase MatchCase()
        {
            Match(TokenType.Case);

            Expr exp = MatchExpr();
            MatchNewline();

            var ac = new AegisCase();
            ac.Exp = exp;
            PushNode(ac);
            curNode.Items = MatchCaseBlock().Items;
            ac = (AegisCase) PopNode();

            while (tk.Type == TokenType.NewLine)
                MatchNewline();
            return ac;
        }

        private AegisWhile MatchWhile()
        {
            Match(TokenType.While);

            Expr exp = MatchExpr();
            MatchNewline();

            var ai = new AegisWhile();
            ai.Exp = exp;
            PushNode(ai);
            curNode.Items = MatchWhileBlock().Items;
            ai = (AegisWhile) PopNode();

            Match(TokenType.EndWhile);
            Match(TokenType.NewLine);

            return ai;
        }

        private AegisItem MatchIfBlock()
        {
            var ai = new AegisItem();

            PushNode(ai);
            while (tk.Type != TokenType.Label && tk.Type != TokenType.npc && tk.Type != TokenType.trader &&
                   tk.Type != TokenType.warp && tk.Type != TokenType.putmob && tk.Type != TokenType.putboss &&
                   tk.Type != TokenType.hiddenwarp && tk.Type != TokenType.EOF && tk.Type != TokenType.EndIf &&
                   tk.Type != TokenType.EOF && tk.Type != TokenType.Else && tk.Type != TokenType.ElseIf)
            {
                AegisItem aii = MatchStmt();
                if (fallback)
                {
                    fallback = false;
                    continue;
                }
                curNode.Items.Add(aii);
            }
            ai = PopNode();

            return ai;
        }

        private AegisItem MatchWhileBlock()
        {
            var ai = new AegisItem();

            PushNode(ai);
            while (tk.Type != TokenType.Label && tk.Type != TokenType.npc && tk.Type != TokenType.trader &&
                   tk.Type != TokenType.warp && tk.Type != TokenType.putmob && tk.Type != TokenType.putboss &&
                   tk.Type != TokenType.hiddenwarp && tk.Type != TokenType.EOF && tk.Type != TokenType.EndWhile &&
                   tk.Type != TokenType.EOF)
            {
                AegisItem aii = MatchStmt();
                if (fallback)
                {
                    fallback = false;
                    continue;
                }
                curNode.Items.Add(aii);
            }
            ai = PopNode();

            return ai;
        }

        private AegisVarDecl MatchVarDecl()
        {
            Match(TokenType.VarDecl);

            var vd = new AegisVarDecl();
            vd.Name = MatchIdent();

            if (tk.Type == TokenType.SET)
            {
                Match(TokenType.SET);

                vd.Exp = MatchExpr();
                if (fallback)
                    return null;
                return vd;
            }
            else
                return null;
        }

        private AegisIf MatchIf()
        {
            if (tk.Type == TokenType.If)
                Match(TokenType.If);
            else
                Match(TokenType.ElseIf);

            if (fallback)
                return null;

            if (tk.Type == TokenType.LPar)
                Match(TokenType.LPar);
            Expr exp = MatchExpr();
            if (fallback)
                return null;
            if (tk.Type == TokenType.RPar)
                Match(TokenType.RPar);

            if (tk.Type == TokenType.NewLine)
                MatchNewline();

            var ai = new AegisIf();
            PushNode(ai);
            curNode.Items = MatchIfBlock().Items;
            ai = (AegisIf) PopNode();
            ai.Exp = exp;
            ai.ElseIfs = new List<AegisIf>();
            while (tk.Type == TokenType.ElseIf)
            {
                ai.ElseIfs.Add(MatchEElseIf());
            }

            if (tk.Type == TokenType.Else)
            {
                Match(TokenType.Else);
                ai.Else = MatchIfBlock();
            }

            Match(TokenType.EndIf);
            Match(TokenType.NewLine);

            return ai;
        }

        private AegisIf MatchEElseIf()
        {
            if (tk.Type == TokenType.If)
                Match(TokenType.If);
            else
                Match(TokenType.ElseIf);

            Expr exp = MatchExpr();
            MatchNewline();

            var ai = new AegisIf();
            ai.Exp = exp;


            PushNode(ai);
            curNode.Items = MatchIfBlock().Items;
            ai = (AegisIf) PopNode();


            return ai;
        }

        private AegisFunc MatchFunc()
        {
            var af = new AegisFunc();

            af.Name = MatchIdent();

            if (af.Name == "showimage")
            {
            }

            while (tk.Type != TokenType.NewLine && tk.Type != TokenType.EOF)
            {
                Expr exp = MatchExpr(false);

                if (fallback)
                    return null;

                af.Items.Add(exp);
            }

            MatchNewline();

            return af;
        }

        private Expr MatchExpr()
        {
            return MatchExpr(true);
        }

        private Expr MatchExpr(bool canFunc)
        {
            m_canfunc = canFunc;
            return MatchLog();
        }

        private Expr MatchExpr2()
        {
            return MatchLog();
        }

        public Expr MatchLog()
        {
            Expr left = MatchComp();

            if (fallback)
                return null;

            if (tk.Type == TokenType.AND)
            {
                var a = new Comp();
                a.Op = CompOp.AND;
                Match(TokenType.AND);
                a.Left = left;
                a.Right = MatchLog();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.OR)
            {
                var a = new Comp();
                a.Op = CompOp.OR;
                Match(TokenType.OR);
                a.Left = left;
                a.Right = MatchLog();

                if (fallback)
                    return null;

                return a;
            }

            if (fallback)
                return null;

            return left;
        }

        public Expr MatchComp()
        {
            Expr left = MatchGTS();

            if (fallback)
                return null;

            if (tk.Type == TokenType.EQ)
            {
                var a = new Comp();
                a.Op = CompOp.EQ;
                Match(TokenType.EQ);
                a.Left = left;
                a.Right = MatchComp();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.DIF)
            {
                var a = new Comp();
                a.Op = CompOp.DIF;
                Match(TokenType.DIF);
                a.Left = left;
                a.Right = MatchComp();

                if (fallback)
                    return null;

                return a;
            }

            if (fallback)
                return null;

            return left;
        }

        public Expr MatchGTS()
        {
            Expr left = MatchTerm();

            if (fallback)
                return null;

            if (tk.Type == TokenType.GT)
            {
                var a = new Comp();
                a.Op = CompOp.GT;
                Match(TokenType.GT);
                a.Left = left;
                a.Right = MatchGTS();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.LT)
            {
                var a = new Comp();
                a.Op = CompOp.LT;
                Match(TokenType.LT);
                a.Left = left;
                a.Right = MatchGTS();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.GET)
            {
                var a = new Comp();
                a.Op = CompOp.GET;
                Match(TokenType.GET);
                a.Left = left;
                a.Right = MatchGTS();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.LET)
            {
                var a = new Comp();
                a.Op = CompOp.LET;
                Match(TokenType.LET);
                a.Left = left;
                a.Right = MatchGTS();

                if (fallback)
                    return null;

                return a;
            }

            if (fallback)
                return null;

            return left;
        }

        public Expr MatchTerm()
        {
            Expr left = MatchFactor();

            if (fallback)
                return null;

            if (tk.Type == TokenType.Plus)
            {
                var a = new Arith();
                a.Op = ArithOp.Add;
                Match(TokenType.Plus);
                a.Left = left;
                a.Right = MatchTerm();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.Minus)
            {
                var a = new Arith();
                a.Op = ArithOp.Sub;
                Match(TokenType.Minus);
                a.Left = left;
                a.Right = MatchTerm();

                if (fallback)
                    return null;

                return a;
            }

            if (fallback)
                return null;

            return left;
        }

        public Expr MatchFactor()
        {
            Expr left = MatchValue();

            if (fallback)
                return null;

            if (tk.Type == TokenType.Times)
            {
                var a = new Arith();
                a.Op = ArithOp.Mul;
                Match(TokenType.Times);
                a.Left = left;
                a.Right = MatchFactor();

                if (fallback)
                    return null;

                return a;
            }
            else if (tk.Type == TokenType.Slash)
            {
                var a = new Arith();
                a.Op = ArithOp.Div;
                Match(TokenType.Slash);
                a.Left = left;
                a.Right = MatchFactor();

                if (fallback)
                    return null;

                return a;
            }

            if (fallback)
                return null;

            return left;
        }

        public Expr MatchValue()
        {
            if (tk.Type == TokenType.DecLiteral)
            {
                var il = new IntLiteral();
                il.Value = int.Parse(tk.Value);
                Match(tk.Type);
                return il;
            }
            else if (tk.Type == TokenType.StringLiteral)
            {
                var il = new StringLiteral();
                il.Value = tk.Value;
                Match(tk.Type);
                return il;
            }
            else if (tk.Type == TokenType.Identifier)
            {
                string txt = tk.Value;
                Match(tk.Type);

                if (ValuesExprs.Contains(tk.Type) && m_canfunc)
                {
                    var il = new FuncCall();
                    il.Name = txt;

                    if (tk.Type == TokenType.LPar)
                        Match(tk.Type);

                    il.Args = new List<Expr>();

                    while (tk.Type != TokenType.NewLine && tk.Type != TokenType.EOF && tk.Type != TokenType.RPar)
                    {
                        Expr expr = MatchExpr();

                        il.Args.Add(expr);
                    }

                    if (tk.Type == TokenType.RPar)
                        Match(tk.Type);

                    return il;
                }
                else
                {
                    var il = new Variable();

                    il.Ident = txt;
                    il.type = "var";
                    return il;
                }
            }
            else if (tk.Type == TokenType.Var)
            {
                var il = new Variable();

                il.Ident = tk.Value;

                if (il.Ident == null)
                    il.Ident = "";

                if (il.Ident == il.Ident.ToUpper())
                    il.type = "const";
                else if (il.Ident[0] >= 'a' && il.Ident[0] <= 'z')
                    il.type = "var";
                else if (il.Ident[0] >= 'A' && il.Ident[0] <= 'Z')
                    il.type = "item";
                else
                    il.type = "var";

                Match(tk.Type);
                return il;
            }
            else if (tk.Type == TokenType.LPar)
            {
                Match(TokenType.LPar);
                var exp = new ComposExpr();
                exp.Exp = MatchExpr();
                Match(TokenType.RPar);
                return exp;
            }

            LineFallback();

            return null;
        }

        private string MatchNumber()
        {
            if (tk.Type != TokenType.DecLiteral)
            {
                LineFallback();
            }

            string str = tk.Value;
            tk = s.Next();
            return str;
        }

        private string MatchIdent()
        {
            if (tk.Type != TokenType.Identifier)
            {
                LineFallback();
            }

            string str = tk.Value;
            tk = s.Next();
            return str;
        }

        private string MatchString()
        {
            if (tk.Type != TokenType.StringLiteral)
            {
                LineFallback();
            }

            string str = tk.Value;
            tk = s.Next();
            return str;
        }

        public void PushNode(AegisItem ai)
        {
            sai.Push(curNode);
            curNode = ai;
        }

        public AegisItem PopNode()
        {
            AegisItem ret = curNode;
            curNode = sai.Pop();

            return ret;
        }

        public void Match(TokenType tt)
        {
            if (tk.Type == tt)
            {
                tk = s.Next();
                return;
            }

            LineFallback();
        }

        public void LineFallback()
        {
            string str = s.CatchToTheEnd();
            var au = new AegisUnknown();
            au.str = str;
            curNode.Items.Add(au);
            fallback = true;
        }

        internal AegisBody Parse()
        {
            var ab = new AegisBody();

            PushNode(ab);
            while (true)
            {
                if (tk.Type == TokenType.EOF)
                {
                    break;
                }
                else if (tk.Type == TokenType.npc)
                {
                    curNode.Items.Add(MatchNpc());
                }
                else if (tk.Type == TokenType.trader)
                {
                    curNode.Items.Add(MatchTrader());
                }
                else if (tk.Type == TokenType.warp)
                {
                    curNode.Items.Add(MatchWarp());
                }
                else if (tk.Type == TokenType.putmob)
                {
                    curNode.Items.Add(MatchPutM());
                }
                else if (tk.Type == TokenType.putboss)
                {
                    curNode.Items.Add(MatchPutB());
                }
                else if (tk.Type == TokenType.hiddenwarp)
                {
                    curNode.Items.Add(MatchHiddenWarp());
                }
                else if (tk.Type == TokenType.arenaguide)
                {
                    curNode.Items.Add(MatchArenaGuide());
                }
                else if (tk.Type == TokenType.NewLine)
                    Match(TokenType.NewLine);
                else
                {
                    if (tk.Value != "")
                        Error.Errors.Add("Unexpected " + tk.Type + " - '" + tk.Value + "' at " + tk.Line.ToString() +
                                         ":" + tk.Col.ToString());
                    else
                        Error.Errors.Add("Unexpected " + tk.Type + " at " + tk.Line.ToString() + ":" + tk.Col.ToString());
                    throw new Exception();
                }
            }
            ab = (AegisBody) PopNode();

            return ab;
        }

        private AegisItem MatchArenaGuide()
        {
            var an = new AegisNpc();

            Match(TokenType.npc);

            an.Map = MatchString();
            an.Name = MatchString();
            an.Sprite = MatchNumberOrIdent();
            an.X = MatchNumber();
            an.Y = MatchNumber();
            an.Dir = MatchNumber();
            an.W = MatchNumber();
            an.H = MatchNumber();

            PushNode(an);
            MatchNewline();

            do
            {
                MatchNewline();
                curNode.Items.Add(MatchLabel());
                MatchNewline();
            } while (tk.Type == TokenType.Label);
            an = (AegisNpc) PopNode();

            return an;
        }
    }
}