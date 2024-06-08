namespace BattleshipGame.Models
{
    public class GameBoard
    {
        public const int BoardSize = 10;
        public CellState[,] Cells { get; set; }


        public GameBoard()
        {
            Cells = new CellState[BoardSize, BoardSize];
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    Cells[i, j] = CellState.Empty;
                }
            }
        }

        public bool IsHit(int x, int y)
        {
            if (Cells[x, y] == CellState.Ship)
            {
                Cells[x, y] = CellState.Hit;
                return true;
            }

            Cells[x, y] = CellState.Miss;
            return false;
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
