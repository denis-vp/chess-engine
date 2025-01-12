namespace chess_engine.Engine
{
    public class MoveOrdering
    {

        int[] moveScores;
        const int maxMoveCount = 218;

        const int squareControlledByOpponentPawnPenalty = 350;
        const int capturedPieceValueMultiplier = 10;

        AbstractMoveGenerator moveGenerator;

        public MoveOrdering(AbstractMoveGenerator moveGenerator)
        {
            moveScores = new int[maxMoveCount];
            this.moveGenerator = moveGenerator;
        }

        public void OrderMoves(Move hashMove, Board board, List<Move> moves)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                int score = 0;
                int movePieceType = Piece.PieceType(board.Squares[moves[i].StartSquare]);
                int capturePieceType = Piece.PieceType(board.Squares[moves[i].TargetSquare]);
                int flag = moves[i].MoveFlag;

                if (capturePieceType != Piece.None)
                {
                    // Order moves to try capturing the most valuable opponent piece with least valuable of own pieces first
                    // capturedPieceValueMultiplier is used to make even 'bad' captures like QxP rank above non-captures
                    score = capturedPieceValueMultiplier * GetPieceValue(capturePieceType) - GetPieceValue(movePieceType);
                }

                if (movePieceType == Piece.Pawn)
                {

                    if (flag == Move.Flag.PromoteToQueen)
                    {
                        score += AbstractEvaluation.queenValue;
                    }
                    else if (flag == Move.Flag.PromoteToKnight)
                    {
                        score += AbstractEvaluation.knightValue;
                    }
                    else if (flag == Move.Flag.PromoteToRook)
                    {
                        score += AbstractEvaluation.rookValue;
                    }
                    else if (flag == Move.Flag.PromoteToBishop)
                    {
                        score += AbstractEvaluation.bishopValue;
                    }
                }
                else
                {
                    // Penalize moving piece to a square attacked by opponent pawn
                    if (BitBoardUtility.ContainsSquare(moveGenerator.opponentPawnAttackMap, moves[i].TargetSquare))
                    {
                        score -= squareControlledByOpponentPawnPenalty;
                    }
                }
                if (Move.SameMove(moves[i], hashMove))
                {
                    score += 10000;
                }

                moveScores[i] = score;
            }

            Sort(moves);
        }

        static int GetPieceValue(int pieceType)
        {
            switch (pieceType)
            {
                case Piece.Queen:
                    return AbstractEvaluation.queenValue;
                case Piece.Rook:
                    return AbstractEvaluation.rookValue;
                case Piece.Knight:
                    return AbstractEvaluation.knightValue;
                case Piece.Bishop:
                    return AbstractEvaluation.bishopValue;
                case Piece.Pawn:
                    return AbstractEvaluation.pawnValue;
                default:
                    return 0;
            }
        }

        void Sort(List<Move> moves)
        {
            // Sort the moves list based on scores
            for (int i = 0; i < moves.Count - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    if (moveScores[swapIndex] < moveScores[j])
                    {
                        (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                        (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                    }
                }
            }
        }
    }
}