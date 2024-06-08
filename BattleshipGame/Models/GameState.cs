namespace BattleshipGame.Models
{
    public class GameState
    {
        public int Id { get; set; }
        public string PlayerId { get; set; }
        public string PlayerBoard { get; set; } // JSON representation of the player's game board
        public string TrackingBoard { get; set; } // JSON representation of the tracking board
        public string CurrentTurnPlayerId { get; set; }

    }
}
