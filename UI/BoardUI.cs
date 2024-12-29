using chess_engine.Engine;
using Raylib_cs;
using System.Numerics;

namespace chess_engine.UI
{
    public class BoardUI
    {

        // Board settings
        const int squareSize = 100;
        const double moveAnimDuration = 0.15;
        bool whitePerspective = true;

        // Drag state
        bool isDraggingPiece;
        int dragSquare;
        Vector2 dragPos;

        // Board assets
        static readonly int[] pieceImageOrder = { 5, 3, 2, 4, 1, 0 };
        Texture2D piecesTexture;
        BoardTheme theme;
        Dictionary<int, Color> squareColOverrides;

        // Board state
        Board board;
        Move lastMove;

        // Animate move state
        Board animateMoveTargetBoardState;
        Move moveToAnimate;
        double moveAnimStartTime;
        bool isAnimatingMove;

        // Fonts and sounds assets
        Font font;
        Sound moveSound;

        // Enums to help with text alignment
        public enum AlignH
        {
            Left,
            Centre,
            Right
        }
        public enum AlignV
        {
            Top,
            Centre,
            Bottom
        }
        public enum HighlightType
        {
            MoveFrom,
            MoveTo,
            LegalMove,
            Check
        }

        public BoardUI()
        {
            // Initialize the theme
            theme = new BoardTheme();

            // Load the move sound and font assets
            font = Raylib.LoadFont(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.FontPath));
            moveSound = Raylib.LoadSound(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.MoveSoundPath));

            LoadPieceTexture();

