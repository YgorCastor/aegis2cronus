using System;
using System.Collections.Generic;
using System.IO;

namespace Aegis2Cronus.Aegis
{
    internal class CodeGen
    {
        public static List<string[]> itemsdb;
        public static List<string[]> itemsdb2;
        public static List<string[]> mobsdb;
        public static List<string[]> mobsnamedb;
        private readonly Dictionary<string, Func<string>> funcs;
        private readonly Parser p;
        private List<AegisItem> curArgs;
        private AegisItem curNpc;
        private AegisFunc lastFunc;
        private string sw;
        private int tabLevel;

        static CodeGen()
        {
            itemsdb = new List<string[]>();

            foreach (string str in File.ReadAllLines("./itemdb.txt"))
            {
                itemsdb.Add(str.Split(','));
            }

            itemsdb2 = new List<string[]>();

            foreach (string str in File.ReadAllLines("./itemdb_names.txt"))
            {
                itemsdb2.Add(str.Split(','));
            }

            mobsdb = new List<string[]>();

            foreach (string str in File.ReadAllLines("./mobdb.txt"))
            {
                mobsdb.Add(str.Split(','));
            }

            mobsnamedb = new List<string[]>();

            foreach (string str in File.ReadAllLines("./mobnames.txt"))
            {
                mobsnamedb.Add(str.Split(','));
            }
        }

        public CodeGen(Parser p)
        {
            this.p = p;
            sw = "";
            tabLevel = 0;
            funcs = new Dictionary<string, Func<string>>();

            InitializeFuncs();
        }

        private string Ident()
        {
            string str = "";

            for (int i = 0; i < tabLevel; i++)
                str += '\t';

            return str;
        }

        private void WriteNew(string str, params object[] args)
        {
            sw += Ident() + string.Format(str, args);
        }

        private void WriteNewLine(string str, params object[] args)
        {
            sw += Ident() + string.Format(str, args);

            if (endLineComment != "")
            {
                sw += " //" + endLineComment;
                endLineComment = "";
            }

            sw += "\n";
        }

        private void Write(string str, params object[] args)
        {
            sw += string.Format(str, args);
        }

        private void WriteLine(string str, params object[] args)
        {
            sw += string.Format(str, args);

            if (endLineComment != "")
            {
                sw += " //" + endLineComment;
                endLineComment = "";
            }

            sw += "\n";
        }

        private void GenBoss(AegisPutBoss ai)
        {
            WriteNewLine(
                "{0},{1},{2},{3},{4}\tboss_monster\t" + GetMobName(GetMobId(ai.name)) + "\t{5},{6},{7},{8},{9}", ai.Map,
                ai.x1, ai.y1, ai.x2, ai.y2, GetMobId(ai.name), ai.amount, ai.delay1, ai.delay2, ai.event_);
        }

        private void GenMonster(AegisPutMob ai)
        {
            WriteNewLine("{0},{1},{2},{3},{4}\tmonster\t" + GetMobName(GetMobId(ai.name)) + "\t{5},{6},{7},{8},{9}",
                         ai.Map, ai.x1, ai.y1, ai.x2, ai.y2, GetMobId(ai.name), ai.amount, ai.delay1, ai.delay2,
                         ai.event_);
        }

        private void GenHWarp(AegisHiddenWarp ai)
        {
            curNpc = ai;

            if (ai.spanx != "0" || ai.spany != "0")
                WriteNewLine("{0},{1},{2},{3}\tscript\t{4}\t{5},{6},{7},{8}", ai.MapName, ai.x, ai.y, 0, ai.name, "-1",
                             ai.spanx, ai.spany, "{");
            else
                WriteNewLine("{0},{1},{2},{3}\tscript\t{4}\t{5},{6}", ai.MapName, ai.x, ai.y, 0, ai.name, "-1", "{");

            foreach (AegisLabel al in ai.Items)
            {
                if (al.Name == "OnClick")
                {
                    GenLabel(al);
                    break;
                }
            }

            foreach (AegisLabel al in ai.Items)
            {
                if (al.Name == "OnClick")
                {
                    continue;
                }

                GenLabel(al);
            }

            WriteNewLine("{0}\n", "}");
        }

