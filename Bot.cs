using chess_engine.Core;
using chess_engine.Helper;

namespace chess_engine
{
    public class Bot
    {
        // Public stuff
        public event Action<string>? OnMoveChosen;
        public bool IsThinking { get; private set; }

        // References
        readonly Board board;
        CancellationTokenSource? cancelSearchTimer;

        // State
        int currentSearchID;
        bool isQuitting;

        public Bot()
        {
            board = Board.CreateBoard();
        }

        public void NotifyNewGame()
        {
            board.LoadStartPosition();
        }

        public void SetPosition(string fen)
        {
            board.LoadPosition(fen);
        }

        public void MakeMove(string moveString)
        {
            Move move = MoveUtility.GetMoveFromUCIName(moveString, board);
            board.MakeMove(move);
        }

        public void ThinkTimed(int timeMs)
        {
            IsThinking = true;
            cancelSearchTimer?.Cancel();

            StartSearch(timeMs);
        }

        void StartSearch(int timeMs)
        {
            currentSearchID++;
            cancelSearchTimer = new CancellationTokenSource();
            Task.Delay(timeMs, cancelSearchTimer.Token).ContinueWith((t) => EndSearch(currentSearchID));
        }

        public void StopThinking()
        {
            EndSearch();
        }

        public void Quit()
        {
            isQuitting = true;
            EndSearch();
        }

        public string GetBoardDiagram() => board.ToString();

        void EndSearch()
        {
            cancelSearchTimer?.Cancel();
            IsThinking = false;
        }

        void EndSearch(int searchID)
        {
            if (cancelSearchTimer != null && cancelSearchTimer.IsCancellationRequested)
            {
                return;
            }

            if (currentSearchID == searchID)
            {
                EndSearch();
            }
        }

        public int[] GetBoardSquares()
        {
            return board.Square;
        }
    }
}
