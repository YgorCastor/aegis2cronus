namespace Aegis2Cronus.Aegis
{
    internal class Token
    {
        public int Col, Line;
        public TokenType Type;
        public string Value;

        public override string ToString()
        {
            return string.Format("{0},{1}: {2} -> {3}", Line, Col, Type, Value);
        }
    }

    internal enum TokenType
    {
        None,

        DecLiteral,
        HexLiteral,
        StringLiteral,
        Identifier,

        Label,

        RCol,
        RPar,
        RBrack,
        LCol,
        LPar,
        LBrack,

        Plus,
        Minus,
        Times,
        Slash,
        Remainder,

        Comment,

        If,
        While,
        ElseIf,
        Else,
        Choose,
        Break,
        Case,
        Default,
        Continue,
        EndChoose,
        EndIf,
        EndWhile,
        ExitWhile,

        Comma,

        GT,
        LT,
        GET,
        LET,

        EQ,
        DIF,

        SET,

        AND,
        OR,
        ANDALSO,
        ORELSE,

        npc,
        trader,
        warp,
        putmob,
        putboss,
        Return,
        NewLine,

        EOF,
        Var,

        VarDecl,
        hiddenwarp,
        arenaguide,
    }
}