        private void GenTrader(AegisTrader at)
        {
            AegisLabel oninit = null;
            foreach (AegisLabel al in at.Items)
            {
                if (al.Name == "OnInit")
                {
                    oninit = al;
                    break;
                }
            }

            if (oninit == null)
                return;

            var items = new List<string>();
            foreach (AegisFunc af in oninit.Items)
            {
                if (af != null && af.Name == "sellitem")
                {
                    curArgs = af.Items;
                    items.Add(FindArg(0));
                }
            }

            WriteNew("{0},{1},{2},{3}\tshop\t{4},{5},", at.Map, at.X, at.Y, at.Dir, at.Name, at.Sprite);

            for (int i = 0; i < items.Count; i++)
            {
                if (i == items.Count - 1)
                    Write("{0}:-1", GetItemId(items[i]));
                else
                    Write("{0}:-1,", GetItemId(items[i]));
            }

            WriteLine("\n");
        }

        private static string GetMobId(string p)
        {
            foreach (var item in mobsdb)
            {
                if (item.Length >= 2)
                    if (item[1] == p)
                        return item[0];
            }

            return "1002";
        }

        private string GetMobName(string p)
        {
            foreach (var item in mobsnamedb)
            {
                if (item.Length >= 2)
                    if (item[0] == p)
                        return item[2];
            }


            return p;
        }

        public static string GetItemId(string p)
        {
            foreach (var item in itemsdb)
            {
                if (item.Length >= 2)
                    if (item[1] == p)
                        return item[0];
            }

            return p;
        }

        public static string GetItemName(string p)
        {
            foreach (var item in itemsdb2)
            {
                if (item.Length >= 3)
                    if (item[0] == p)
                        return item[2];
            }

            return "Unknown_Item";
        }

        private void GenWarp(AegisWarp aw)
        {
            AegisLabel ontouch = null;
            foreach (AegisLabel al in aw.Items)
            {
                if (al.Name == "OnTouch")
                {
                    ontouch = al;
                    break;
                }
            }

            if (ontouch == null)
                return;

            AegisFunc moveto = null;
            foreach (AegisFunc af in ontouch.Items)
            {
                if (af.Name == "moveto")
                {
                    moveto = af;
                    break;
                }
            }

            if (moveto == null)
                return;

            curArgs = moveto.Items;

            WriteNewLine("{0},{1},{2},{3}\twarp\t{4}\t{5},{6},{7},{8},{9}\n", aw.MapName, aw.x, aw.y, "0", aw.name,
                         aw.spanx, aw.spany, FindArg(0).Substring(1, FindArg(0).Length - 2), FindArg(1), FindArg(2));
        }

        private void GenNpc(AegisNpc ai)
        {
            curNpc = ai;

            if (ai.W != "0" || ai.H != "0")
                WriteNewLine("{0},{1},{2},{3}\tscript\t{4}\t{5},{6},{7},{8}", ai.Map, ai.X, ai.Y, ai.Dir, ai.Name,
                             ai.Sprite, ai.W, ai.H, "{");
            else
                WriteNewLine("{0},{1},{2},{3}\tscript\t{4}\t{5},{6}", ai.Map, ai.X, ai.Y, ai.Dir, ai.Name, ai.Sprite,
                             "{");

            foreach (AegisItem aii in ai.Items)
            {
                if (aii.GetType() == typeof (AegisLabel))
                {
                    var al = (AegisLabel) aii;
                    if (al.Name == "OnClick")
                    {
                        GenLabel(al);
                        break;
                    }
                }
                else
                {
                    var al = (AegisUnknown) aii;
                    GenUnk(al);
                }
            }

            foreach (AegisItem aii in ai.Items)
            {
                if (aii.GetType() == typeof (AegisLabel))
                {
                    var al = (AegisLabel) aii;
                    if (al.Name == "OnClick")
                    {
                        continue;
                    }

                    GenLabel(al);
                }
                else
                {
                    var al = (AegisUnknown) aii;
                    GenUnk(al);
                }
            }

            WriteNewLine("{0}\n", "}");
        }

