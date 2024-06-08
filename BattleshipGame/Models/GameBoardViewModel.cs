namespace BattleshipGame.Models
{
    public class GameBoardViewModel
    {
        public int BoardSize { get; set; }
        public CellState[,] PlayerBoard { get; set; }
        public CellState[,] TrackingBoard { get; set; }

        public GameBoardViewModel(GameBoard playerBoard, GameBoard trackingBoard)
        {
            BoardSize = GameBoard.BoardSize;
            PlayerBoard = playerBoard.Cells;
            TrackingBoard = trackingBoard.Cells;
        }
    }
}
