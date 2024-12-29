using System.Collections.ObjectModel;

namespace chess_engine.Engine
{
    public static class FenUtility
    {
        public const string StartPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static PositionInfo PositionFromFen(string fen)
        {

            PositionInfo loadedPositionInfo = new(fen);
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
                        bool isBlack = Piece.IsColour(piece, Piece.Black);
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
            fen += (board.IsWhiteToMove) ? 'w' : 'b';

            // Castling
            bool whiteKingside = (board.CurrentGameState.castlingRights & 1) == 1;
            bool whiteQueenside = (board.CurrentGameState.castlingRights >> 1 & 1) == 1;
            bool blackKingside = (board.CurrentGameState.castlingRights >> 2 & 1) == 1;
            bool blackQueenside = (board.CurrentGameState.castlingRights >> 3 & 1) == 1;
            fen += ' ';
            fen += (whiteKingside) ? "K" : "";
            fen += (whiteQueenside) ? "Q" : "";
            fen += (blackKingside) ? "k" : "";
            fen += (blackQueenside) ? "q" : "";
            fen += ((board.CurrentGameState.castlingRights) == 0) ? "-" : "";

            // En-passant
            fen += ' ';
            int epFileIndex = board.CurrentGameState.enPassantFile - 1;
            int epRankIndex = (board.IsWhiteToMove) ? 5 : 2;

            bool isEnPassant = epFileIndex != -1;
            bool includeEP = alwaysIncludeEPSquare || EnPassantCanBeCaptured(epFileIndex, epRankIndex, board);
            if (isEnPassant && includeEP)
            {
                fen += BoardHelper.SquareNameFromCoordinate(epFileIndex, epRankIndex);
            }
            else
            {
                fen += '-';
            }

            // 50 move counter
            fen += ' ';
            fen += board.CurrentGameState.fiftyMoveCounter;

            // Full-move count
            fen += ' ';
            fen += (board.PlyCount / 2) + 1;

            return fen;
        }

        static bool EnPassantCanBeCaptured(int epFileIndex, int epRankIndex, Board board)
        {
            Coord captureFromA = new Coord(epFileIndex - 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
            Coord captureFromB = new Coord(epFileIndex + 1, epRankIndex + (board.IsWhiteToMove ? -1 : 1));
            int epCaptureSquare = new Coord(epFileIndex, epRankIndex).SquareIndex;
            int friendlyPawn = Piece.MakePiece(Piece.Pawn, board.MoveColor);

            return CanCapture(captureFromA) || CanCapture(captureFromB);

            bool CanCapture(Coord from)
            {
                bool isPawnOnSquare = board.Squares[from.SquareIndex] == friendlyPawn;
                if (from.IsValidSquare() && isPawnOnSquare)
                {
                    Move move = new Move(from.SquareIndex, epCaptureSquare, Move.EnPassantCaptureFlag);
                    board.MakeMove(move);
                    board.MakeNullMove();
                    bool wasLegalMove = !board.CalculateInCheckState();

                    board.UnmakeNullMove();
                    board.UnmakeMove(move);
                    return wasLegalMove;
                }

                return false;
            }
        }

        public readonly struct PositionInfo
        {
            public readonly string fen;
            public readonly ReadOnlyCollection<int> squares;

            // Castling rights
            public readonly bool whiteCastleKingside;
            public readonly bool whiteCastleQueenside;
            public readonly bool blackCastleKingside;
            public readonly bool blackCastleQueenside;
            // En passant file (1 is a-file, 8 is h-file, 0 means none)
            public readonly int epFile;
            public readonly bool whiteToMove;
            // Number of plies since last capture or pawn advance (starts at 0 and increments after each player's move)
            public readonly int fiftyMovePlyCount;
            // Total number of moves played in the game (starts at 1 and increments after black's move)
            public readonly int moveCount;

            public PositionInfo(string fen)
            {
                this.fen = fen;
                int[] squarePieces = new int[64];

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
                            int pieceColour = (char.IsUpper(symbol)) ? Piece.White : Piece.Black;
                            int pieceType = char.ToLower(symbol) switch
                            {
                                'k' => Piece.King,
                                'p' => Piece.Pawn,
                                'n' => Piece.Knight,
                                'b' => Piece.Bishop,
                                'r' => Piece.Rook,
                                'q' => Piece.Queen,
                                _ => Piece.None
                            };

                            squarePieces[rank * 8 + file] = pieceType | pieceColour;
                            file++;
                        }
                    }
                }

                squares = new(squarePieces);

                whiteToMove = (sections[1] == "w");

                string castlingRights = sections[2];
                whiteCastleKingside = castlingRights.Contains('K');
                whiteCastleQueenside = castlingRights.Contains('Q');
                blackCastleKingside = castlingRights.Contains('k');
                blackCastleQueenside = castlingRights.Contains('q');

                // Default values
                epFile = 0;
                fiftyMovePlyCount = 0;
                moveCount = 0;

                if (sections.Length > 3)
                {
                    string enPassantFileName = sections[3][0].ToString();
                    if (BoardHelper.fileNames.Contains(enPassantFileName))
                    {
                        epFile = BoardHelper.fileNames.IndexOf(enPassantFileName) + 1;
                    }
                }

                // Ply clock
                if (sections.Length > 4)
                {
                    int.TryParse(sections[4], out fiftyMovePlyCount);
                }
                // Full move number
                if (sections.Length > 5)
                {
                    int.TryParse(sections[5], out moveCount);
                }
            }
        }
    }
}