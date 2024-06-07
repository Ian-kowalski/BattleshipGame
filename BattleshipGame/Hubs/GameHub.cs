using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

public class GameHub : Hub
{
    public async Task SendMove(string user, int x, int y)
    {
        try
        {
            // Add your game logic here
            // For example, broadcasting the move to all clients
            await Clients.All.SendAsync("ReceiveMove", user, x, y);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendMove: {ex.Message}");
            throw;
        }
    }

    public async Task SendMessage(string user, string message)
    {
        try
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendMessage: {ex.Message}");
            throw;
        }
    }
}
