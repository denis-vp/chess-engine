using chess_engine.Engine;

namespace chess_engine.Game
{
    public class EnginePlayer
    {
        Board board;
        Searcher searcher;
        OpeningBook openingBook;
        Action<Move> onMoveChosen;

        public EnginePlayer(Board board, Action<Move> onMoveChosen)
        {
            this.board = board;
            searcher = new Searcher(board);
            searcher.OnSearchComplete += onMoveChosen;
            this.onMoveChosen = onMoveChosen;
            openingBook = new OpeningBook(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.OpeningBookPath)));
        }

        public void GetMove()
        {
            if (board.PlyCount <= Settings.MaxBookPly && openingBook.TryGetBookMove(board, out string moveString))
            {
                // If we can, play the move from the opening book
                Move bookMove = MoveUtility.GetMoveFromUCIName(moveString, board);
                onMoveChosen(bookMove);
                return;
            }
            // Otherwise, start the search on a separate thread to avoid blocking the UI
            Task.Run(() => searcher.StartSearch());
            // Also, set a timer to cancel the search after a certain amount of time
            Task.Delay(Settings.BotThinkTimeSeconds * 1000).ContinueWith(_ => searcher.EndSearch());
        }
    }
}
