using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace chess_engine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Brush lightSquareBrush = new SolidColorBrush(Color.FromRgb(210, 180, 140));
        private readonly Brush darkSquareBrush = new SolidColorBrush(Color.FromRgb(139, 69, 19));

        public MainWindow()
        {
            InitializeComponent();
            PopulateChessBoard();
        }

        private void PopulateChessBoard()
        {
            var squares = new List<ChessSquare>();
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    var background = (row + col) % 2 == 0 ? lightSquareBrush : darkSquareBrush;
                    var piece = GetPieceImage(row, col);
                    squares.Add(new ChessSquare { Background = background, Piece = piece });
                }
            }
            ChessBoard.ItemsSource = squares;
        }

        private BitmapImage GetPieceImage(int row, int col)
        {
            string pieceName = null;

            // Define the default positions of the pieces
            if (row == 0 || row == 7)
            {
                bool isWhite = row == 7;
                switch (col)
                {
                    case 0:
                    case 7:
                        pieceName = "rook";
                        break;
                    case 1:
                    case 6:
                        pieceName = "knight";
                        break;
                    case 2:
                    case 5:
                        pieceName = "bishop";
                        break;
                    case 3:
                        pieceName = isWhite ? "queen" : "king";
                        break;
                    case 4:
                        pieceName = isWhite ? "king" : "queen";
                        break;
                }
                pieceName = (isWhite ? "white" : "black") + "-" + pieceName;
            }
            else if (row == 1 || row == 6)
            {
                pieceName = (row == 6 ? "white" : "black") + "-pawn";
            }

            if (pieceName != null)
            {
                return new BitmapImage(new Uri($"pack://application:,,,/chess-engine;component/assets/pieces/{pieceName}.png"));
            }

            return null;
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