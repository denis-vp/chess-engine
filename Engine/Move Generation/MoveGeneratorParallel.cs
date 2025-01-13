namespace chess_engine.Engine
{
    using System.Collections.Concurrent;
    using static PrecomputedMoveData;

    public class MoveGeneratorParallel : AbstractMoveGenerator
    {

        ConcurrentBag<Move> moves;
        int friendlyColour;
        int opponentColour;
        int friendlyKingSquare;
        int friendlyColourIndex;
        int opponentColourIndex;

        bool inCheck;
        bool inDoubleCheck;
        bool pinsExistInPosition;
        ulong checkRayBitmask;
        ulong pinRayBitmask;
        ulong opponentKnightAttacks;
        ulong opponentAttackMapNoPawns;
        ulong opponentSlidingAttackMap;

        bool genQuiets;

        override public AbstractMoveGenerator Clone(Board board)
        {
            AbstractMoveGenerator newMoveGenerater = new MoveGeneratorParallel();
            newMoveGenerater.board = board;
            newMoveGenerater.Init();
            return newMoveGenerater;
        }
        override public bool InCheck()
        {
            // Note, this will only return correct value after GenerateMoves() has been called in the current position
            return inCheck;
        }

        public override void Init()
        {
            moves = new ConcurrentBag<Move>();
            inCheck = false;
            inDoubleCheck = false;
            pinsExistInPosition = false;
            checkRayBitmask = 0;
            pinRayBitmask = 0;

            friendlyColour = board.ColorToMove;
            opponentColour = board.OpponentColor;
            friendlyKingSquare = board.KingSquares[board.ColorToMoveIndex];
            friendlyColourIndex = (board.WhiteToMove) ? Board.WhiteIndex : Board.BlackIndex;
            opponentColourIndex = 1 - friendlyColourIndex;
        }

        override public List<Move> GenerateMoves(Board board, bool includeQuietMoves = true)
        {
            this.board = board;
            genQuiets = includeQuietMoves;
            Init();

            ComputeAttackData();
            GenerateKingMoves();

            // Only king moves are valid in a double check position, so can return early.
            if (inDoubleCheck)
            {
                return moves.ToList();
            }

            var slidingTask = Task.Run(() => GenerateSlidingMoves());
            var knightTask = Task.Run(() => GenerateKnightMoves());
            var pawnTask = Task.Run(() => GeneratePawnMoves());

            Task.WaitAll(slidingTask, knightTask, pawnTask);
            return moves.ToList();
        }

        void GenerateKingMoves()
        {
            for (int i = 0; i < kingMoves[friendlyKingSquare].Length; i++)
            {
                int targetSquare = kingMoves[friendlyKingSquare][i];
                int pieceOnTargetSquare = board.Squares[targetSquare];

                // Skip squares occupied by friendly pieces
                if (Piece.IsColor(pieceOnTargetSquare, friendlyColour))
                {
                    continue;
                }

                bool isCapture = Piece.IsColor(pieceOnTargetSquare, opponentColour);
                if (!isCapture)
                {
                    // King can't move to square marked as under enemy control, unless he is capturing that piece
                    // Also skip if not generating quiet moves
                    if (!genQuiets || SquareIsInCheckRay(targetSquare))
                    {
                        continue;
                    }
                }

                // Safe for king to move to this square
                if (!SquareIsAttacked(targetSquare))
                {
                    moves.Add(new Move(friendlyKingSquare, targetSquare));

                    // Castling:
                    if (!inCheck && !isCapture)
                    {
                        // Castle kingside
                        if ((targetSquare == BoardHelper.f1 || targetSquare == BoardHelper.f8) && HasKingsideCastleRight)
                        {
                            int castleKingsideSquare = targetSquare + 1;
                            if (board.Squares[castleKingsideSquare] == Piece.None)
                            {
                                if (!SquareIsAttacked(castleKingsideSquare))
                                {
                                    moves.Add(new Move(friendlyKingSquare, castleKingsideSquare, Move.Flag.Castling));
                                }
                            }
                        }
                        // Castle queenside
                        else if ((targetSquare == BoardHelper.d1 || targetSquare == BoardHelper.d8) && HasQueensideCastleRight)
                        {
                            int castleQueensideSquare = targetSquare - 1;
                            if (board.Squares[castleQueensideSquare] == Piece.None && board.Squares[castleQueensideSquare - 1] == Piece.None)
                            {
                                if (!SquareIsAttacked(castleQueensideSquare))
                                {
                                    moves.Add(new Move(friendlyKingSquare, castleQueensideSquare, Move.Flag.Castling));
                                }
                            }
                        }
                    }
                }
            }
        }

        void GenerateSlidingMoves()
        {
            PieceList rooks = board.Rooks[friendlyColourIndex];
            for (int i = 0; i < rooks.Count; i++)
            {
                GenerateSlidingPieceMoves(rooks[i], 0, 4);
            }

            PieceList bishops = board.Bishops[friendlyColourIndex];
            for (int i = 0; i < bishops.Count; i++)
            {
                GenerateSlidingPieceMoves(bishops[i], 4, 8);
            }

            PieceList queens = board.Queens[friendlyColourIndex];
            for (int i = 0; i < queens.Count; i++)
            {
                GenerateSlidingPieceMoves(queens[i], 0, 8);
            }

        }

        void GenerateSlidingPieceMoves(int startSquare, int startDirIndex, int endDirIndex)
        {
            bool isPinned = IsPinned(startSquare);

            // If this piece is pinned, and the king is in check, this piece cannot move
            if (inCheck && isPinned)
            {
                return;
            }

            Parallel.For(startDirIndex, endDirIndex, directionIndex =>
            {
                int currentDirOffset = directionOffsets[directionIndex];

                // If pinned, this piece can only move along the ray towards/away from the friendly king, so skip other directions
                if (isPinned && !IsMovingAlongRay(currentDirOffset, friendlyKingSquare, startSquare))
                {
                    return;
                }

                for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++)
                {
                    int targetSquare = startSquare + currentDirOffset * (n + 1);
                    int targetSquarePiece = board.Squares[targetSquare];

                    // Blocked by friendly piece, so stop looking in this direction
                    if (Piece.IsColor(targetSquarePiece, friendlyColour))
                    {
                        break;
                    }
                    bool isCapture = targetSquarePiece != Piece.None;

                    bool movePreventsCheck = SquareIsInCheckRay(targetSquare);
                    if (movePreventsCheck || !inCheck)
                    {
                        if (genQuiets || isCapture)
                        {
                            moves.Add(new Move(startSquare, targetSquare));
                        }
                    }
                    // If square not empty, can't move any further in this direction
                    // Also, if this move blocked a check, further moves won't block the check
                    if (isCapture || movePreventsCheck)
                    {
                        break;
                    }
                }
            });
        }

        void GenerateKnightMoves()
        {
            PieceList myKnights = board.Knights[friendlyColourIndex];

            Parallel.For(0, myKnights.Count, i =>
            {
                int startSquare = myKnights[i];

                // Knight cannot move if it is pinned
                if (IsPinned(startSquare))
                {
                    return;
                }

                for (int knightMoveIndex = 0; knightMoveIndex < knightMoves[startSquare].Length; knightMoveIndex++)
                {
                    int targetSquare = knightMoves[startSquare][knightMoveIndex];
                    int targetSquarePiece = board.Squares[targetSquare];
                    bool isCapture = Piece.IsColor(targetSquarePiece, opponentColour);
                    if (genQuiets || isCapture)
                    {
                        // Skip if square contains friendly piece, or if in check and knight is not interposing/capturing checking piece
                        if (Piece.IsColor(targetSquarePiece, friendlyColour) || (inCheck && !SquareIsInCheckRay(targetSquare)))
                        {
                            continue;
                        }
                        moves.Add(new Move(startSquare, targetSquare));
                    }
                }
            });
        }

        void GeneratePawnMoves()
        {
            PieceList myPawns = board.Pawns[friendlyColourIndex];
            int pawnOffset = (friendlyColour == Piece.White) ? 8 : -8;
            int startRank = (board.WhiteToMove) ? 1 : 6;
            int finalRankBeforePromotion = (board.WhiteToMove) ? 6 : 1;

            int enPassantFile = ((int)(board.CurrentGameState >> 4) & 0b1111) - 1;
            int enPassantSquare = -1;
            if (enPassantFile != -1)
            {
                enPassantSquare = 8 * ((board.WhiteToMove) ? 5 : 2) + enPassantFile;
            }

            Parallel.For(0, myPawns.Count, i =>
            {
                int startSquare = myPawns[i];
                int rank = BoardHelper.RankIndex(startSquare);
                bool oneStepFromPromotion = rank == finalRankBeforePromotion;

                if (genQuiets)
                {

                    int squareOneForward = startSquare + pawnOffset;

                    // Square ahead of pawn is empty: forward moves
                    if (board.Squares[squareOneForward] == Piece.None)
                    {
                        // Pawn not pinned, or is moving along line of pin
                        if (!IsPinned(startSquare) || IsMovingAlongRay(pawnOffset, startSquare, friendlyKingSquare))
                        {
                            // Not in check, or pawn is interposing checking piece
                            if (!inCheck || SquareIsInCheckRay(squareOneForward))
                            {
                                if (oneStepFromPromotion)
                                {
                                    MakePromotionMoves(startSquare, squareOneForward);
                                }
                                else
                                {
                                    moves.Add(new Move(startSquare, squareOneForward));
                                }
                            }

                            // Is on starting square (so can move two forward if not blocked)
                            if (rank == startRank)
                            {
                                int squareTwoForward = squareOneForward + pawnOffset;
                                if (board.Squares[squareTwoForward] == Piece.None)
                                {
                                    // Not in check, or pawn is interposing checking piece
                                    if (!inCheck || SquareIsInCheckRay(squareTwoForward))
                                    {
                                        moves.Add(new Move(startSquare, squareTwoForward, Move.Flag.PawnTwoForward));
                                    }
                                }
                            }
                        }
                    }
                }

                // Pawn captures.
                for (int j = 0; j < 2; j++)
                {
                    // Check if square exists diagonal to pawn
                    if (numSquaresToEdge[startSquare][pawnAttackDirections[friendlyColourIndex][j]] > 0)
                    {
                        // move in direction friendly pawns attack to get square from which enemy pawn would attack
                        int pawnCaptureDir = directionOffsets[pawnAttackDirections[friendlyColourIndex][j]];
                        int targetSquare = startSquare + pawnCaptureDir;
                        int targetPiece = board.Squares[targetSquare];

                        // If piece is pinned, and the square it wants to move to is not on same line as the pin, then skip this direction
                        if (IsPinned(startSquare) && !IsMovingAlongRay(pawnCaptureDir, friendlyKingSquare, startSquare))
                        {
                            continue;
                        }

                        // Regular capture
                        if (Piece.IsColor(targetPiece, opponentColour))
                        {
                            // If in check, and piece is not capturing/interposing the checking piece, then skip to next square
                            if (inCheck && !SquareIsInCheckRay(targetSquare))
                            {
                                continue;
                            }
                            if (oneStepFromPromotion)
                            {
                                MakePromotionMoves(startSquare, targetSquare);
                            }
                            else
                            {
                                moves.Add(new Move(startSquare, targetSquare));
                            }
                        }

                        // Capture en-passant
                        if (targetSquare == enPassantSquare)
                        {
                            int epCapturedPawnSquare = targetSquare + ((board.WhiteToMove) ? -8 : 8);
                            if (!InCheckAfterEnPassant(startSquare, targetSquare, epCapturedPawnSquare))
                            {
                                moves.Add(new Move(startSquare, targetSquare, Move.Flag.EnPassantCapture));
                            }
                        }
                    }
                }
            });
        }

        void MakePromotionMoves(int fromSquare, int toSquare)
        {
            moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToQueen));
            if (promotionsToGenerate == PromotionMode.All)
            {
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToKnight));
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToRook));
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToBishop));
            }
            else if (promotionsToGenerate == PromotionMode.QueenAndKnight)
            {
                moves.Add(new Move(fromSquare, toSquare, Move.Flag.PromoteToKnight));
            }

        }

        bool IsMovingAlongRay(int rayDir, int startSquare, int targetSquare)
        {
            int moveDir = directionLookup[targetSquare - startSquare + 63];
            return (rayDir == moveDir || -rayDir == moveDir);
        }

        bool IsPinned(int square)
        {
            return pinsExistInPosition && ((pinRayBitmask >> square) & 1) != 0;
        }

        bool SquareIsInCheckRay(int square)
        {
            return inCheck && ((checkRayBitmask >> square) & 1) != 0;
        }

        bool HasKingsideCastleRight
        {
            get
            {
                int mask = (board.WhiteToMove) ? 1 : 4;
                return (board.CurrentGameState & mask) != 0;
            }
        }

        bool HasQueensideCastleRight
        {
            get
            {
                int mask = (board.WhiteToMove) ? 2 : 8;
                return (board.CurrentGameState & mask) != 0;
            }
        }

        void GenSlidingAttackMap()
        {
            opponentSlidingAttackMap = 0;

            PieceList enemyRooks = board.Rooks[opponentColourIndex];
            PieceList enemyQueens = board.Queens[opponentColourIndex];
            PieceList enemyBishops = board.Bishops[opponentColourIndex];

            var rookTask = Task.Run(() =>
            {
                for (int i = 0; i < enemyRooks.Count; i++)
                {
                    UpdateSlidingAttackPiece(enemyRooks[i], 0, 4);
                }
            });

            var queenTask = Task.Run(() =>
            {
                for (int i = 0; i < enemyQueens.Count; i++)
                {
                    UpdateSlidingAttackPiece(enemyQueens[i], 0, 8);
                }
            });

            var bishopTask = Task.Run(() =>
            {
                for (int i = 0; i < enemyBishops.Count; i++)
                {
                    UpdateSlidingAttackPiece(enemyBishops[i], 4, 8);
                }
            });

            Task.WaitAll(rookTask, queenTask, bishopTask);
        }

        void UpdateSlidingAttackPiece(int startSquare, int startDirIndex, int endDirIndex)
        {
            ulong localAttackMap = 0;

            for (int directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
            {
                int currentDirOffset = directionOffsets[directionIndex];
                for (int n = 0; n < numSquaresToEdge[startSquare][directionIndex]; n++)
                {
                    int targetSquare = startSquare + currentDirOffset * (n + 1);
                    int targetSquarePiece = board.Squares[targetSquare];
                    localAttackMap |= 1ul << targetSquare;
                    if (targetSquare != friendlyKingSquare)
                    {
                        if (targetSquarePiece != Piece.None)
                        {
                            break;
                        }
                    }
                }
            }

            Interlocked.Or(ref opponentSlidingAttackMap, localAttackMap);
        }

        void ComputeAttackData()
        {
            GenSlidingAttackMap();
            // Search squares in all directions around friendly king for checks/pins by enemy sliding pieces (queen, rook, bishop)
            int startDirIndex = 0;
            int endDirIndex = 8;

            if (board.Queens[opponentColourIndex].Count == 0)
            {
                startDirIndex = (board.Rooks[opponentColourIndex].Count > 0) ? 0 : 4;
                endDirIndex = (board.Bishops[opponentColourIndex].Count > 0) ? 8 : 4;
            }

            for (int dir = startDirIndex; dir < endDirIndex; dir++)
            {
                bool isDiagonal = dir > 3;

                int n = numSquaresToEdge[friendlyKingSquare][dir];
                int directionOffset = directionOffsets[dir];
                bool isFriendlyPieceAlongRay = false;
                ulong rayMask = 0;

                for (int i = 0; i < n; i++)
                {
                    int squareIndex = friendlyKingSquare + directionOffset * (i + 1);
                    rayMask |= 1ul << squareIndex;
                    int piece = board.Squares[squareIndex];

                    // This square contains a piece
                    if (piece != Piece.None)
                    {
                        if (Piece.IsColor(piece, friendlyColour))
                        {
                            // First friendly piece we have come across in this direction, so it might be pinned
                            if (!isFriendlyPieceAlongRay)
                            {
                                isFriendlyPieceAlongRay = true;
                            }
                            // This is the second friendly piece we've found in this direction, therefore pin is not possible
                            else
                            {
                                break;
                            }
                        }
                        // This square contains an enemy piece
                        else
                        {
                            int pieceType = Piece.PieceType(piece);

                            // Check if piece is in bitmask of pieces able to move in current direction
                            if (isDiagonal && Piece.IsBishopOrQueen(pieceType) || !isDiagonal && Piece.IsRookOrQueen(pieceType))
                            {
                                // Friendly piece blocks the check, so this is a pin
                                if (isFriendlyPieceAlongRay)
                                {
                                    pinsExistInPosition = true;
                                    pinRayBitmask |= rayMask;
                                }
                                // No friendly piece blocking the attack, so this is a check
                                else
                                {
                                    checkRayBitmask |= rayMask;
                                    inDoubleCheck = inCheck; // if already in check, then this is double check
                                    inCheck = true;
                                }
                                break;
                            }
                            else
                            {
                                // This enemy piece is not able to move in the current direction, and so is blocking any checks/pins
                                break;
                            }
                        }
                    }
                }
                // Stop searching for pins if in double check, as the king is the only piece able to move in that case anyway
                if (inDoubleCheck)
                {
                    break;
                }

            }

            PieceList opponentKnights = board.Knights[opponentColourIndex];
            PieceList opponentPawns = board.Pawns[opponentColourIndex];

            var knightTask = Task.Run(() =>
            {
                opponentKnightAttacks = 0;
                bool isKnightCheck = false;

                for (int knightIndex = 0; knightIndex < opponentKnights.Count; knightIndex++)
                {
                    int startSquare = opponentKnights[knightIndex];
                    opponentKnightAttacks |= knightAttackBitboards[startSquare];

                    if (!isKnightCheck && BitBoardUtility.ContainsSquare(opponentKnightAttacks, friendlyKingSquare))
                    {
                        isKnightCheck = true;
                        inDoubleCheck = inCheck; // if already in check, then this is double check
                        inCheck = true;
                        checkRayBitmask |= 1ul << startSquare;
                    }
                }
            });

            // Pawn attacks
            var pawnTask = Task.Run(() =>
            {
                opponentPawnAttackMap = 0;
                bool isPawnCheck = false;

                for (int pawnIndex = 0; pawnIndex < opponentPawns.Count; pawnIndex++)
                {
                    int pawnSquare = opponentPawns[pawnIndex];
                    ulong pawnAttacks = pawnAttackBitboards[pawnSquare][opponentColourIndex];
                    opponentPawnAttackMap |= pawnAttacks;

                    if (!isPawnCheck && BitBoardUtility.ContainsSquare(pawnAttacks, friendlyKingSquare))
                    {
                        isPawnCheck = true;
                        inDoubleCheck = inCheck; // if already in check, then this is double check
                        inCheck = true;
                        checkRayBitmask |= 1ul << pawnSquare;
                    }
                }
            });

            Task.WaitAll(knightTask, pawnTask);

            int enemyKingSquare = board.KingSquares[opponentColourIndex];

            opponentAttackMapNoPawns = opponentSlidingAttackMap | opponentKnightAttacks | kingAttackBitboards[enemyKingSquare];
            opponentAttackMap = opponentAttackMapNoPawns | opponentPawnAttackMap;
        }

        bool SquareIsAttacked(int square)
        {
            return BitBoardUtility.ContainsSquare(opponentAttackMap, square);
        }

        bool InCheckAfterEnPassant(int startSquare, int targetSquare, int epCapturedPawnSquare)
        {
            // Update board to reflect en-passant capture
            board.Squares[targetSquare] = board.Squares[startSquare];
            board.Squares[startSquare] = Piece.None;
            board.Squares[epCapturedPawnSquare] = Piece.None;

            bool inCheckAfterEpCapture = false;
            if (SquareAttackedAfterEPCapture(epCapturedPawnSquare, startSquare))
            {
                inCheckAfterEpCapture = true;
            }

            // Undo change to board
            board.Squares[targetSquare] = Piece.None;
            board.Squares[startSquare] = Piece.Pawn | friendlyColour;
            board.Squares[epCapturedPawnSquare] = Piece.Pawn | opponentColour;
            return inCheckAfterEpCapture;
        }

        bool SquareAttackedAfterEPCapture(int epCaptureSquare, int capturingPawnStartSquare)
        {
            if (BitBoardUtility.ContainsSquare(opponentAttackMapNoPawns, friendlyKingSquare))
            {
                return true;
            }

            // Loop through the horizontal direction towards ep capture to see if any enemy piece now attacks king
            int dirIndex = (epCaptureSquare < friendlyKingSquare) ? 2 : 3;
            for (int i = 0; i < numSquaresToEdge[friendlyKingSquare][dirIndex]; i++)
            {
                int squareIndex = friendlyKingSquare + directionOffsets[dirIndex] * (i + 1);
                int piece = board.Squares[squareIndex];
                if (piece != Piece.None)
                {
                    // Friendly piece is blocking view of this square from the enemy.
                    if (Piece.IsColor(piece, friendlyColour))
                    {
                        break;
                    }
                    // This square contains an enemy piece
                    else
                    {
                        if (Piece.IsRookOrQueen(piece))
                        {
                            return true;
                        }
                        else
                        {
                            // This piece is not able to move in the current direction, and is therefore blocking any checks along this line
                            break;
                        }
                    }
                }
            }

            // check if enemy pawn is controlling this square (can't use pawn attack bitboard, because pawn has been captured)
            for (int i = 0; i < 2; i++)
            {
                // Check if square exists diagonal to friendly king from which enemy pawn could be attacking it
                if (numSquaresToEdge[friendlyKingSquare][pawnAttackDirections[friendlyColourIndex][i]] > 0)
                {
                    // move in direction friendly pawns attack to get square from which enemy pawn would attack
                    int piece = board.Squares[friendlyKingSquare + directionOffsets[pawnAttackDirections[friendlyColourIndex][i]]];
                    if (piece == (Piece.Pawn | opponentColour)) // is enemy pawn
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}