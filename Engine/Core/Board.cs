namespace chess_engine.Engine
{
    public class Board
    {

        public const int WhiteIndex = 0;
        public const int BlackIndex = 1;

        // Stores piece code for each square on the board.
        public int[] Squares;
        // Index of square of white and black king
        public int[] KingSquares;

        public bool WhiteToMove;
        public int ColorToMove;
        public int OpponentColor;
        public int ColorToMoveIndex;

        // Bits 0-3 store white and black kingside/queenside castling legality
        // Bits 4-7 store file of ep square (starting at 1, so 0 = no ep square)
        // Bits 8-13 captured piece
        // Bits 14-... fifty mover counter
        public uint CurrentGameState;
        Stack<uint> gameStateHistory;

        public int PlyCount; // Total plies played in game
        public int FiftyMoveCounter; // Number of plies since last pawn move or capture

        public ulong ZobristKey;
        public Stack<ulong> RepetitionPositionHistory;

        public PieceList[] Rooks;
        public PieceList[] Bishops;
        public PieceList[] Queens;
        public PieceList[] Knights;
        public PieceList[] Pawns;

        PieceList[] allPieceLists;

        PieceList GetPieceList(int pieceType, int colorIndex)
        {
            return allPieceLists[colorIndex * 8 + pieceType];
        }

        const uint whiteCastleKingsideMask = 0b1111111111111110;
        const uint whiteCastleQueensideMask = 0b1111111111111101;
        const uint blackCastleKingsideMask = 0b1111111111111011;
        const uint blackCastleQueensideMask = 0b1111111111110111;

        const uint whiteCastleMask = whiteCastleKingsideMask & whiteCastleQueensideMask;
        const uint blackCastleMask = blackCastleKingsideMask & blackCastleQueensideMask;

        public int MoveColorIndex => WhiteToMove ? WhiteIndex : BlackIndex;

        public Board(Board? source = null)
        {
            if (source != null)
            {
                LoadPosition(FenUtility.CurrentFen(source));
            }
        }

        void Initialize()
        {
            Squares = new int[64];
            KingSquares = new int[2];

            gameStateHistory = new Stack<uint>();
            ZobristKey = 0;
            RepetitionPositionHistory = new Stack<ulong>();
            PlyCount = 0;
            FiftyMoveCounter = 0;

            Knights = [new PieceList(10), new PieceList(10)];
            Pawns = [new PieceList(8), new PieceList(8)];
            Rooks = [new PieceList(10), new PieceList(10)];
            Bishops = [new PieceList(10), new PieceList(10)];
            Queens = [new PieceList(9), new PieceList(9)];
            PieceList emptyList = new PieceList(0);
            allPieceLists = [
                emptyList,
                emptyList,
                Pawns[WhiteIndex],
                Knights[WhiteIndex],
                emptyList,
                Bishops[WhiteIndex],
                Rooks[WhiteIndex],
                Queens[WhiteIndex],
                emptyList,
                emptyList,
                Pawns[BlackIndex],
                Knights[BlackIndex],
                emptyList,
                Bishops[BlackIndex],
                Rooks[BlackIndex],
                Queens[BlackIndex],
            ];
        }

        public void LoadStartPosition()
        {
            LoadPosition(FenUtility.startFen);
        }

        public void LoadPosition(string fen)
        {
            Initialize();
            var loadedPosition = FenUtility.PositionFromFen(fen);

            // Load pieces into board array and piece lists
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                int piece = loadedPosition.squares[squareIndex];
                Squares[squareIndex] = piece;

                if (piece != Piece.None)
                {
                    int pieceType = Piece.PieceType(piece);
                    int pieceColorIndex = (Piece.IsColor(piece, Piece.White)) ? WhiteIndex : BlackIndex;
                    if (Piece.IsSlidingPiece(piece))
                    {
                        if (pieceType == Piece.Queen)
                        {
                            Queens[pieceColorIndex].AddPieceAtSquare(squareIndex);
                        }
                        else if (pieceType == Piece.Rook)
                        {
                            Rooks[pieceColorIndex].AddPieceAtSquare(squareIndex);
                        }
                        else if (pieceType == Piece.Bishop)
                        {
                            Bishops[pieceColorIndex].AddPieceAtSquare(squareIndex);
                        }
                    }
                    else if (pieceType == Piece.Knight)
                    {
                        Knights[pieceColorIndex].AddPieceAtSquare(squareIndex);
                    }
                    else if (pieceType == Piece.Pawn)
                    {
                        Pawns[pieceColorIndex].AddPieceAtSquare(squareIndex);
                    }
                    else if (pieceType == Piece.King)
                    {
                        KingSquares[pieceColorIndex] = squareIndex;
                    }
                }
            }

            // Side to move
            WhiteToMove = loadedPosition.whiteToMove;
            ColorToMove = (WhiteToMove) ? Piece.White : Piece.Black;
            OpponentColor = (WhiteToMove) ? Piece.Black : Piece.White;
            ColorToMoveIndex = (WhiteToMove) ? 0 : 1;

            // Create gamestate
            int whiteCastle = ((loadedPosition.whiteCastleKingside) ? 1 << 0 : 0) | ((loadedPosition.whiteCastleQueenside) ? 1 << 1 : 0);
            int blackCastle = ((loadedPosition.blackCastleKingside) ? 1 << 2 : 0) | ((loadedPosition.blackCastleQueenside) ? 1 << 3 : 0);
            int epState = loadedPosition.epFile << 4;
            ushort initialGameState = (ushort)(whiteCastle | blackCastle | epState);
            gameStateHistory.Push(initialGameState);
            CurrentGameState = initialGameState;
            PlyCount = loadedPosition.plyCount;

            // Initialize zobrist key
            ZobristKey = Zobrist.CalculateZobristKey(this);
        }

        public bool IsInCheck()
        {

            AbstractMoveGenerator moveGenerator = Settings.MoveGenerationParallel ? new MoveGeneratorParallel() : new MoveGenerator();
            moveGenerator.GenerateMoves(this, false);
            return moveGenerator.InCheck();
        }

        // Make a move on the board
        // inSearch parameter controls whether this move should be recorded in the game history (for detecting three-fold repetition)
        public void MakeMove(Move move, bool inSearch = false)
        {
            uint oldEnPassantFile = (CurrentGameState >> 4) & 15;
            uint originalCastleState = CurrentGameState & 15;
            uint newCastleState = originalCastleState;
            CurrentGameState = 0;

            int opponentColorIndex = 1 - ColorToMoveIndex;
            int moveFrom = move.StartSquare;
            int moveTo = move.TargetSquare;

            int capturedPieceType = Piece.PieceType(Squares[moveTo]);
            int movePiece = Squares[moveFrom];
            int movePieceType = Piece.PieceType(movePiece);

            int moveFlag = move.MoveFlag;
            bool isPromotion = move.IsPromotion;
            bool isEnPassant = moveFlag == Move.Flag.EnPassantCapture;

            // Handle captures
            CurrentGameState |= (ushort)(capturedPieceType << 8);
            if (capturedPieceType != 0 && !isEnPassant)
            {
                ZobristKey ^= Zobrist.piecesArray[capturedPieceType, opponentColorIndex, moveTo];
                GetPieceList(capturedPieceType, opponentColorIndex).RemovePieceAtSquare(moveTo);
            }

            // Move pieces in piece lists
            if (movePieceType == Piece.King)
            {
                KingSquares[ColorToMoveIndex] = moveTo;
                newCastleState &= (WhiteToMove) ? whiteCastleMask : blackCastleMask;
            }
            else
            {
                GetPieceList(movePieceType, ColorToMoveIndex).MovePiece(moveFrom, moveTo);
            }

            int pieceOnTargetSquare = movePiece;

            // Handle promotion
            if (isPromotion)
            {
                int promoteType = 0;
                switch (moveFlag)
                {
                    case Move.Flag.PromoteToQueen:
                        promoteType = Piece.Queen;
                        Queens[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToRook:
                        promoteType = Piece.Rook;
                        Rooks[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToBishop:
                        promoteType = Piece.Bishop;
                        Bishops[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;
                    case Move.Flag.PromoteToKnight:
                        promoteType = Piece.Knight;
                        Knights[ColorToMoveIndex].AddPieceAtSquare(moveTo);
                        break;

                }
                pieceOnTargetSquare = promoteType | ColorToMove;
                Pawns[ColorToMoveIndex].RemovePieceAtSquare(moveTo);
            }
            else
            {
                // Handle other special moves (en-passant, and castling)
                switch (moveFlag)
                {
                    case Move.Flag.EnPassantCapture:
                        int epPawnSquare = moveTo + ((ColorToMove == Piece.White) ? -8 : 8);
                        CurrentGameState |= (ushort)(Squares[epPawnSquare] << 8); // Add pawn as capture type
                        Squares[epPawnSquare] = 0; // Clear ep capture square
                        Pawns[opponentColorIndex].RemovePieceAtSquare(epPawnSquare);
                        ZobristKey ^= Zobrist.piecesArray[Piece.Pawn, opponentColorIndex, epPawnSquare];
                        break;
                    case Move.Flag.Castling:
                        bool kingside = moveTo == BoardHelper.g1 || moveTo == BoardHelper.g8;
                        int castlingRookFromIndex = (kingside) ? moveTo + 1 : moveTo - 2;
                        int castlingRookToIndex = (kingside) ? moveTo - 1 : moveTo + 1;

                        Squares[castlingRookFromIndex] = Piece.None;
                        Squares[castlingRookToIndex] = Piece.Rook | ColorToMove;

                        Rooks[ColorToMoveIndex].MovePiece(castlingRookFromIndex, castlingRookToIndex);
                        ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColorToMoveIndex, castlingRookFromIndex];
                        ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColorToMoveIndex, castlingRookToIndex];
                        break;
                }
            }

            // Update the board representation:
            Squares[moveTo] = pieceOnTargetSquare;
            Squares[moveFrom] = 0;

            // Pawn has moved two forwards, mark file with en-passant flag
            if (moveFlag == Move.Flag.PawnTwoForward)
            {
                int file = BoardHelper.FileIndex(moveFrom) + 1;
                CurrentGameState |= (ushort)(file << 4);
                ZobristKey ^= Zobrist.enPassantFile[file];
            }

            // Piece moving to/from rook square removes castling right for that side
            if (originalCastleState != 0)
            {
                if (moveTo == BoardHelper.h1 || moveFrom == BoardHelper.h1)
                {
                    newCastleState &= whiteCastleKingsideMask;
                }
                else if (moveTo == BoardHelper.a1 || moveFrom == BoardHelper.a1)
                {
                    newCastleState &= whiteCastleQueensideMask;
                }
                if (moveTo == BoardHelper.h8 || moveFrom == BoardHelper.h8)
                {
                    newCastleState &= blackCastleKingsideMask;
                }
                else if (moveTo == BoardHelper.a8 || moveFrom == BoardHelper.a8)
                {
                    newCastleState &= blackCastleQueensideMask;
                }
            }

            // Update zobrist key with new piece position and side to move
            ZobristKey ^= Zobrist.sideToMove;
            ZobristKey ^= Zobrist.piecesArray[movePieceType, ColorToMoveIndex, moveFrom];
            ZobristKey ^= Zobrist.piecesArray[Piece.PieceType(pieceOnTargetSquare), ColorToMoveIndex, moveTo];

            if (oldEnPassantFile != 0)
                ZobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];

            if (newCastleState != originalCastleState)
            {
                ZobristKey ^= Zobrist.castlingRights[originalCastleState]; // Remove old castling rights state
                ZobristKey ^= Zobrist.castlingRights[newCastleState]; // Add new castling rights state
            }
            CurrentGameState |= newCastleState;
            CurrentGameState |= (uint)FiftyMoveCounter << 14;
            gameStateHistory.Push(CurrentGameState);

            // Change side to move
            WhiteToMove = !WhiteToMove;
            ColorToMove = (WhiteToMove) ? Piece.White : Piece.Black;
            OpponentColor = (WhiteToMove) ? Piece.Black : Piece.White;
            ColorToMoveIndex = 1 - ColorToMoveIndex;
            PlyCount++;
            FiftyMoveCounter++;

            if (!inSearch)
            {
                if (movePieceType == Piece.Pawn || capturedPieceType != Piece.None)
                {
                    RepetitionPositionHistory.Clear();
                    FiftyMoveCounter = 0;
                }
                else
                {
                    RepetitionPositionHistory.Push(ZobristKey);
                }
            }

        }

        public void UnmakeMove(Move move, bool inSearch = false)
        {

            int opponentColorIndex = ColorToMoveIndex;
            bool undoingWhiteMove = OpponentColor == Piece.White;
            ColorToMove = OpponentColor; // Side whose move we are undoing
            OpponentColor = (undoingWhiteMove) ? Piece.Black : Piece.White;
            ColorToMoveIndex = 1 - ColorToMoveIndex;
            WhiteToMove = !WhiteToMove;

            uint originalCastleState = CurrentGameState & 0b1111;

            int capturedPieceType = ((int)CurrentGameState >> 8) & 0b111111;
            int capturedPiece = (capturedPieceType == 0) ? 0 : capturedPieceType | OpponentColor;

            int movedFrom = move.StartSquare;
            int movedTo = move.TargetSquare;
            int moveFlags = move.MoveFlag;
            bool isEnPassant = moveFlags == Move.Flag.EnPassantCapture;
            bool isPromotion = move.IsPromotion;

            int toSquarePieceType = Piece.PieceType(Squares[movedTo]);
            int movedPieceType = (isPromotion) ? Piece.Pawn : toSquarePieceType;

            // Update zobrist key with new piece position and side to move
            ZobristKey ^= Zobrist.sideToMove;
            ZobristKey ^= Zobrist.piecesArray[movedPieceType, ColorToMoveIndex, movedFrom]; // Add piece back to square it moved from
            ZobristKey ^= Zobrist.piecesArray[toSquarePieceType, ColorToMoveIndex, movedTo]; // Remove piece from square it moved to

            uint oldEnPassantFile = (CurrentGameState >> 4) & 0b1111;
            if (oldEnPassantFile != 0)
                ZobristKey ^= Zobrist.enPassantFile[oldEnPassantFile];

            // Ignore ep captures, handled later
            if (capturedPieceType != 0 && !isEnPassant)
            {
                ZobristKey ^= Zobrist.piecesArray[capturedPieceType, opponentColorIndex, movedTo];
                GetPieceList(capturedPieceType, opponentColorIndex).AddPieceAtSquare(movedTo);
            }

            // Update king index
            if (movedPieceType == Piece.King)
            {
                KingSquares[ColorToMoveIndex] = movedFrom;
            }
            else if (!isPromotion)
            {
                GetPieceList(movedPieceType, ColorToMoveIndex).MovePiece(movedTo, movedFrom);
            }

            // Put back moved piece
            Squares[movedFrom] = movedPieceType | ColorToMove;
            Squares[movedTo] = capturedPiece;

            if (isPromotion)
            {
                Pawns[ColorToMoveIndex].AddPieceAtSquare(movedFrom);
                switch (moveFlags)
                {
                    case Move.Flag.PromoteToQueen:
                        Queens[ColorToMoveIndex].RemovePieceAtSquare(movedTo);
                        break;
                    case Move.Flag.PromoteToKnight:
                        Knights[ColorToMoveIndex].RemovePieceAtSquare(movedTo);
                        break;
                    case Move.Flag.PromoteToRook:
                        Rooks[ColorToMoveIndex].RemovePieceAtSquare(movedTo);
                        break;
                    case Move.Flag.PromoteToBishop:
                        Bishops[ColorToMoveIndex].RemovePieceAtSquare(movedTo);
                        break;
                }
            }
            else if (isEnPassant)
            {
                // Ep cature: put captured pawn back on right square

                int epIndex = movedTo + ((ColorToMove == Piece.White) ? -8 : 8);
                Squares[movedTo] = 0;
                Squares[epIndex] = (int)capturedPiece;
                Pawns[opponentColorIndex].AddPieceAtSquare(epIndex);
                ZobristKey ^= Zobrist.piecesArray[Piece.Pawn, opponentColorIndex, epIndex];
            }
            else if (moveFlags == Move.Flag.Castling)
            {
                // Castles: move rook back to starting square

                bool kingside = movedTo == 6 || movedTo == 62;
                int castlingRookFromIndex = (kingside) ? movedTo + 1 : movedTo - 2;
                int castlingRookToIndex = (kingside) ? movedTo - 1 : movedTo + 1;

                Squares[castlingRookToIndex] = 0;
                Squares[castlingRookFromIndex] = Piece.Rook | ColorToMove;

                Rooks[ColorToMoveIndex].MovePiece(castlingRookToIndex, castlingRookFromIndex);
                ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColorToMoveIndex, castlingRookFromIndex];
                ZobristKey ^= Zobrist.piecesArray[Piece.Rook, ColorToMoveIndex, castlingRookToIndex];

            }

            gameStateHistory.Pop(); // Removes current state from history
            CurrentGameState = gameStateHistory.Peek(); // Sets current state to previous state in history

            FiftyMoveCounter = (int)(CurrentGameState & 0b11111111111111110000000000000000) >> 14;
            int newEnPassantFile = (int)(CurrentGameState >> 4) & 0b1111;
            if (newEnPassantFile != 0)
                ZobristKey ^= Zobrist.enPassantFile[newEnPassantFile];

            uint newCastleState = CurrentGameState & 0b1111;
            if (newCastleState != originalCastleState)
            {
                ZobristKey ^= Zobrist.castlingRights[originalCastleState]; // Remove old castling rights state
                ZobristKey ^= Zobrist.castlingRights[newCastleState]; // Add new castling rights state
            }

            PlyCount--;

            if (!inSearch && RepetitionPositionHistory.Count > 0)
            {
                RepetitionPositionHistory.Pop();
            }
        }
    }
}