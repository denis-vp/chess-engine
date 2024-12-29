using chess_engine.Engine;
using chess_engine.UI;

namespace chess_engine
{
    public class GameController
    {
        private Board board;
        private Searcher searcher;
        private MoveGenerator moveGenerator;
        private OpeningBook openingBook;

        private bool isPlayerTurn;
        private bool isWaitingToPlay;

        private Move moveToPlay;

        private BoardUI boardUI;
        private Player player;

        public GameController()
        {
            // Load the settings
            isPlayerTurn = Settings.IsPlayerWhite;
            isWaitingToPlay = !Settings.IsPlayerWhite;

            // Initialize game logic components
            board = new Board();
            board.LoadStartPosition();
            searcher = new Searcher(board);
            searcher.OnSearchComplete += OnMoveChosen; // Subscribe to the search complete event
            moveGenerator = new MoveGenerator();

            // Load the opening book
            openingBook = new OpeningBook(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.OpeningBookPath)));

            // Initialize UI components
            boardUI = new BoardUI();
            player = new Player(boardUI);
            player.SubscribeToMoveChosenEvent(OnMoveChosen);

            boardUI.UpdatePosition(board);
            boardUI.ResetSquareColours();

            bool perspective = Settings.InvertPerspective ? !Settings.IsPlayerWhite : Settings.IsPlayerWhite;
            boardUI.SetPerspective(perspective);

            // Start the game
            NotifyTurnToMove();
        }

        void NotifyTurnToMove()
        {
            if (isPlayerTurn)
            {
                // If it's the player's turn, notify the player
                player.SetPosition(FenUtility.CurrentFen(board));
                player.NotifyTurnToMove();
            }
            else
            {
                // If it's the engine's turn, check if we can use the opening book
                if (board.PlyCount <= Settings.MaxBookPly && openingBook.TryGetBookMove(board, out string moveString))
                {
                    // If we can, play the move from the opening book
                    Move bookMove = MoveUtility.GetMoveFromUCIName(moveString, board);
                    OnMoveChosen(bookMove);
                }
                else
                {
                    // If we can't, start the search on a separate thread to not block the UI
                    Task.Run(() => searcher.StartSearch());
                    // Also, set a timeout to end the search after a certain amount of time
                    Task.Run(async () =>
                    {
                        await Task.Delay(Settings.BotThinkTimeSeconds * 1000);
                        searcher.EndSearch();
                    });
                }
            }
        }

        void OnMoveChosen(Move chosenMove)
        {
            // If the player chose a move, play it immediately
            // Because we already checked if it's legal in the player class
            if (isPlayerTurn)
            {
                isPlayerTurn = !isPlayerTurn;
                PlayMove(chosenMove);
                return;
            }

            // When a move is chosen by the engine, check if it's legal and play it
            if (IsLegal(chosenMove))
            {
                // If it's the engine's turn, store the move to play later
                // This is because we can't play the move immediately from a separate thread
                moveToPlay = chosenMove;
                isWaitingToPlay = true;
            }
            else
            {
                // If the engine chose an illegal move then throw an exception
                throw new Exception("Illegal move from engine");
            }
        }

        bool IsLegal(Move givenMove)
        {
            // Generate all legal moves and check if the given move is one of them
            var moves = moveGenerator.GenerateMoves(board);
            foreach (var legalMove in moves)
            {
                if (givenMove.Value == legalMove.Value)
                {
                    return true;
                }
            }

            return false;
        }

        void PlayMove(Move move)
        {
            // Determine if the move should be animated (only for moves made by the engine)
            bool animate = isPlayerTurn;

            // Actually play the move and update the UI
            board.MakeMove(move, false);
            boardUI.UpdatePosition(board, move, animate);

            // Check if the game is over
            GameResult result = Arbiter.GetGameState(board);
            if (result == GameResult.InProgress)
            {
                // If the game is not over, notify the next player to move
                NotifyTurnToMove();
            }
            else
            {
                // TODO: Handle game over
            }
        }

        public void Update()
        {
            // Function to check if the game state has changed
            // Tell the player to update (if it's their turn then they will handle the input)
            // Otherwise, it will do nothing
            player.Update();
            // If the engine has chosen a move to play, play it
            if (isWaitingToPlay)
            {
                isPlayerTurn = !isPlayerTurn;
                isWaitingToPlay = false;
                PlayMove(moveToPlay);
            }
        }

        public void Draw()
        {
            boardUI.Draw();
        }

        public void Release()
        {
            // Clear the loaded resources
            boardUI.Release();
        }
    }
}
