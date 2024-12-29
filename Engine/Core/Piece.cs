namespace chess_engine.Engine
{
    /*
    Pieces are stored as 5 bit numbers.
    cc ttt

    bit 0-2: type (0 to 7)
    bit 3-4: color (0 = white, 2 = black)
    */
    public static class Piece
    {

        public const int None = 0;
        public const int King = 1;
        public const int Pawn = 2;
        public const int Knight = 3;
        public const int Bishop = 5;
        public const int Rook = 6;
        public const int Queen = 7;

        public const int White = 8;
        public const int Black = 16;

        const int typeMask = 0b00111;
        const int blackMask = 0b10000;
        const int whiteMask = 0b01000;
        const int colorMask = whiteMask | blackMask;

        public static bool IsColor(int piece, int color)
        {
            return (piece & colorMask) == color;
        }

        public static bool IsWhite(int piece)
        {
            return (piece & whiteMask) != 0;
        }

        public static int Color(int piece)
        {
            return piece & colorMask;
        }

        public static int PieceType(int piece)
        {
            return piece & typeMask;
        }

        public static bool IsRookOrQueen(int piece)
        {
            return (piece & 0b110) == 0b110;
        }

        public static bool IsBishopOrQueen(int piece)
        {
            return (piece & 0b101) == 0b101;
        }

        public static bool IsSlidingPiece(int piece)
        {
            return (piece & 0b100) != 0;
        }
    }
}