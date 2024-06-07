namespace BattleshipGame.Models
{
    public class GamePlayer
    {
        public string Id { get; set; }
        public GameBoard Board { get; set; }
        public List<Ship> Ships { get; set; }

        public GamePlayer(string id)
        {
            Id = id;
            Board = new GameBoard();
            Ships = new List<Ship>();
        }
    }

}
