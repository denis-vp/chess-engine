namespace chess_engine.Engine
{
    public abstract class AbstractMoveGenerator
    {
        public enum PromotionMode { All, QueenOnly, QueenAndKnight }
        public PromotionMode promotionsToGenerate = PromotionMode.All;
        public ulong opponentAttackMap;
        public ulong opponentPawnAttackMap;

        public abstract bool InCheck();

        public abstract List<Move> GenerateMoves(Board board, bool includeQuietMoves = true);
    }
}
