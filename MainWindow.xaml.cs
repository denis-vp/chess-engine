using chess_engine.Core;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace chess_engine
{
    public partial class MainWindow : Window
    {
        private readonly Brush lightSquareBrush = new SolidColorBrush(Color.FromRgb(210, 180, 140));
        private readonly Brush darkSquareBrush = new SolidColorBrush(Color.FromRgb(139, 69, 19));
        private readonly Engine engine;

        public List<string> Files { get; } = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
        public List<string> Ranks { get; } = new List<string> { "8", "7", "6", "5", "4", "3", "2", "1" };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            engine = new Engine();
            engine.OnMoveMade += HandleEngineMove;

            engine.NewGame();
            PopulateChessBoard();
        }

        private void PopulateChessBoard()
        {
            var squares = new List<ChessSquare>();
            int[] boardSquares = engine.GetBoardSquares(); // Directly access the board's square array

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    int correctRow = 7 - row;
                    int squareIndex = correctRow * 8 + col;

                    var background = (row + col) % 2 == 0 ? lightSquareBrush : darkSquareBrush;
                    var piece = GetPieceImage(boardSquares[squareIndex]); // Use integer representation of pieces
                    squares.Add(new ChessSquare { Background = background, Piece = piece });
                }
            }

            ChessBoard.ItemsSource = squares;
        }

        private BitmapImage GetPieceImage(int piece)
        {
            string pieceName = null;

            if (piece != Piece.None)
            {
                bool isWhite = Piece.IsWhite(piece);
                int pieceType = Piece.PieceType(piece);

                pieceName = pieceType switch
                {
                    Piece.Pawn => "pawn",
                    Piece.Knight => "knight",
                    Piece.Bishop => "bishop",
                    Piece.Rook => "rook",
                    Piece.Queen => "queen",
                    Piece.King => "king",
                    _ => null
                };

                if (pieceName != null)
                {
                    pieceName = (isWhite ? "white" : "black") + "-" + pieceName;
                    return new BitmapImage(new Uri($"pack://application:,,,/chess-engine;component/Assets/Pieces/{pieceName}.png"));
                }
            }

            return null;
        }

        private void HandleEngineMove(string move)
        {
            // Handle the move received from the engine
            engine.MakeMove(move);
            PopulateChessBoard();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width != e.NewSize.Height)
            {
                double newSize = Math.Min(e.NewSize.Width, e.NewSize.Height);
                this.Width = newSize;
                this.Height = newSize;
            }
        }
    }

    public class ChessSquare
    {
        public Brush Background { get; set; }
        public BitmapImage Piece { get; set; }
    }
}
