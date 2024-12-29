using chess_engine.Engine;
using Raylib_cs;
using System.Numerics;

namespace chess_engine.UI
{
    internal class Player
    {
        public event Action<Move>? MoveChosen;

        readonly Board board;
        readonly BoardUI boardUI;

        // State
        bool isDragging;
        int selectedSquare;
        bool isTurnToMove;


        public Player(BoardUI boardUI)
        {
            board = new();
            board.LoadStartPosition();
            this.boardUI = boardUI;
        }

        public void NotifyTurnToMove()
        {
            isTurnToMove = true;
        }

        public void SetPosition(string fen)
        {
            board.LoadPosition(fen);
        }

        public void Update()
        {
            // If it's not the player's turn, don't do anything
            if (!isTurnToMove)
            {
                return;
            }
            // Get the mouse position in world coordinates
            Vector2 mouseScreenPos = Raylib.GetMousePosition();
            Vector2 mouseWorldPos = Program.ScreenToWorldPos(mouseScreenPos);

            // Handle input
            // If the player is not dragging a piece
            // And the player presses the left mouse button
            if (LeftMousePressedThisFrame())
            {
                // Get the square the player clicked on
                if (boardUI.TryGetSquareAtPoint(mouseWorldPos, out int square))
                {
                    // If the square contains a piece of the player's colour
                    // Start dragging the piece
                    int piece = board.Squares[square];
                    if (Piece.IsColour(piece, board.IsWhiteToMove ? Piece.White : Piece.Black))
                    {
                        isDragging = true;
                        selectedSquare = square;
                        boardUI.HighlightLegalMoves(board, square);
                    }
                }
            }

            // If the player is dragging a piece
            if (isDragging)
            {
                // If the player releases the left mouse button
                if (LeftMouseReleasedThisFrame())
                {
                    // If the player released the mouse over a square
                    // Try to make the move
                    CancelDrag();
                    if (boardUI.TryGetSquareAtPoint(mouseWorldPos, out int square))
                    {
                        TryMakeMove(selectedSquare, square);
                    }
                }
                // If the player presses the right mouse button
                // Cancel the drag
                else if (RightMousePressedThisFrame())
                {
                    CancelDrag();
                }
                // Otherwise, if no mouse buttons are pressed
                // Drag the piece
                else
                {
                    boardUI.DragPiece(selectedSquare, mouseWorldPos);
                }
            }
        }

        void CancelDrag()
        {
            isDragging = false;
            boardUI.ResetSquareColours(true);
        }

        void TryMakeMove(int startSquare, int targetSquare)
        {
            // Check if the move is legal
            bool isLegal = false;
            Move move = Move.NullMove;

            // Generate all legal moves and check if the given move is one of them
            MoveGenerator generator = new();
            var legalMoves = generator.GenerateMoves(board);
            foreach (var legalMove in legalMoves)
            {
                if (legalMove.StartSquare == startSquare && legalMove.TargetSquare == targetSquare)
                {
                    isLegal = true;
                    move = legalMove;
                    break;
                }
            }

            // If the move is legal, play it
            if (isLegal)
            {
                isTurnToMove = false;
                MoveChosen?.Invoke(move);
            }
        }

        static bool LeftMousePressedThisFrame() => Raylib.IsMouseButtonPressed(MouseButton.Left);
        static bool LeftMouseReleasedThisFrame() => Raylib.IsMouseButtonReleased(MouseButton.Left);
        static bool RightMousePressedThisFrame() => Raylib.IsMouseButtonPressed(MouseButton.Right);

        public void SubscribeToMoveChosenEvent(Action<Move> action)
        {
            MoveChosen += action;
        }
    }
}
