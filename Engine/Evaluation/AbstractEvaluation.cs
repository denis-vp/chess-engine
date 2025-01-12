namespace chess_engine.Engine
{
    public abstract class AbstractEvaluation
    {
        public const int pawnValue = 100;
        public const int knightValue = 300;
        public const int bishopValue = 320;
        public const int rookValue = 500;
        public const int queenValue = 900;

        public abstract int Evaluate(Board board);
    }
}
