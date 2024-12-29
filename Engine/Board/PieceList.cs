namespace chess_engine.Engine
{
    public class PieceList
    {

        // Contains the index of the squares ocupied by the given piece type
        public int[] occupiedSquares;
        // Map from index of a square, to the index in the occupiedSquares array where that square is stored
        int[] map;
        int numPieces;

        public PieceList(int maxPieceCount = 16)
        {
            occupiedSquares = new int[maxPieceCount];
            map = new int[64];
            numPieces = 0;
        }

        public int Count
        {
            get
            {
                return numPieces;
            }
        }

        public void AddPieceAtSquare(int square)
        {
            occupiedSquares[numPieces] = square;
            map[square] = numPieces;
            numPieces++;
        }

        public void RemovePieceAtSquare(int square)
        {
            // Get the index of this element in the occupiedSquares array
            int pieceIndex = map[square];
            // Move last element in array to the place of the removed element
            occupiedSquares[pieceIndex] = occupiedSquares[numPieces - 1];
            // Update map to point to the moved element's new location in the array
            map[occupiedSquares[pieceIndex]] = pieceIndex;
            numPieces--;
        }

        public void MovePiece(int startSquare, int targetSquare)
        {
            // Get the index of this element in the occupiedSquares array
            int pieceIndex = map[startSquare];
            occupiedSquares[pieceIndex] = targetSquare;
            map[targetSquare] = pieceIndex;
        }

        public int this[int index] => occupiedSquares[index];

    }
}