using chess_engine.Engine;
using chess_engine.UI;

namespace chess_engine.Game
{
    public class GameController
    {
        public enum PlayerType
        {
            Human,
            Engine
        }

        ChessPlayer playerWhite;
        ChessPlayer playerBlack;

        ChessPlayer playerToMove => board.WhiteToMove ? playerWhite : playerBlack;

        Board board;
        AbstractMoveGenerator moveGenerator;

        bool isPlayerTurn;
        bool isWaitingToPlayMove;

        Move moveToPlay;

        BoardUI boardUI;

        public GameController(PlayerType whiteType, PlayerType blackType)
        {
            // Load the settings
            isPlayerTurn = Settings.IsPlayerWhite;
            isWaitingToPlayMove = !Settings.IsPlayerWhite;

            // Initialize game logic components
            board = new Board();
            board.LoadStartPosition();
            moveGenerator = Settings.MoveGenerationParallel ? new MoveGeneratorParallel() : new MoveGenerator();

            // Initialize UI components
            boardUI = new BoardUI();

            // Create the players
            playerWhite = CreatePlayer(whiteType);
            playerBlack = CreatePlayer(blackType);
            playerWhite.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);
            playerBlack.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);

            boardUI.UpdatePosition(board);
            boardUI.ResetSquareColours();

            SetPerspective();

            // Start the game
            NotifyTurnToMove();
        }

        ChessPlayer CreatePlayer(PlayerType type)
        {
            return type switch
            {
                PlayerType.Human => new ChessPlayer(new HumanPlayer(boardUI), type),
                PlayerType.Engine => new ChessPlayer(new EnginePlayer(board, OnMoveChosen), type),
                _ => new ChessPlayer(new HumanPlayer(boardUI), type)
            };
        }

        void SetPerspective()
        {
            bool perspective = true;
            if (playerWhite.IsHuman || playerBlack.IsHuman)
            {
                perspective = playerWhite.IsHuman;
            }

            if (Settings.InvertPerspective)
            {
                perspective = !perspective;
            }

            boardUI.SetPerspective(perspective);
        }

        void NotifyTurnToMove()
        {
            if (playerToMove.IsHuman)
            {
                playerToMove.Human.SetPosition(FenUtility.CurrentFen(board));
                playerToMove.Human.NotifyTurnToMove();
            }
            else
            {
                playerToMove.GetEngineMove();
            }
        }

        void OnMoveChosen(Move chosenMove)
        {
            if (IsLegal(chosenMove))
            {
                if (playerToMove.IsEngine)
                {
                    moveToPlay = chosenMove;
                    isWaitingToPlayMove = true;
                }
                else
                {
                    PlayMove(chosenMove);
                }
            }
            else
            {
                throw new Exception("Illegal move chosen");
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
            bool animate = playerToMove.IsEngine;

            board.MakeMove(move, false);
            boardUI.UpdatePosition(board, move, animate);

            GameResult result = Arbiter.GetGameState(board);
            if (result == GameResult.InProgress)
            {
                NotifyTurnToMove();
            }
            else
            {
                // TODO: Handle game over
            }
        }

        public void Update()
        {
            playerWhite.Update();
            playerBlack.Update();

            if (isWaitingToPlayMove)
            {
                isPlayerTurn = !isPlayerTurn;
                isWaitingToPlayMove = false;
                PlayMove(moveToPlay);
            }
        }

        public void Draw()
        {
            boardUI.Draw();
        }

        public void Release()
        {
            boardUI.Release();
        }
    }
}
