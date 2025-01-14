namespace chess_engine.Engine
{
    public abstract class AbstractMoveGenerator
    {
        public enum PromotionMode { All, QueenOnly, QueenAndKnight }
        public PromotionMode promotionsToGenerate = PromotionMode.All;
        public ulong opponentAttackMap;
        public ulong opponentPawnAttackMap;
        public Board board;
        public abstract bool InCheck();

        public abstract List<Move> GenerateMoves(Board board, bool includeQuietMoves = true);
        public abstract void Init();
        public abstract AbstractMoveGenerator Clone(Board board);
    }
}
