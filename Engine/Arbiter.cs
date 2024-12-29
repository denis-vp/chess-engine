﻿namespace chess_engine.Engine
{
    public enum GameResult
    {
        NotStarted,
        InProgress,
        WhiteIsMated,
        BlackIsMated,
        Stalemate,
        Repetition,
        FiftyMoveRule,
        InsufficientMaterial,
        DrawByArbiter,
        WhiteTimeout,
        BlackTimeout,
        WhiteIllegalMove,
        BlackIllegalMove
    }

    public static class Arbiter
    {
        public static bool IsDrawResult(GameResult result)
        {
            return result is GameResult.DrawByArbiter or GameResult.FiftyMoveRule or
                GameResult.Repetition or GameResult.Stalemate or GameResult.InsufficientMaterial;
        }

        public static bool IsWhiteWinsResult(GameResult result)
        {
            return result is GameResult.BlackIsMated or GameResult.BlackTimeout or GameResult.BlackIllegalMove;
        }

        public static bool IsBlackWinsResult(GameResult result)
        {
            return result is GameResult.WhiteIsMated or GameResult.WhiteTimeout or GameResult.WhiteIllegalMove;
        }

        public static GameResult GetGameState(Board board)
        {
            MoveGenerator moveGenerator = new MoveGenerator();
            var moves = moveGenerator.GenerateMoves(board);

            // Look for mate/stalemate
            if (moves.Count == 0)
            {
                if (moveGenerator.InCheck())
                {
                    return (board.WhiteToMove) ? GameResult.WhiteIsMated : GameResult.BlackIsMated;
                }
                return GameResult.Stalemate;
            }

            // Fifty move rule
            if (board.FiftyMoveCounter >= 100)
            {
                return GameResult.FiftyMoveRule;
            }

            // Threefold repetition
            int repCount = board.RepetitionPositionHistory.Count((x => x == board.ZobristKey));
            if (repCount == 3)
            {
                return GameResult.Repetition;
            }

            // Look for insufficient material
            if (InsufficentMaterial(board))
            {
                return GameResult.InsufficientMaterial;
            }
            return GameResult.InProgress;
        }

        // Test for insufficient material
        public static bool InsufficentMaterial(Board board)
        {
            // Can't have insufficient material with pawns on the board
            if (board.Pawns[Board.WhiteIndex].Count > 0 || board.Pawns[Board.BlackIndex].Count > 0)
            {
                return false;
            }

            int numWhiteQueens = board.Queens[Board.WhiteIndex].Count;
            int numBlackQueens = board.Queens[Board.BlackIndex].Count;
            int numWhiteRooks = board.Rooks[Board.WhiteIndex].Count;
            int numBlackRooks = board.Rooks[Board.BlackIndex].Count;

            // Can't have insufficient material with queens/rooks on the board
            if (numWhiteQueens > 0 || numBlackQueens > 0 || numWhiteRooks > 0 || numBlackRooks > 0)
            {
                return false;
            }

            // If no pawns, queens, or rooks on the board, then consider knight and bishop cases
            int numWhiteBishops = board.Bishops[Board.WhiteIndex].Count;
            int numBlackBishops = board.Bishops[Board.BlackIndex].Count;
            int numWhiteKnights = board.Knights[Board.WhiteIndex].Count;
            int numBlackKnights = board.Knights[Board.BlackIndex].Count;
            int numWhiteMinors = numWhiteBishops + numWhiteKnights;
            int numBlackMinors = numBlackBishops + numBlackKnights;
            int numMinors = numWhiteMinors + numBlackMinors;

            // Lone kings or King vs King + single minor: is insuffient
            if (numMinors <= 1)
            {
                return true;
            }

            // Bishop vs bishop: is insufficient when bishops are same colour complex
            if (numMinors == 2 && numWhiteBishops == 1 && numBlackBishops == 1)
            {
                bool whiteBishopIsLightSquare = BoardHelper.IsLightSquare(board.Bishops[Board.WhiteIndex][0]);
                bool blackBishopIsLightSquare = BoardHelper.IsLightSquare(board.Bishops[Board.BlackIndex][0]);
                return whiteBishopIsLightSquare == blackBishopIsLightSquare;
            }

            return false;
        }
    }
}