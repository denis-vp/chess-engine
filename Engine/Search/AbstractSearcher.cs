using static System.Math;

namespace chess_engine.Engine.Search
{
    public abstract class AbstractSearcher
    {
        protected const int immediateMateScore = 100000;

        public event Action<Move> OnSearchComplete;

        public abstract void StartSearch();

        public abstract void EndSearch();

        protected void InvokeOnSearchComplete(Move move)
        {
            OnSearchComplete?.Invoke(move);
        }

        public static bool IsMateScore(int score)
        {
            const int maxMateDepth = 1000;
            return Abs(score) > immediateMateScore - maxMateDepth;
        }
    }
}
