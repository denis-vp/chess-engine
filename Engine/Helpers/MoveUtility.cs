namespace chess_engine.Engine
{
    public static class MoveUtility
    {
        /// Converts a moveName into internal move representation
        /// Name is expected in format: "e2e4" = UCI format
        public static Move GetMoveFromUCIName(string moveName, Board board)
        {
            int startSquare = BoardHelper.SquareIndexFromName(moveName.Substring(0, 2));
            int targetSquare = BoardHelper.SquareIndexFromName(moveName.Substring(2, 2));

            int movedPieceType = Piece.PieceType(board.Squares[startSquare]);
            Coord startCoord = new(startSquare);
            Coord targetCoord = new(targetSquare);

            // Get corresponding move flag
            int flag = Move.Flag.None;

            if (movedPieceType == Piece.Pawn)
            {
                // Promotion
                if (moveName.Length > 4)
                {
                    flag = moveName[^1] switch
                    {
                        'q' => Move.Flag.PromoteToQueen,
                        'r' => Move.Flag.PromoteToRook,
                        'n' => Move.Flag.PromoteToKnight,
                        'b' => Move.Flag.PromoteToBishop,
                        _ => Move.Flag.None
                    };
                }
                // Double pawn push
                else if (System.Math.Abs(targetCoord.rankIndex - startCoord.rankIndex) == 2)
                {
                    flag = Move.Flag.PawnTwoForward;
                }
                // En-passant
                else if (startCoord.fileIndex != targetCoord.fileIndex && board.Squares[targetSquare] == Piece.None)
                {
                    flag = Move.Flag.EnPassantCapture;
                }
            }
            else if (movedPieceType == Piece.King)
            {
                if (Math.Abs(startCoord.fileIndex - targetCoord.fileIndex) > 1)
                {
                    flag = Move.Flag.Castling;
                }
            }

            return new Move(startSquare, targetSquare, flag);
        }
    }
}