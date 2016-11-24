using System.Collections.Generic;

namespace Aegis2Cronus.Aegis
{
    internal class AegisIf : AegisItem
    {
        public AegisItem Else;
        public List<AegisIf> ElseIfs;
        public Expr Exp;
    }

    internal class AegisWhile : AegisItem
    {
        public Expr Exp;
    }

    internal class AegisChoose : AegisItem
    {
        public Expr Exp;
    }

    internal class AegisCase : AegisItem
    {
        public Expr Exp;
    }
}