            // Initialize the board state
            board = new Board();
            board.LoadStartPosition();
            squareColOverrides = new Dictionary<int, Color>();
        }

        public void SetPerspective(bool whitePerspective)
        {
            this.whitePerspective = whitePerspective;
        }

        public void UpdatePosition(Board board)
        {
            isAnimatingMove = false;

            // Update
            this.board = new(board);
            lastMove = Move.NullMove;
            if (board.IsInCheck())
            {
                OverrideSquareColour(board.KingSquares[board.MoveColorIndex], HighlightType.Check);
            }
        }

        public void UpdatePosition(Board board, Move moveMade, bool animate = false)
        {
            // Interrupt previous animation
            if (isAnimatingMove)
            {
                UpdatePosition(animateMoveTargetBoardState);
                isAnimatingMove = false;
            }

            ResetSquareColours();
            if (animate)
            {
                OverrideSquareColour(moveMade.StartSquare, HighlightType.MoveFrom);
                animateMoveTargetBoardState = new Board(board);
                moveToAnimate = moveMade;
                moveAnimStartTime = Raylib.GetTime();
                isAnimatingMove = true;
            }
            else
            {
                UpdatePosition(board);

                if (!moveMade.IsNull)
                {
                    HighlightMove(moveMade);
                    lastMove = moveMade;
                }
            }

            Raylib.PlaySound(moveSound);
        }

        void HighlightMove(Move move)
        {
            OverrideSquareColour(move.StartSquare, HighlightType.MoveFrom);
            OverrideSquareColour(move.TargetSquare, HighlightType.MoveTo);
        }

        public void DragPiece(int square, Vector2 worldPos)
        {
            isDraggingPiece = true;
            dragSquare = square;
            dragPos = worldPos;
        }

        public bool TryGetSquareAtPoint(Vector2 worldPos, out int squareIndex)
        {
            Vector2 boardStartPosWorld = new Vector2(squareSize, squareSize) * -4;
            Vector2 endPosWorld = boardStartPosWorld + new Vector2(8, 8) * squareSize;

            float tx = (worldPos.X - boardStartPosWorld.X) / (endPosWorld.X - boardStartPosWorld.X);
            float ty = (worldPos.Y - boardStartPosWorld.Y) / (endPosWorld.Y - boardStartPosWorld.Y);

            if (tx >= 0 && tx <= 1 && ty >= 0 && ty <= 1)
            {
                if (!whitePerspective)
                {
                    tx = 1 - tx;
                    ty = 1 - ty;
                }
                squareIndex = new Coord((int)(tx * 8), 7 - (int)(ty * 8)).SquareIndex;
                return true;
            }

            squareIndex = -1;
            return false;
        }

        public void OverrideSquareColour(int square, HighlightType hightlightType)
        {
            bool isLight = new Coord(square).IsLightSquare();

            Color col = hightlightType switch
            {
                HighlightType.MoveFrom => isLight ? theme.MoveFromLight : theme.MoveFromDark,
                HighlightType.MoveTo => isLight ? theme.MoveToLight : theme.MoveToDark,
                HighlightType.LegalMove => isLight ? theme.LegalLight : theme.LegalDark,
                HighlightType.Check => isLight ? theme.CheckLight : theme.CheckDark,
                _ => Color.Magenta
            };

            if (squareColOverrides.ContainsKey(square))
            {
                squareColOverrides[square] = col;
            }
            else
            {
                squareColOverrides.Add(square, col);
            }
        }

        public void HighlightLegalMoves(Board board, int square)
        {
            MoveGenerator moveGenerator = new();
            var moves = moveGenerator.GenerateMoves(board);
            foreach (var move in moves)
            {
                if (move.StartSquare == square)
                {
                    OverrideSquareColour(move.TargetSquare, HighlightType.LegalMove);
                }
            }
        }

        public void Draw()
        {
            double animT = (Raylib.GetTime() - moveAnimStartTime) / moveAnimDuration;

            if (isAnimatingMove && animT >= 1)
            {
                isAnimatingMove = false;
                UpdatePosition(animateMoveTargetBoardState, moveToAnimate, false);
            }

            ForEachSquare(DrawSquare);

            if (isAnimatingMove)
            {
                UpdateMoveAnimation(animT);
            }

            if (isDraggingPiece)
            {
                DrawPiece(board.Squares[dragSquare], dragPos - new Vector2(squareSize * 0.5f, squareSize * 0.5f));
            }


            // Reset state
            isDraggingPiece = false;
        }

        static void ForEachSquare(Action<int, int> action)
        {
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    action(x, y);
                }
            }
        }

        void UpdateMoveAnimation(double animT)
        {
            Coord startCoord = new Coord(moveToAnimate.StartSquare);
            Coord targetCoord = new Coord(moveToAnimate.TargetSquare);
            Vector2 startPos = GetSquarePos(startCoord.fileIndex, startCoord.rankIndex, whitePerspective);
            Vector2 targetPos = GetSquarePos(targetCoord.fileIndex, targetCoord.rankIndex, whitePerspective);

            Vector2 animPos = Vector2.Lerp(startPos, targetPos, (float)animT);
            DrawPiece(board.Squares[moveToAnimate.StartSquare], animPos);
        }

        public void ResetSquareColours(bool keepPrevMoveHighlight = false)
        {
            squareColOverrides.Clear();
            if (keepPrevMoveHighlight && !lastMove.IsNull)
            {
                HighlightMove(lastMove);
            }
        }

        void DrawSquare(int file, int rank)
        {

            Coord coord = new Coord(file, rank);
            Color col = coord.IsLightSquare() ? theme.LightColor : theme.DarkColor;
            if (squareColOverrides.TryGetValue(coord.SquareIndex, out Color overrideCol))
            {
                col = overrideCol;
            }

            // Top Left
            Vector2 pos = GetSquarePos(file, rank, whitePerspective);
            Raylib.DrawRectangle((int)pos.X, (int)pos.Y, squareSize, squareSize, col);
            int piece = board.Squares[coord.SquareIndex];
            float alpha = isDraggingPiece && dragSquare == coord.SquareIndex ? 0.3f : 1;
            if (!isAnimatingMove || coord.SquareIndex != moveToAnimate.StartSquare)
            {
                DrawPiece(piece, new Vector2((int)pos.X, (int)pos.Y), alpha);
            }

            int textSize = 25;
            Color coordNameCol = coord.IsLightSquare() ? theme.DarkCoordColor : theme.LightCoordColor;

            if (rank == (whitePerspective ? 0 : 7))
            {
                float xpadding = -10f;
                float ypadding = 2f;
                string fileName = BoardHelper.fileNames[file] + "";
                Vector2 drawPos = pos + new Vector2(squareSize + xpadding, squareSize - ypadding);
                DrawText(fileName, drawPos, textSize, 0, coordNameCol, AlignH.Right, AlignV.Bottom);
            }
            if (file == (whitePerspective ? 0 : 7))
            {
                float xpadding = 5f;
                float ypadding = 5f;
                string rankName = (rank + 1) + "";
                Vector2 drawPos = pos + new Vector2(xpadding, ypadding);
                DrawText(rankName, drawPos, textSize, 0, coordNameCol, AlignH.Left, AlignV.Top);
            }
        }

        static Vector2 GetSquarePos(int file, int rank, bool whitePerspective)
        {
            const int boardStartX = -squareSize * 4;
            const int boardStartY = -squareSize * 4;

            if (!whitePerspective)
            {
                file = 7 - file;
                rank = 7 - rank;
            }

            int posX = boardStartX + file * squareSize;
            int posY = boardStartY + (7 - rank) * squareSize;
            return new Vector2(posX, posY);
        }

        void DrawPiece(int piece, Vector2 posTopLeft, float alpha = 1)
        {
            if (piece != Piece.None)
            {
                int type = Piece.PieceType(piece);
                bool white = Piece.IsWhite(piece);
                Rectangle srcRect = GetPieceTextureRect(type, white);
                Rectangle targRect = new Rectangle((int)posTopLeft.X, (int)posTopLeft.Y, squareSize, squareSize);

                Color tint = new Color(255, 255, 255, (int)MathF.Round(255 * alpha));
                Raylib.DrawTexturePro(piecesTexture, srcRect, targRect, new Vector2(0, 0), 0, tint);
            }
        }

        void LoadPieceTexture()
        {
            byte[] pieceImgBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.PiecesPath));
            Image pieceImg = Raylib.LoadImageFromMemory(".png", pieceImgBytes);
            piecesTexture = Raylib.LoadTextureFromImage(pieceImg);
            Raylib.UnloadImage(pieceImg);

            Raylib.GenTextureMipmaps(ref piecesTexture);
            Raylib.SetTextureWrap(piecesTexture, TextureWrap.Clamp);
            Raylib.SetTextureFilter(piecesTexture, TextureFilter.Bilinear);
        }

        public void Release()
        {
            // Unload assets
            Raylib.UnloadTexture(piecesTexture);
            Raylib.UnloadFont(font);
            Raylib.UnloadSound(moveSound);
        }

        static Rectangle GetPieceTextureRect(int pieceType, bool isWhite)
        {
            const int size = 333;
            return new Rectangle(size * pieceImageOrder[pieceType - 1], isWhite ? 0 : size, size, size);
        }

        public void DrawText(string text, Vector2 pos, int size, int spacing, Color col, AlignH alignH = AlignH.Left, AlignV alignV = AlignV.Centre)
        {
            Vector2 boundSize = Raylib.MeasureTextEx(font, text, size, spacing);
            float offsetX = alignH == AlignH.Left ? 0 : (alignH == AlignH.Centre ? -boundSize.X / 2 : -boundSize.X);
            float offsetY = alignV == AlignV.Top ? 0 : (alignV == AlignV.Centre ? -boundSize.Y / 2 : -boundSize.Y);
            Vector2 offset = new(offsetX, offsetY);
            Raylib.DrawTextEx(font, text, pos + offset, size, spacing, col);
        }
    }
}