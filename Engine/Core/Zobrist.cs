namespace chess_engine.Engine
{
    public static class Zobrist
    {
        /// Piece type, colour, square index
        public static readonly ulong[,,] piecesArray = new ulong[8, 2, 64];
        public static readonly ulong[] castlingRights = new ulong[16];
        /// Ep file (0 = no ep).
        public static readonly ulong[] enPassantFile = new ulong[9]; // No need for rank info as side to move is included in key
        public static readonly ulong sideToMove;

        static Zobrist()
        {

            const int seed = 11122003;
            Random rng = new Random(seed);

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                for (int pieceIndex = 0; pieceIndex < 8; pieceIndex++)
                {
                    piecesArray[pieceIndex, Board.WhiteIndex, squareIndex] = RandomUnsigned64BitNumber(rng);
                    piecesArray[pieceIndex, Board.BlackIndex, squareIndex] = RandomUnsigned64BitNumber(rng);

                }
            }

            for (int i = 0; i < 16; i++)
            {
                castlingRights[i] = RandomUnsigned64BitNumber(rng);
            }

            for (int i = 0; i < enPassantFile.Length; i++)
            {
                enPassantFile[i] = RandomUnsigned64BitNumber(rng);
            }

            sideToMove = RandomUnsigned64BitNumber(rng);
        }

        /// Calculate zobrist key from current board position. This should only be used after setting board from fen; during search the key should be updated incrementally.
        public static ulong CalculateZobristKey(Board board)
        {
            ulong zobristKey = 0;

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                if (board.Squares[squareIndex] != 0)
                {
                    int pieceType = Piece.PieceType(board.Squares[squareIndex]);
                    int pieceColour = Piece.Color(board.Squares[squareIndex]);

                    zobristKey ^= piecesArray[pieceType, (pieceColour == Piece.White) ? Board.WhiteIndex : Board.BlackIndex, squareIndex];
                }
            }

            int epIndex = (int)(board.CurrentGameState >> 4) & 15;
            if (epIndex != -1)
            {
                zobristKey ^= enPassantFile[epIndex];
            }

            if (board.ColorToMove == Piece.Black)
            {
                zobristKey ^= sideToMove;
            }

            zobristKey ^= castlingRights[board.CurrentGameState & 0b1111];

            return zobristKey;
        }

        static ulong RandomUnsigned64BitNumber(Random rng)
        {
            byte[] buffer = new byte[8];
            rng.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}