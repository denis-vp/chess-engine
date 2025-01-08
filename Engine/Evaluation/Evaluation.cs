namespace chess_engine.Engine
{
    public class Evaluation
    {

        public const int pawnValue = 100;
        public const int knightValue = 300;
        public const int bishopValue = 320;
        public const int rookValue = 500;
        public const int queenValue = 900;

        const float endgameMaterialStart = rookValue * 2 + bishopValue + knightValue;
        Board board;

        // Performs static evaluation of the current position.
        // The position is assumed to be 'quiet', i.e no captures are available that could drastically affect the evaluation.
        // The score that's returned is given from the perspective of whoever's turn it is to move.
        // So a positive score means the player who's turn it is to move has an advantage, while a negative score indicates a disadvantage.
        public int Evaluate(Board board)
        {
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

            // Parallelize the evaluations for white and black
            Parallel.Invoke(
                () => whiteEval += EvaluatePieceSquareTables(Board.WhiteIndex, blackEndgamePhaseWeight),
                () => blackEval += EvaluatePieceSquareTables(Board.BlackIndex, whiteEndgamePhaseWeight)
            );

            int eval = whiteEval - blackEval;

            int perspective = (board.WhiteToMove) ? 1 : -1;
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

            // Parallelize the piece-square table evaluations
            int rooksValue = 0, knightsValue = 0, bishopsValue = 0, queensValue = 0, kingEarlyPhase = 0, kingLatePhase = 0;
            int pawnEarly = 0, pawnLate = 0;

            Parallel.Invoke(
                () => rooksValue = EvaluatePieceSquareTable(PieceSquareTable.Rooks, board.Rooks[colorIndex], isWhite),
                () => knightsValue = EvaluatePieceSquareTable(PieceSquareTable.Knights, board.Knights[colorIndex], isWhite),
                () => bishopsValue = EvaluatePieceSquareTable(PieceSquareTable.Bishops, board.Bishops[colorIndex], isWhite),
                () => queensValue = EvaluatePieceSquareTable(PieceSquareTable.Queens, board.Queens[colorIndex], isWhite),
                () =>
                {
                    pawnEarly = EvaluatePieceSquareTable(PieceSquareTable.PawnsStart, board.Pawns[colorIndex], isWhite);
                    pawnEarly = (int)(pawnEarly * (1 - endgamePhaseWeight));
                },
                () =>
                {
                    pawnLate = EvaluatePieceSquareTable(PieceSquareTable.PawnsEnd, board.Pawns[colorIndex], isWhite);
                    pawnLate = (int)(pawnLate * endgamePhaseWeight);
                },
                () =>
                {
                    kingEarlyPhase = PieceSquareTable.Read(PieceSquareTable.KingStart, board.KingSquares[colorIndex], isWhite);
                    kingEarlyPhase = (int)(kingEarlyPhase * (1 - endgamePhaseWeight));
                },
                () =>
                {
                    kingLatePhase = PieceSquareTable.Read(PieceSquareTable.KingEnd, board.KingSquares[colorIndex], isWhite);
                    kingLatePhase = (int)(kingLatePhase * endgamePhaseWeight);
                }
            );

            value += rooksValue + knightsValue + bishopsValue + queensValue + kingEarlyPhase + kingLatePhase + pawnEarly + pawnLate;

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