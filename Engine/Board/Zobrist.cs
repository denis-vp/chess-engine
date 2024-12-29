namespace chess_engine.Engine
{
    // Zobrist hash for storage in transposition table.
    // 64 bit unsigned integer that represents the current state of the game.
    // It does not guarantee uniqueness.

    public static class Zobrist
    {
        // Piece type, colour, square index
        public static readonly ulong[,] piecesArray = new ulong[Piece.MaxPieceIndex + 1, 64];
        // Each player has 4 possible castling right states: none, queenside, kingside, both.
        // 4 by 4 = 16 possible states.
        public static readonly ulong[] castlingRights = new ulong[16];
        // En passant file (0 = no ep).
        public static readonly ulong[] enPassantFile = new ulong[9];
        public static readonly ulong sideToMove;

        static Zobrist()
        {

            const int seed = 11122003;
            Random rng = new Random(seed);

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                foreach (int piece in Piece.PieceIndices)
                {
                    piecesArray[piece, squareIndex] = RandomUnsigned64BitNumber(rng);
                }
            }


            for (int i = 0; i < castlingRights.Length; i++)
            {
                castlingRights[i] = RandomUnsigned64BitNumber(rng);
            }

            for (int i = 0; i < enPassantFile.Length; i++)
            {
                enPassantFile[i] = i == 0 ? 0 : RandomUnsigned64BitNumber(rng);
            }

            sideToMove = RandomUnsigned64BitNumber(rng);
        }

        public static ulong CalculateZobristKey(Board board)
        {
            ulong zobristKey = 0;

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                int piece = board.Squares[squareIndex];

                if (Piece.PieceType(piece) != Piece.None)
                {
                    zobristKey ^= piecesArray[piece, squareIndex];
                }
            }

            zobristKey ^= enPassantFile[board.CurrentGameState.enPassantFile];

            if (board.MoveColor == Piece.Black)
            {
                zobristKey ^= sideToMove;
            }

            zobristKey ^= castlingRights[board.CurrentGameState.castlingRights];

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