        private void GenLabel(AegisLabel al)
        {
            string tmp = al.Name.ToLower();

            if (tmp == "onclick")
            {
            }
            else if (al.Exp != null)
            {
                string lblExp = al.Exp.ToString();
                lblExp = lblExp.Substring(1, lblExp.Length - 1).ToLower();

                if (lblExp == "reset")
                    WriteLine("OnReset:");
                else if (lblExp == "on")
                    WriteLine("OnCommandOn:");
                else if (lblExp == "off")
                    WriteLine("OnCommandOff:");
                else
                    WriteLine("{0}{1}:", al.Name, al.Exp.ToString().Trim('"'));
            }
            else
                WriteLine("{0}:", al.Name);

            tabLevel++;

            GenBlock(al.Items);

            tabLevel--;
        }

        private void GenBlock(List<AegisItem> list)
        {
            int finalType = 0;

            foreach (AegisItem ai in list)
            {
                if (ai == null)
                    continue;

                Type tp = ai.GetType();

                if (tp == typeof (AegisFunc))
                {
                    var af = (AegisFunc) ai;

                    af.Name = af.Name.ToLower();

                    if (af.Name == "close")
                    {
                        finalType = 1;
                    }
                    else if (af.Name == "end")
                    {
                        finalType = 2;
                    }
                    else if (af.Name == "break")
                    {
                        finalType = 3;
                    }
                    else
                    {
                        finalType = 0;
                        GenFunc(af);
                    }
                }
                else if (tp == typeof (AegisIf))
                {
                    GenIf((AegisIf) ai);
                }
                else if (tp == typeof (AegisChoose))
                {
                    GenChoose((AegisChoose) ai);
                }
                else if (tp == typeof (AegisVarDecl))
                {
                    GenVarDev((AegisVarDecl) ai);
                }
                else if (tp == typeof (AegisUnknown))
                {
                    GenUnk((AegisUnknown) ai);
                }
                else if (tp == typeof (AegisWhile))
                {
                    GenWhile((AegisWhile) ai);
                }
                else
                {
                    throw new Exception();
                }
            }

            switch (finalType)
            {
                case 0:
                    break;
                case 1:
                    WriteNewLine("close;");
                    break;
                case 2:
                    WriteNewLine("close;");
                    break;
                case 3:
                    lastFunc.Name = "break";
                    WriteNewLine("break;");
                    break;
            }
        }

        private void GenWhile(AegisWhile aegisWhile)
        {
            if (aegisWhile.Exp.GetType() == typeof (ComposExpr))
            {
                WriteNew("while ");

                Write(TranslateExpr(aegisWhile.Exp));

                WriteLine("");
            }
            else
            {
                WriteNew("while (");

                Write(TranslateExpr(aegisWhile.Exp));

                WriteLine(")");
            }
            WriteNewLine("{0}", "{");
            tabLevel++;

            GenBlock(aegisWhile.Items);

            tabLevel--;
            WriteNewLine("{0}", "}");
        }

        private void GenUnk(AegisUnknown aegisUnknown)
        {
            WriteNewLine("// FIX: {0}", aegisUnknown.str);
        }

        private void GenVarDev(AegisVarDecl aegisVarDecl)
        {
            if (!Variable.haxforever.Contains(aegisVarDecl.Name.ToLower()))
                Variable.haxforever.Add(aegisVarDecl.Name.ToLower());
            WriteNewLine("set .@{0}, {1};", aegisVarDecl.Name, TranslateExpr(aegisVarDecl.Exp));
        }

