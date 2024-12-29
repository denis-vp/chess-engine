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
            int flag = Move.NoFlag;

            if (movedPieceType == Piece.Pawn)
            {
                // Promotion
                if (moveName.Length > 4)
                {
                    flag = moveName[^1] switch
                    {
                        'q' => Move.PromoteToQueenFlag,
                        'r' => Move.PromoteToRookFlag,
                        'n' => Move.PromoteToKnightFlag,
                        'b' => Move.PromoteToBishopFlag,
                        _ => Move.NoFlag
                    };
                }
                // Double pawn push
                else if (System.Math.Abs(targetCoord.rankIndex - startCoord.rankIndex) == 2)
                {
                    flag = Move.PawnTwoUpFlag;
                }
                // En-passant
                else if (startCoord.fileIndex != targetCoord.fileIndex && board.Squares[targetSquare] == Piece.None)
                {
                    flag = Move.EnPassantCaptureFlag;
                }
            }
            else if (movedPieceType == Piece.King)
            {
                if (Math.Abs(startCoord.fileIndex - targetCoord.fileIndex) > 1)
                {
                    flag = Move.CastleFlag;
                }
            }

            return new Move(startSquare, targetSquare, flag);
        }

        /// Get algebraic name of move (with promotion specified)
        /// Examples: "e2e4", "e7e8q"
        public static string GetMoveNameUCI(Move move)
        {
            string startSquareName = BoardHelper.SquareNameFromIndex(move.StartSquare);
            string endSquareName = BoardHelper.SquareNameFromIndex(move.TargetSquare);
            string moveName = startSquareName + endSquareName;
            if (move.IsPromotion)
            {
                switch (move.MoveFlag)
                {
                    case Move.PromoteToRookFlag:
                        moveName += "r";
                        break;
                    case Move.PromoteToKnightFlag:
                        moveName += "n";
                        break;
                    case Move.PromoteToBishopFlag:
                        moveName += "b";
                        break;
                    case Move.PromoteToQueenFlag:
                        moveName += "q";
                        break;
                }
            }
            return moveName;
        }
    }
}