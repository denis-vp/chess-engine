using chess_engine.Engine.Search;
using static System.Math;

namespace chess_engine.Engine
{
    public class Searcher : AbstractSearcher
    {

        const int numEntriestranspositionTable = 64000;
        const int positiveInfinity = 9999999;
        const int negativeInfinity = -positiveInfinity;
        const int MaxExtensions = 16;

        TranspositionTable tt;
        AbstractMoveGenerator moveGenerator;

        Move bestMoveThisIteration;
        int bestEvalThisIteration;
        Move bestMove;
        int bestEval;
        bool searchCancelled;

        MoveOrdering moveOrdering;
        Board board;
        AbstractEvaluation evaluation;

        public Searcher(Board board)
        {
            this.board = board;
            evaluation = Settings.EvaluationParallel ? new EvaluationParallel() : new Evaluation();
            moveGenerator = Settings.MoveGenerationParallel ? new MoveGeneratorParallel() : new MoveGenerator();
            tt = new TranspositionTable(board, numEntriestranspositionTable);
            moveOrdering = new MoveOrdering(moveGenerator);
        }

        override public void StartSearch()
        {
            // Initialize search settings
            bestMoveThisIteration = bestMove = Move.NullMove;
            bestEvalThisIteration = bestEval = 0;
            tt.enabled = true;

            moveGenerator.promotionsToGenerate = AbstractMoveGenerator.PromotionMode.QueenAndKnight;
            searchCancelled = false;

            for (int searchDepth = 1; searchDepth <= 256; searchDepth++)
            {
                bestMoveThisIteration = Move.NullMove;
                bestEvalThisIteration = negativeInfinity;
                SearchMoves(searchDepth, 0, negativeInfinity, positiveInfinity);

                if (!Move.IsNullMove(bestMoveThisIteration))
                {
                    bestMove = bestMoveThisIteration;
                    bestEval = bestEvalThisIteration;
                    if (Settings.PrintSearch)
                    {
                        Console.WriteLine($"Depth: {searchDepth}, Best move: {MoveUtility.GetMoveNameUCI(bestMove)}, Eval: {bestEval}");
                    }
                }

                if (searchCancelled)
                {
                    if (Settings.PrintSearch)
                    {
                        string colorToMove = board.ColorToMove == Piece.White ? "White" : "Black";
                        Console.WriteLine($"Cancelled, Depth: {searchDepth}, Best move: {MoveUtility.GetMoveNameUCI(bestMove)}, Eval: {bestEval}, Color: {colorToMove}");
                    }
                    break;
                }
            }

            // If search didn't find a move before being cancelled, just play the first move generated
            if (Move.IsNullMove(bestMove))
            {
                bestMove = moveGenerator.GenerateMoves(board)[0];
                if (Settings.PrintSearch)
                {
                    Console.WriteLine($"No move found, playing first move generated: {MoveUtility.GetMoveNameUCI(bestMove)}");
                }
            }

            InvokeOnSearchComplete(bestMove);
        }

        override public void EndSearch()
        {
            searchCancelled = true;
        }

