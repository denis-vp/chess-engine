using chess_engine.Engine.Search;
using static System.Math;

namespace chess_engine.Engine
{
    public class SearcherParallel : AbstractSearcher
    {

        const int numEntriestranspositionTable = 64000;
        const int positiveInfinity = 9999999;
        const int negativeInfinity = -positiveInfinity;
        const int MaxExtensions = 16;
        const int nrOfThreads = 8;

        TranspositionTable tt;
        AbstractMoveGenerator moveGenerator;

        int bestEvalDepth;
        Move bestMove;
        Board board;
        int bestEval;
        bool searchCancelled;
        object ttLock = new object();

        MoveOrdering moveOrdering;
        AbstractEvaluation evaluation;

        public SearcherParallel(Board board)
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
            bestMove = Move.NullMove;
            bestEval = bestEvalDepth = 0;
            tt.enabled = false;
            var threads = new Thread[nrOfThreads];

            moveGenerator.promotionsToGenerate = AbstractMoveGenerator.PromotionMode.QueenAndKnight;
            searchCancelled = false;
            object syncObject = new object(); // For synchronizing best move updates

            using (var countdown = new CountdownEvent(nrOfThreads))
            {
                for (int threadIndex = 0; threadIndex < nrOfThreads; threadIndex++)
                {
                    threads[threadIndex] = new Thread(() =>
                    {
                        int threadDepth = threadIndex + 1;
                        int localBestEvalThisIteration = negativeInfinity;
                        Move localBestMoveThisIteration = Move.NullMove;
                        Board localBoard = new(board);
                        AbstractMoveGenerator localMoveGenerator = new MoveGenerator();

                        int eval = SearchMovesParallel(threadDepth, 0, negativeInfinity, positiveInfinity, ref localBestEvalThisIteration, ref localBestMoveThisIteration, ref localBoard, ref localMoveGenerator);

                        if (threadDepth > bestEvalDepth && !Move.IsNullMove(localBestMoveThisIteration))
                        {
                            bestEval = eval;
                            bestEvalDepth = threadDepth;
                            bestMove = localBestMoveThisIteration;
                            if (Settings.PrintSearch)
                            {
                                Console.WriteLine($"Depth: {threadDepth}, Best move: {MoveUtility.GetMoveNameUCI(bestMove)}, Eval: {bestEval}");
                            }
                        }

                        countdown.Signal();
                    });
                    threads[threadIndex].Start();
                }
                countdown.Wait();
            }

            if (Settings.PrintSearch && searchCancelled)
            {
                Console.WriteLine($"Cancelled, Depth: {bestEvalDepth}, Best move: {MoveUtility.GetMoveNameUCI(bestMove)}, Eval: {bestEval}");

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

        int SearchMovesParallel(int depth, int plyFromRoot, int alpha, int beta, ref int bestEvalThisIteration, ref Move bestMoveThisIteration, ref Board board, ref AbstractMoveGenerator moveGenerator, List<Move> moves = null, int numExtensions = 0)
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

            int ttVal = TranspositionTable.lookupFailed;

            lock (ttLock)
            {
                ttVal = tt.LookupEvaluation(depth, plyFromRoot, alpha, beta);
            }
            if (ttVal != TranspositionTable.lookupFailed)
            {
                if (plyFromRoot == 0)
                {
                    lock (ttLock)
                    {
                        bestMoveThisIteration = tt.GetStoredMove();
                        bestEvalThisIteration = tt.entries[tt.Index].value;
                    }

                }
                return ttVal;
            }

            if (depth == 0)
            {
                int evaluation = QuiescenceSearch(alpha, beta, ref board, ref moveGenerator);
                return evaluation;
            }

            if (moves == null)
            {
                moves = moveGenerator.GenerateMoves(board);
            }
            Move prevBestMove = Move.NullMove;
            lock (ttLock)
            {
                prevBestMove = plyFromRoot == 0 ? bestMove : tt.TryGetStoredMove();

            }
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
                int extension = ComputeExtensionDepthParallel(moves[i], numExtensions, ref board, ref moveGenerator);
                int eval = -SearchMovesParallel(depth - 1 + extension, plyFromRoot + 1, -beta, -alpha, ref bestEvalThisIteration, ref bestMoveThisIteration, ref board, ref moveGenerator, opponentMoves, numExtensions + extension);
                board.UnmakeMove(moves[i], inSearch: true);

                if (searchCancelled)
                {
                    return 0;
                }

                // Move was *too* good, so opponent won't allow this position to be reached
                // (by choosing a different move earlier on). Skip remaining moves.
                if (eval >= beta)
                {
                    lock (ttLock)
                    {
                        tt.StoreEvaluation(depth, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);
                    }
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
            lock (ttLock)
            {
                tt.StoreEvaluation(depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);
            }

            return alpha;
        }

        // Search capture moves until a 'quiet' position is reached.
        int QuiescenceSearch(int alpha, int beta, ref Board board, ref AbstractMoveGenerator moveGenerator)
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
                eval = -QuiescenceSearch(-beta, -alpha, ref board, ref moveGenerator);
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

        int ComputeExtensionDepthParallel(Move movePlayed, int numExtensions, ref Board board, ref AbstractMoveGenerator moveGenerator)
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