        private void GenChoose(AegisChoose aegisChoose)
        {
            if (aegisChoose.Exp.GetType() == typeof (ComposExpr))
            {
                WriteNew("switch ");

                Write(TranslateExpr(aegisChoose.Exp));

                WriteLine("");
            }
            else
            {
                WriteNew("switch (");

                Write(TranslateExpr(aegisChoose.Exp));

                WriteLine(")");
            }
            WriteNewLine("{0}", "{");
            tabLevel++;

            foreach (AegisCase ac in aegisChoose.Items)
            {
                GenCase(ac);
            }

            tabLevel--;
            WriteNewLine("{0}", "}");
        }

        private void GenCase(AegisCase ac)
        {
            WriteNew("case ");

            Write(TranslateExpr(ac.Exp));

            WriteLine(":");

            tabLevel++;

            GenBlock(ac.Items);

            if (lastFunc.Name != "break")
                WriteNewLine("break;");

            tabLevel--;
        }

        private void GenIf(AegisIf ai)
        {
            if (ai.Exp.GetType() == typeof (ComposExpr))
            {
                WriteNew("if ");

                Write(TranslateExpr(ai.Exp));

                WriteLine("");
            }
            else
            {
                WriteNew("if (");

                Write(TranslateExpr(ai.Exp));

                WriteLine(")");
            }
            WriteNewLine("{0}", "{");
            tabLevel++;

            GenBlock(ai.Items);

            tabLevel--;
            WriteNewLine("{0}", "}");

            if (ai.ElseIfs != null)
            {
                foreach (AegisIf aii in ai.ElseIfs)
                {
                    GenElseIf(aii);
                }
            }

            if (ai.Else != null && ai.Else.Items.Count > 0)
            {
                WriteNewLine("else");
                if (ai.Else.Items.Count == 1)
                {
                    tabLevel++;

                    GenBlock(ai.Else.Items);

                    tabLevel--;
                }
                else
                {
                    WriteNewLine("{0}", "{");
                    tabLevel++;

                    GenBlock(ai.Else.Items);

                    tabLevel--;
                    WriteNewLine("{0}", "}");
                }
            }
        }

        private string TranslateExpr(Expr expr)
        {
            if (expr.GetType() == typeof (FuncCall))
            {
                var af = new AegisFunc();
                af.Name = ((FuncCall) expr).Name;
                af.Items = new List<AegisItem>();
                foreach (Expr exp in ((FuncCall) expr).Args)
                    af.Items.Add(exp);

                return TranslateFunc(af.Name, af.Items);
            }

            return expr.ToString();
        }

        private void GenElseIf(AegisIf ai)
        {
            if (ai.Exp.GetType() == typeof (ComposExpr))
            {
                WriteNew("else if ");

                Write(TranslateExpr(ai.Exp));

                WriteLine("");
            }
            else
            {
                WriteNew("else if (");

                Write(TranslateExpr(ai.Exp));

                WriteLine(")");
            }

            if (ai.Items.Count == 1)
            {
                tabLevel++;

                GenBlock(ai.Items);

                tabLevel--;
            }
            else
            {
                WriteNewLine("{0}", "{");
                tabLevel++;

                GenBlock(ai.Items);

                tabLevel--;
                WriteNewLine("{0}", "}");
            }
        }

        private void GenFunc(AegisFunc aegisFunc)
        {
            lastFunc = aegisFunc;
            lastFunc.Name = lastFunc.Name.ToLower();

            string str = TranslateFunc(aegisFunc.Name, aegisFunc.Items);

            if (str == "")
                return;

            WriteNewLine(str + ";");
        }

        private string TranslateFunc(string p, List<AegisItem> list)
        {
            curArgs = list;

            if (funcs.ContainsKey(p.ToLower()))
                return funcs[p.ToLower()]();

            return func_aegisout(p);
        }

        public string FindArg(int index)
        {
            string str = "";

            try
            {
                str = curArgs[index].ToString();
            }
            catch
            {
            }

            return str;
        }

