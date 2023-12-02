using Microsoft.AspNetCore.SignalR;

namespace WebGamesAPI.PingPong;

public class PingPongHub : Hub
{
    public static List<Game> Games { get; private set; } = [];

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ReceiveMessage", "Server", "Welcome to the Ping Pong Hub!");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var game = Games.FirstOrDefault(g => g.Host?.ConnectionId == Context.ConnectionId || g.Guest?.ConnectionId == Context.ConnectionId);
        if (game is not null)
        {
            Games.Remove(game);
            await Clients.Group(game.Host!.ConnectionId).SendAsync("GameEnded");
            if (game.Guest is not null)
            {
                await Clients.Group(game.Guest.ConnectionId).SendAsync("GameEnded");
            }
        }
    }

    public async Task CreateGame()
    {
        var host = new PlayerConnection { ConnectionId = Context.ConnectionId };
        var game = new Game
        {
            Host = host,
            Guest = null
        };
        Games.Add(game);
        await Clients.Caller.SendAsync("GameCreated");
    }

    public async Task JoinGame()
    {
        var game = Games.FirstOrDefault(g => g.Host is not null && g.Guest is null);
        if (game is null)
        {
            await Clients.Caller.SendAsync("NoGameAvailable");
            return;
        }
        var guest = new PlayerConnection { ConnectionId = Context.ConnectionId };
        game.Guest = guest;
        await Clients.Caller.SendAsync("GameStarted");
        await Clients.Client(game.Host!.ConnectionId).SendAsync("GameStarted");
    }

    public async Task SendRacketPosition(int y)
    {
        var game = Games.FirstOrDefault(g => g.Host!.ConnectionId == Context.ConnectionId || g.Guest?.ConnectionId == Context.ConnectionId);
        if (game is null)
        {
            return;
        }
        if (game.Host!.ConnectionId == Context.ConnectionId)
        {
            await Clients.Client(game.Guest!.ConnectionId).SendAsync("ReceiveRacketPosition", y);
        }
        else
        {
            await Clients.Client(game.Host.ConnectionId).SendAsync("ReceiveRacketPosition", y);
        }
    }
}
