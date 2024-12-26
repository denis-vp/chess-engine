namespace chess_engine.Core
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
}