        internal string Gen()
        {
            AegisBody ab = p.Parse();

            foreach (AegisItem ai in ab.Items)
            {
                Type tp = ai.GetType();

                if (tp == typeof (AegisNpc))
                {
                    GenNpc((AegisNpc) ai);
                }
                else if (tp == typeof (AegisUnknown))
                {
                    GenUnk((AegisUnknown) ai);
                }
                else if (tp == typeof (AegisWarp))
                {
                    GenWarp((AegisWarp) ai);
                }
                else if (tp == typeof (AegisHiddenWarp))
                {
                    GenHWarp((AegisHiddenWarp) ai);
                }
                else if (tp == typeof (AegisTrader))
                {
                    GenTrader((AegisTrader) ai);
                }
                else if (tp == typeof (AegisPutMob))
                {
                    GenMonster((AegisPutMob) ai);
                }
                else if (tp == typeof (AegisPutBoss))
                {
                    GenBoss((AegisPutBoss) ai);
                }
                else
                    throw new Exception();
            }

            return sw;
        }

        #region Funcs Area

        public static string endLineComment = "";
        private int waitroomsize;

        private void InitializeFuncs()
        {
            funcs.Add("dialog", func_dialog);
            funcs.Add("wait", func_wait);
            funcs.Add("close", func_close);
            funcs.Add("jobchange", func_changejob);
            funcs.Add("getitem", func_getitem);
            funcs.Add("dropitem", func_dropitem);
            funcs.Add("setitem", func_setitem);
            funcs.Add("moveto", func_moveto);
            funcs.Add("checkpoint", func_checkpoint);
            funcs.Add("showimage", func_showimage);
            funcs.Add("hpheal", func_hpheal);
            funcs.Add("spheal", func_spheal);
            funcs.Add("return", func_return);
            funcs.Add("menu", func_menu);
            funcs.Add("break", func_break);
            funcs.Add("broadcastinmap", func_broadcastinmap);
            funcs.Add("rand", func_rand);
            funcs.Add("cmdothernpc", func_cmdothernpc);
            funcs.Add("callmonster", func_callmonster);
            funcs.Add("setglobalvar", func_setglobalvar);
            funcs.Add("showeffect", func_showeffect);
            funcs.Add("checkmaxcount", func_checkmaxcount);
            funcs.Add("dropgold", func_dropgold);
            funcs.Add("disablenpc", func_disablenpc);
            funcs.Add("dlgwrite", func_dlgwrite);
            funcs.Add("dlgwritestr", func_dlgwritestr);
            funcs.Add("inittimer", func_inittimer);
            funcs.Add("stoptimer", func_stoptimer);
            funcs.Add("changespr", func_changespr);
            funcs.Add("especialeffect", func_espeffect);
            funcs.Add("compass", func_compass);
            funcs.Add("makewaitingroom", func_makewaitroom);
            funcs.Add("setarenaeventsize", func_arenasize);
            funcs.Add("enablearena", func_enablearena);
            funcs.Add("disablearena", func_disablearena);
            funcs.Add("warpwaitingpctoarena", func_warpwaitingpctoarena);
            funcs.Add("eventaddskill", func_eventaddskill);
            funcs.Add("exitwhile", func_exitwhile);
            funcs.Add("enableitemmove", func_enableitemmove);
            funcs.Add("blockitemmove", func_blockitemmove);
            funcs.Add("consumespecialitem", func_consumespecialitem); //existe saporra? consumir um item via script?
            funcs.Add("warpallpcinthemap", func_warpallpcinthemap);
            funcs.Add("enablenpc", func_enablenpc);
            funcs.Add("resetmymob", func_resetmymob);
            funcs.Add("broadcastinmap2", func_broadcastinmap2);
            funcs.Add("isbegin_quest", func_isbegin_quest);
            funcs.Add("setquest", func_setquest);
            funcs.Add("changequest", func_changequest);
        }

        private string func_warpwaitingpctoarena()
        {
            return "warpwaitingpc " + FindArg(0) + ", " + FindArg(1) + ", " + FindArg(2);
        }

