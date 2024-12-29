namespace chess_engine.Engine
{
    public static class FenUtility
    {

        static Dictionary<char, int> pieceTypeFromSymbol = new Dictionary<char, int>()
        {
            ['k'] = Piece.King,
            ['p'] = Piece.Pawn,
            ['n'] = Piece.Knight,
            ['b'] = Piece.Bishop,
            ['r'] = Piece.Rook,
            ['q'] = Piece.Queen
        };

        public const string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static LoadedPositionInfo PositionFromFen(string fen)
        {

            LoadedPositionInfo loadedPositionInfo = new LoadedPositionInfo();
            string[] sections = fen.Split(' ');

            int file = 0;
            int rank = 7;

            foreach (char symbol in sections[0])
            {
                if (symbol == '/')
                {
                    file = 0;
                    rank--;
                }
                else
                {
                    if (char.IsDigit(symbol))
                    {
                        file += (int)char.GetNumericValue(symbol);
                    }
                    else
                    {
                        int pieceColor = (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
                        int pieceType = pieceTypeFromSymbol[char.ToLower(symbol)];
                        loadedPositionInfo.squares[rank * 8 + file] = pieceType | pieceColor;
                        file++;
                    }
                }
            }

            loadedPositionInfo.whiteToMove = (sections[1] == "w");

            string castlingRights = (sections.Length > 2) ? sections[2] : "KQkq";
            loadedPositionInfo.whiteCastleKingside = castlingRights.Contains("K");
            loadedPositionInfo.whiteCastleQueenside = castlingRights.Contains("Q");
            loadedPositionInfo.blackCastleKingside = castlingRights.Contains("k");
            loadedPositionInfo.blackCastleQueenside = castlingRights.Contains("q");

            if (sections.Length > 3)
            {
                string enPassantFileName = sections[3][0].ToString();
                if (BoardHelper.fileNames.Contains(enPassantFileName))
                {
                    loadedPositionInfo.epFile = BoardHelper.fileNames.IndexOf(enPassantFileName) + 1;
                }
            }

            // Half-move clock
            if (sections.Length > 4)
            {
                int.TryParse(sections[4], out loadedPositionInfo.plyCount);
            }
            return loadedPositionInfo;
        }

        public static string CurrentFen(Board board, bool alwaysIncludeEPSquare = true)
        {
            string fen = "";
            for (int rank = 7; rank >= 0; rank--)
            {
                int numEmptyFiles = 0;
                for (int file = 0; file < 8; file++)
                {
                    int i = rank * 8 + file;
                    int piece = board.Squares[i];
                    if (piece != 0)
                    {
                        if (numEmptyFiles != 0)
                        {
                            fen += numEmptyFiles;
                            numEmptyFiles = 0;
                        }
                        bool isBlack = Piece.IsColor(piece, Piece.Black);
                        int pieceType = Piece.PieceType(piece);
                        char pieceChar = ' ';
                        switch (pieceType)
                        {
                            case Piece.Rook:
                                pieceChar = 'R';
                                break;
                            case Piece.Knight:
                                pieceChar = 'N';
                                break;
                            case Piece.Bishop:
                                pieceChar = 'B';
                                break;
                            case Piece.Queen:
                                pieceChar = 'Q';
                                break;
                            case Piece.King:
                                pieceChar = 'K';
                                break;
                            case Piece.Pawn:
                                pieceChar = 'P';
                                break;
                        }
                        fen += (isBlack) ? pieceChar.ToString().ToLower() : pieceChar.ToString();
                    }
                    else
                    {
                        numEmptyFiles++;
                    }

                }
                if (numEmptyFiles != 0)
                {
                    fen += numEmptyFiles;
                }
                if (rank != 0)
                {
                    fen += '/';
                }
            }

            // Side to move
            fen += ' ';
            fen += (board.WhiteToMove) ? 'w' : 'b';

            // Castling
            bool whiteKingside = (board.CurrentGameState & 1) == 1;
            bool whiteQueenside = (board.CurrentGameState >> 1 & 1) == 1;
            bool blackKingside = (board.CurrentGameState >> 2 & 1) == 1;
            bool blackQueenside = (board.CurrentGameState >> 3 & 1) == 1;
            fen += ' ';
            fen += (whiteKingside) ? "K" : "";
            fen += (whiteQueenside) ? "Q" : "";
            fen += (blackKingside) ? "k" : "";
            fen += (blackQueenside) ? "q" : "";
            fen += ((board.CurrentGameState & 15) == 0) ? "-" : "";

            // En-passant
            fen += ' ';
            int epFile = (int)(board.CurrentGameState >> 4) & 15;

            if (alwaysIncludeEPSquare && epFile != 0)
            {
                string fileName = BoardHelper.fileNames[epFile - 1].ToString();
                int epRank = (board.WhiteToMove) ? 6 : 3;
                fen += fileName + epRank;
            }
            else
            {
                fen += '-';
            }

            // 50 move counter
            fen += ' ';
            fen += board.FiftyMoveCounter;

            // Full-move count (should be one at start, and increase after each move by black)
            fen += ' ';
            fen += (board.PlyCount / 2) + 1;

            return fen;
        }

        public class LoadedPositionInfo
        {
            public int[] squares;
            public bool whiteCastleKingside;
            public bool whiteCastleQueenside;
            public bool blackCastleKingside;
            public bool blackCastleQueenside;
            public int epFile;
            public bool whiteToMove;
            public int plyCount;

            public LoadedPositionInfo()
            {
                squares = new int[64];
            }
        }

    }
}