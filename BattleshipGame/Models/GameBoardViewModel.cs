namespace BattleshipGame.Models;

public class GameBoardViewModel
{
    public int BoardSize { get; set; }
    public CellState[,] PlayerBoard { get; set; }
    public CellState[,] TrackingBoard { get; set; }
    public string CurrentTurnPlayerId { get; set; }

    public GameBoardViewModel(GameBoard playerBoard, GameBoard trackingBoard, string currentTurnPlayerId)
    {
        BoardSize = GameBoard.BoardSize;
        PlayerBoard = playerBoard.Cells;
        TrackingBoard = trackingBoard.Cells;
        CurrentTurnPlayerId = currentTurnPlayerId;
    }

    public GameBoardViewModel()
    {
        BoardSize = GameBoard.BoardSize;
        PlayerBoard = new CellState[BoardSize, BoardSize];
        TrackingBoard = new CellState[BoardSize, BoardSize];
    }
}