        int SearchMoves(int depth, int plyFromRoot, int alpha, int beta, List<Move> moves = null, int numExtensions = 0)
        {
            if (searchCancelled)
            {
                return 0;
            }

            if (plyFromRoot > 0)
            {
                // Detect draw by repetition.
                // Returns a draw score even if this position has only appeared once in the game history (for simplicity).
                if (board.RepetitionPositionHistory.Contains(board.ZobristKey))
                {
                    return 0;
                }

                // Skip this position if a mating sequence has already been found earlier in
                // the search, which would be shorter than any mate we could find from here.
                // This is done by observing that alpha can't possibly be worse (and likewise
                // beta can't  possibly be better) than being mated in the current position.
                alpha = Max(alpha, -immediateMateScore + plyFromRoot);
                beta = Min(beta, immediateMateScore - plyFromRoot);
                if (alpha >= beta)
                {
                    return alpha;
                }
            }

            // Try looking up the current position in the transposition table.
            // If the same position has already been searched to at least an equal depth
            // to the search we're doing now,we can just use the recorded evaluation.
            int ttVal = tt.LookupEvaluation(depth, plyFromRoot, alpha, beta);
            if (ttVal != TranspositionTable.lookupFailed)
            {
                if (plyFromRoot == 0)
                {
                    bestMoveThisIteration = tt.GetStoredMove();
                    bestEvalThisIteration = tt.entries[tt.Index].value;
                }
                return ttVal;
            }

            if (depth == 0)
            {
                int evaluation = QuiescenceSearch(alpha, beta);
                return evaluation;
            }

            if (moves == null)
            {
                moves = moveGenerator.GenerateMoves(board);
            }
            Move prevBestMove = plyFromRoot == 0 ? bestMove : tt.TryGetStoredMove();
            moveOrdering.OrderMoves(prevBestMove, board, moves);
            // Detect checkmate and stalemate when no legal moves are available
            if (moves.Count == 0)
            {
                if (moveGenerator.InCheck())
                {
                    int mateScore = immediateMateScore - plyFromRoot;
                    return -mateScore;
                }
                else
                {
                    return 0;
                }
            }

            int evalType = TranspositionTable.UpperBound;
            Move bestMoveInThisPosition = Move.NullMove;

            for (int i = 0; i < moves.Count; i++)
            {
                board.MakeMove(moves[i], inSearch: true);
                List<Move> opponentMoves = moveGenerator.GenerateMoves(board);
                int extension = ComputeExtensionDepth(moves[i], numExtensions);
                int eval = -SearchMoves(depth - 1 + extension, plyFromRoot + 1, -beta, -alpha, opponentMoves, numExtensions + extension);
                board.UnmakeMove(moves[i], inSearch: true);

                if (searchCancelled)
                {
                    return 0;
                }

                // Move was *too* good, so opponent won't allow this position to be reached
                // (by choosing a different move earlier on). Skip remaining moves.
                if (eval >= beta)
                {
                    tt.StoreEvaluation(depth, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);
                    return beta;
                }

                // Found a new best move in this position
                if (eval > alpha)
                {
                    evalType = TranspositionTable.Exact;
                    bestMoveInThisPosition = moves[i];

                    alpha = eval;
                    if (plyFromRoot == 0)
                    {
                        bestMoveThisIteration = moves[i];
                        bestEvalThisIteration = eval;
                    }
                }
            }

            tt.StoreEvaluation(depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

            return alpha;

        }

        // Search capture moves until a 'quiet' position is reached.
        int QuiescenceSearch(int alpha, int beta)
        {
            // A player isn't forced to make a capture (typically), so see what the evaluation is without capturing anything.
            // This prevents situations where a player ony has bad captures available from being evaluated as bad,
            // when the player might have good non-capture moves available.
            int eval = evaluation.Evaluate(board);
            if (eval >= beta)
            {
                return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
            }

            var moves = moveGenerator.GenerateMoves(board, false);
            moveOrdering.OrderMoves(Move.NullMove, board, moves);
            for (int i = 0; i < moves.Count; i++)
            {
                board.MakeMove(moves[i], true);
                eval = -QuiescenceSearch(-beta, -alpha);
                board.UnmakeMove(moves[i], true);

                if (eval >= beta)
                {
                    return beta;
                }
                if (eval > alpha)
                {
                    alpha = eval;
                }
            }

            return alpha;
        }

        int ComputeExtensionDepth(Move movePlayed, int numExtensions)
        {
            int movedPieceType = Piece.PieceType(board.Squares[movePlayed.StartSquare]);
            int targetRank = BoardHelper.RankIndex(movePlayed.TargetSquare);
            int extension = 0;
            if (numExtensions < MaxExtensions)
            {
                if (moveGenerator.InCheck())
                {
                    extension = 1;
                }
                else if (movedPieceType == Piece.Pawn && (targetRank == 6 || targetRank == 1))
                {
                    extension = 1;
                }
            }
            return extension;
        }
    }
}