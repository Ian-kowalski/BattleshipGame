namespace BattleshipGame.Models
{
    public class GameBoard
    {
        public const int BoardSize = 10;
        public CellState[,] Cells { get; set; }

        public GameBoard()
        {
            Cells = new CellState[BoardSize, BoardSize];
            // Initialize all cells as empty
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    Cells[i, j] = CellState.Empty;
                }
            }
        }
    }

    public enum CellState
    {
        Empty,
        Ship,
        Hit,
        Miss
    }

}