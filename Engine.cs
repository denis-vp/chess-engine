namespace chess_engine
{
    public class Engine
    {
        private readonly Bot bot;

        // Event to notify the view (MainWindow) of bot's move
        public event Action<string>? OnMoveMade;

        public Engine()
        {
            bot = new Bot();
            bot.OnMoveChosen += HandleMoveChosen;
        }

        // Starts a new game
        public void NewGame()
        {
            bot.NotifyNewGame();
        }

        // Sets up the board with a custom FEN position
        public void SetPosition(string fen)
        {
            bot.SetPosition(fen);
        }

        // Handles making a move in UCI format
        public void MakeMove(string move)
        {
            bot.MakeMove(move);
        }

        // Starts the bot's thinking process for a move
        public void StartThinking(int timeMs)
        {
            bot.ThinkTimed(timeMs);
        }

        // Stops the bot from thinking (useful for time-limited moves or aborting a move)
        public void StopThinking()
        {
            bot.StopThinking();
        }

        // Fetches the current board state as a string
        public string GetBoardDiagram()
        {
            return bot.GetBoardDiagram();
        }

        // Graceful quit for the bot's internal threads
        public void Quit()
        {
            bot.Quit();
        }

        // Handles the bot's move event and forwards it to the view
        private void HandleMoveChosen(string move)
        {
            OnMoveMade?.Invoke(move);
        }

        public int[] GetBoardSquares()
        {
            return bot.GetBoardSquares();
        }
    }
}
