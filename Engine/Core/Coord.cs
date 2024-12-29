namespace chess_engine.Engine
{
    public struct Coord
    {
        public readonly int fileIndex;
        public readonly int rankIndex;

        public Coord(int fileIndex, int rankIndex)
        {
            this.fileIndex = fileIndex;
            this.rankIndex = rankIndex;
        }

        public Coord(int squareIndex)
        {
            this.fileIndex = BoardHelper.FileIndex(squareIndex);
            this.rankIndex = BoardHelper.RankIndex(squareIndex);
        }

        public bool IsLightSquare()
        {
            return (fileIndex + rankIndex) % 2 != 0;
        }

        public int SquareIndex => BoardHelper.IndexFromCoord(this);
    }
}