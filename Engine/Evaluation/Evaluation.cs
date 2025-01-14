namespace chess_engine.Engine
{
    public class Evaluation : AbstractEvaluation
    {
        const float endgameMaterialStart = rookValue * 2 + bishopValue + knightValue;
        Board board;

        // Performs static evaluation of the current position.
        // The position is assumed to be 'quiet', i.e no captures are available that could drastically affect the evaluation.
        // The score that's returned is given from the perspective of whoever's turn it is to move.
        // So a positive score means the player who's turn it is to move has an advantage, while a negative score indicates a disadvantage.
        override public int Evaluate(Board board)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            this.board = board;
            int whiteEval = 0;
            int blackEval = 0;

            int whiteMaterial = CountMaterial(Board.WhiteIndex);
            int blackMaterial = CountMaterial(Board.BlackIndex);

            int whiteMaterialWithoutPawns = whiteMaterial - board.Pawns[Board.WhiteIndex].Count * pawnValue;
            int blackMaterialWithoutPawns = blackMaterial - board.Pawns[Board.BlackIndex].Count * pawnValue;
            float whiteEndgamePhaseWeight = EndgamePhaseWeight(whiteMaterialWithoutPawns);
            float blackEndgamePhaseWeight = EndgamePhaseWeight(blackMaterialWithoutPawns);

            whiteEval += whiteMaterial;
            blackEval += blackMaterial;
            whiteEval += MopUpEval(Board.WhiteIndex, Board.BlackIndex, whiteMaterial, blackMaterial, blackEndgamePhaseWeight);
            blackEval += MopUpEval(Board.BlackIndex, Board.WhiteIndex, blackMaterial, whiteMaterial, whiteEndgamePhaseWeight);

            whiteEval += EvaluatePieceSquareTables(Board.WhiteIndex, blackEndgamePhaseWeight);
            blackEval += EvaluatePieceSquareTables(Board.BlackIndex, whiteEndgamePhaseWeight);

            int eval = whiteEval - blackEval;

            int perspective = (board.WhiteToMove) ? 1 : -1;

            watch.Stop();
            if (Settings.PrintEvaluationTime)
            {
                Console.WriteLine($"Evaluation time: {watch.ElapsedTicks} ticks");
            }

            return eval * perspective;
        }

        float EndgamePhaseWeight(int materialCountWithoutPawns)
        {
            const float multiplier = 1 / endgameMaterialStart;
            return 1 - System.Math.Min(1, materialCountWithoutPawns * multiplier);
        }

        int MopUpEval(int friendlyIndex, int opponentIndex, int myMaterial, int opponentMaterial, float endgameWeight)
        {
            int mopUpScore = 0;
            if (myMaterial > opponentMaterial + pawnValue * 2 && endgameWeight > 0)
            {

                int friendlyKingSquare = board.KingSquares[friendlyIndex];
                int opponentKingSquare = board.KingSquares[opponentIndex];
                mopUpScore += PrecomputedMoveData.centreManhattanDistance[opponentKingSquare] * 10;
                // Use orthogonal distance to promote direct opposition
                mopUpScore += (14 - PrecomputedMoveData.NumRookMovesToReachSquare(friendlyKingSquare, opponentKingSquare)) * 4;

                return (int)(mopUpScore * endgameWeight);
            }
            return 0;
        }

        int CountMaterial(int colorIndex)
        {
            int material = 0;
            material += board.Pawns[colorIndex].Count * pawnValue;
            material += board.Knights[colorIndex].Count * knightValue;
            material += board.Bishops[colorIndex].Count * bishopValue;
            material += board.Rooks[colorIndex].Count * rookValue;
            material += board.Queens[colorIndex].Count * queenValue;

            return material;
        }

        int EvaluatePieceSquareTables(int colorIndex, float endgamePhaseWeight)
        {
            int value = 0;
            bool isWhite = colorIndex == Board.WhiteIndex;
            value += EvaluatePieceSquareTable(PieceSquareTable.Rooks, board.Rooks[colorIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.Knights, board.Knights[colorIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.Bishops, board.Bishops[colorIndex], isWhite);
            value += EvaluatePieceSquareTable(PieceSquareTable.Queens, board.Queens[colorIndex], isWhite);

            int pawnEarly = EvaluatePieceSquareTable(PieceSquareTable.PawnsStart, board.Pawns[colorIndex], isWhite);
            int pawnLate = EvaluatePieceSquareTable(PieceSquareTable.PawnsEnd, board.Pawns[colorIndex], isWhite);
            value += (int)(pawnEarly * (1 - endgamePhaseWeight));
            value += (int)(pawnLate * endgamePhaseWeight);

            int kingEarlyPhase = PieceSquareTable.Read(PieceSquareTable.KingStart, board.KingSquares[colorIndex], isWhite);
            value += (int)(kingEarlyPhase * (1 - endgamePhaseWeight));
            int kingLatePhase = PieceSquareTable.Read(PieceSquareTable.KingEnd, board.KingSquares[colorIndex], isWhite);
            value += (int)(kingLatePhase * (endgamePhaseWeight));

            return value;
        }

        static int EvaluatePieceSquareTable(int[] table, PieceList pieceList, bool isWhite)
        {
            int value = 0;
            for (int i = 0; i < pieceList.Count; i++)
            {
                value += PieceSquareTable.Read(table, pieceList[i], isWhite);
            }
            return value;
        }
    }
}