namespace chess_engine.Engine
{
    /* 
    Moves are stored as 16 bit numbers.
    ffff tttttt ssssss

    bit 0-5: start square (0 to 63)
    bit 6-11: to square (0 to 63)
    bit 12-15: flag
    */
    public readonly struct Move
    {
        public readonly struct Flag
        {
            public const int None = 0;
            public const int EnPassantCapture = 1;
            public const int Castling = 2;
            public const int PromoteToQueen = 3;
            public const int PromoteToKnight = 4;
            public const int PromoteToRook = 5;
            public const int PromoteToBishop = 6;
            public const int PawnTwoForward = 7;
        }

        readonly ushort moveValue;

        const ushort startSquareMask = 0b0000000000111111;
        const ushort targetSquareMask = 0b0000111111000000;
        const ushort flagMask = 0b1111000000000000;

        public Move Clone()
        {
            Move newMove = new Move(this.moveValue);
            return newMove;
        }
        
        public Move(ushort moveValue)
        {
            this.moveValue = moveValue;
        }

        public Move(int startSquare, int targetSquare)
        {
            moveValue = (ushort)(startSquare | targetSquare << 6);
        }

        public Move(int startSquare, int targetSquare, int flag)
        {
            moveValue = (ushort)(startSquare | targetSquare << 6 | flag << 12);
        }

        public int StartSquare
        {
            get
            {
                return moveValue & startSquareMask;
            }
        }

        public int TargetSquare
        {
            get
            {
                return (moveValue & targetSquareMask) >> 6;
            }
        }

        public bool IsPromotion
        {
            get
            {
                int flag = MoveFlag;
                return flag == Flag.PromoteToQueen || flag == Flag.PromoteToRook || flag == Flag.PromoteToKnight || flag == Flag.PromoteToBishop;
            }
        }

        public int MoveFlag
        {
            get
            {
                return moveValue >> 12;
            }
        }

        public static Move NullMove
        {
            get
            {
                return new Move(0);
            }
        }

        public static bool SameMove(Move a, Move b)
        {
            return a.moveValue == b.moveValue;
        }

        public static bool IsNullMove(Move move)
        {
            return move.moveValue == 0;
        }

        public ushort Value
        {
            get
            {
                return moveValue;
            }
        }

        public bool IsInvalid
        {
            get
            {
                return moveValue == 0;
            }
        }
    }
}