        private string func_enablearena()
        {
            return "enablewaitingroomevent";
        }

        private string func_disablearena()
        {
            return "disablewaitingroomevent";
        }

        private string func_arenasize()
        {
            waitroomsize = int.Parse(FindArg(0));
            return "";
        }

        private string func_makewaitroom()
        {
            return "waitingroom " + FindArg(0) + ", " + FindArg(1) + ", \"" + ((AegisNpc) curNpc).Name +
                   "::OnStartArena\"," + waitroomsize;
        }

        private string func_compass()
        {
            return "viewpoint " + ((FindArg(3) == "0") ? "2" : FindArg(3)) + ", " + FindArg(1) + ", " + FindArg(2) +
                   ", " + FindArg(0) + ", " + FindArg(4).Trim('\"').Remove(2, 2);
                //FindArg(4).Substring(1, FindArg(4).Length - 2);
        }

        private string func_espeffect()
        {
            return "misceffect " + FindArg(0);
        }

        private string func_changespr()
        {
            return "setnpcdisplay " + FindArg(0) + ", " + FindArg(1);
        }

        private string func_stoptimer()
        {
            return "stopnpctimer";
        }

        private string func_inittimer()
        {
            return "initnpctimer";
        }

        private string func_dlgwrite()
        {
            return "input .@input, " + FindArg(0) + ", " + FindArg(1);
        }

        private string func_dlgwritestr()
        {
            return "input .@inputstr$";
        }

        private string func_disablenpc()
        {
            return "disablenpc " + FindArg(0);
        }

        private string func_checkmaxcount()
        {
            if (endLineComment != "")
                endLineComment += ", " + GetItemName(GetItemId(FindArg(0)));
            else
                endLineComment = GetItemName(GetItemId(FindArg(0)));

            return "checkweight (" + GetItemId(FindArg(0)) + ", " + FindArg(1) + ")";
        }

        private string func_dropgold()
        {
            return "set Zeny, Zeny - " + FindArg(0);
        }

        private string func_showeffect()
        {
            if (FindArg(0) == "")
                return "specialeffect2 " + FindArg(1);
            else
                return "specialeffect " + FindArg(1) + ", " + "AREA" + ", " + FindArg(0);
        }

        private string func_setglobalvar()
        {
            return "set $" + FindArg(0).Substring(1, FindArg(0).Length - 2) + ", " + FindArg(1);
        }

        private string func_callmonster()
        {
            return string.Format("monster {0}, {1}, {2}, {3}, {4}, {5}", FindArg(0), FindArg(3), FindArg(4), FindArg(2),
                                 GetMobId(FindArg(1)), "1");
        }

        private string func_template()
        {
            return "";
        }

        private string func_aegisout(string name)
        {
            string p = "// FIX_MEH!: Aegis func -> " + name + " ";

            foreach (AegisItem expr in curArgs)
                p += expr + " ";

            return p;
        }

        private string func_cmdothernpc()
        {
            return "cmdothernpc " + FindArg(0) + ", " + FindArg(1);
        }

        private string func_rand()
        {
            if (int.Parse(FindArg(0)) == 0)
                return "rand(" + FindArg(1) + ")";
            else
                return "rand(" + FindArg(0) + ", " + FindArg(1) + ")";
        }

        private string func_break()
        {
            if (lastFunc.Name == "close2")
                return "";

            if (lastFunc.Name == "close")
                return "";

            return "break";
        }

        private string func_broadcastinmap()
        {
            return "mapannounce \"" + GetCurMap() + "\", " + FindArg(0) + ", bc_yellow";
        }

        private string GetCurMap()
        {
            if (curNpc.GetType() == typeof (AegisNpc))
                return ((AegisNpc) curNpc).Map;
            if (curNpc.GetType() == typeof (AegisHiddenWarp))
                return ((AegisHiddenWarp) curNpc).MapName;
            if (curNpc.GetType() == typeof (AegisWarp))
                return ((AegisWarp) curNpc).MapName;
            if (curNpc.GetType() == typeof (AegisTrader))
                return ((AegisTrader) curNpc).Map;

            return "";
        }

