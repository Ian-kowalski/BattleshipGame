namespace BattleshipGame.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

public class GameHub : Hub
{
    private static Dictionary<string, List<(int x, int y)>> playerShips = new Dictionary<string, List<(int x, int y)>>();
    private static Dictionary<string, List<(int x, int y)>> playerMoves = new Dictionary<string, List<(int x, int y)>>();

    public override Task OnConnectedAsync()
    {
        // Initialize player data
        var playerId = Context.ConnectionId;
        if (!playerShips.ContainsKey(playerId))
        {
            playerShips[playerId] = new List<(int x, int y)>();
            playerMoves[playerId] = new List<(int x, int y)>();
        }
        return base.OnConnectedAsync();
    }

    public async Task SendMove(string user, int x, int y)
    {
        try
        {
            var playerId = Context.ConnectionId;
            var opponentId = GetOpponentId(playerId);

            if (opponentId != null)
            {
                playerMoves[playerId].Add((x, y));
                bool isHit = playerShips[opponentId].Contains((x, y));

                await Clients.Client(playerId).SendAsync("ReceiveMove", user, x, y, isHit ? "hit" : "miss");
                await Clients.Client(opponentId).SendAsync("ReceiveMove", user, x, y, isHit ? "hit" : "miss");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendMove: {ex.Message}");
            throw;
        }
    }

    public async Task PlaceShip(int x, int y)
    {
        var playerId = Context.ConnectionId;
        playerShips[playerId].Add((x, y));
        await Clients.Client(playerId).SendAsync("ShipPlaced", x, y);
    }

    private string GetOpponentId(string playerId)
    {
        // Simple example to get the opponent ID
        foreach (var id in playerShips.Keys)
        {
            if (id != playerId)
            {
                return id;
            }
        }
        return null;
    }
}

