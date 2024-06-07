namespace BattleshipGame.Models
{
    public class GameBoardViewModel
    {
        public int BoardSize { get; set; }
        public CellState[,] Cells { get; set; }

        public GameBoardViewModel(GameBoard gameBoard)
        {
            BoardSize = GameBoard.BoardSize;
            Cells = gameBoard.Cells;
        }
    }
}