        private string func_menu()
        {
            string str = "select (\"";

            for (int i = 0; i < curArgs.Count; i++)
                if (i == curArgs.Count - 1)
                    str += FindArg(i).Trim('\"');
                else
                    str += FindArg(i).Trim('\"') + ":";


            return str + "\")";
        }

        private string func_return()
        {
            if (lastFunc.Name == "break")
                return "end";
            else
                return "close";
        }

        private string func_hpheal()
        {
            return "percentheal " + FindArg(0) + ", 0";
        }

        private string func_spheal()
        {
            return "percentheal 0, " + FindArg(0);
        }

        private string func_moveto()
        {
            return "warp " + FindArg(0) + ", " + FindArg(1) + ", " + FindArg(2);
        }

        private string func_checkpoint()
        {
            return "savepoint " + FindArg(0) + ", " + FindArg(1) + ", " + FindArg(2);
        }

        private string func_showimage()
        {
            try
            {
                return "cutin \"" + Path.GetFileNameWithoutExtension(OutString(FindArg(0))) + "\", " + FindArg(1);
            }
            catch
            {
                string p = "// FIX: Dynamic Cutin -> ";

                foreach (AegisItem expr in curArgs)
                    p += expr + " ";

                return p;
            }
        }

        private string OutString(string p)
        {
            string str = p.Substring(1, p.Length - 2);

            return str;
        }

        private string func_setitem()
        {
            return "set " + FindArg(0) + ", " + FindArg(1);
        }

        private string func_getitem()
        {
            if (endLineComment != "")
                endLineComment += ", " + GetItemName(GetItemId(FindArg(0)));
            else
                endLineComment = GetItemName(GetItemId(FindArg(0)));

            return "getitem " + GetItemId(FindArg(0)) + ", " + FindArg(1);
        }

        private string func_dropitem()
        {
            if (endLineComment != "")
                endLineComment += ", " + GetItemName(GetItemId(FindArg(0)));
            else
                endLineComment = GetItemName(GetItemId(FindArg(0)));

            return "delitem " + GetItemId(FindArg(0)) + ", " + FindArg(1);
        }

        private string func_close()
        {
            return "close";
        }

        private string func_wait()
        {
            return "next";
        }

        private string func_dialog()
        {
            return "mes " + FindArg(0) + "";
        }

        private string func_changejob()
        {
            return "jobchange " + FindArg(0);
        }

        private string func_eventaddskill()
        {
            return "skill \"" + FindArg(0) + "\", " + FindArg(1) + ", 0";
        }

        private string func_exitwhile()
        {
// maybee
            return "close";
        }

        private string func_enableitemmove()
        {
            return "enableitemuse";
        }

        private string func_blockitemmove()
        {
            return "disableitemuse";
        }

        private string func_consumespecialitem()
        {
            return "//usar o efeito do item";
        }

        private string func_warpallpcinthemap()
        {
            return "mapwarp strnpcinfo(4), " + FindArg(0) + ", " + FindArg(1) + ", " + FindArg(2);
        }

        private string func_enablenpc()
        {
            return "enablenpc " + FindArg(0);
        }

        private string func_resetmymob()
        {
            return "killmonster strnpcinfo(4), strnpcinfo(0)+\"::OnMyMobDead\"" + " // FIX?";
        }

        private string func_broadcastinmap2()
        {
            return "mapannounce strnpcinfo(4), " + FindArg(5) + ", bc_map, \"0x" + FindArg(0) + "\"";
        }

        private string func_isbegin_quest()
        {
            return "(checkquest(" + FindArg(0) + ") != -1)" + " // FIX?";
        }

        private string func_setquest()
        {
            return "setquest " + FindArg(0);
        }

        private string func_changequest()
        {
            return "changequest " + FindArg(0);
        }

        #endregion
    }
}