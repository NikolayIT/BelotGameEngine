namespace Belot.UI.Web.Hubs;

using Belot.Engine;
using Belot.Engine.Cards;
using Belot.Engine.Game;
using Belot.Engine.Players;
using Belot.UI.Web.Models;
using Belot.UI.Web.Services;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class GameHub : Hub
{
    private readonly RoomManager _roomManager;

    public GameHub(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = _roomManager.FindRoomByConnectionId(Context.ConnectionId);
        if (room != null)
        {
            room.RemovePlayer(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.RoomId);
            await Clients.Group(room.RoomId).SendAsync("PlayerLeft", Context.ConnectionId);
            
            // If room is empty, remove it
            if (room.PlayerCount() == 0)
            {
                _roomManager.RemoveRoom(room.RoomId);
            }
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object> CreateRoom(string roomName, string playerName)
    {
        var room = _roomManager.CreateRoom(roomName);
        
        if (room.AddPlayer(Context.ConnectionId, playerName, out var position))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
            await Clients.Group(room.RoomId).SendAsync("RoomCreated", new
            {
                roomId = room.RoomId,
                roomName = room.RoomName
            });
            
            return new { success = true, roomId = room.RoomId, position };
        }
        
        return new { success = false, message = "Failed to add player to room" };
    }

    public async Task<object> JoinRoom(string roomId, string playerName)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null)
        {
            return new { success = false, message = "Room not found" };
        }
        
        if (room.State != GameRoomState.Waiting)
        {
            return new { success = false, message = "Game already in progress" };
        }
        
        if (room.AddPlayer(Context.ConnectionId, playerName, out var position))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
            
            var playerInfo = new
            {
                connectionId = Context.ConnectionId,
                name = playerName,
                position
            };
            
            await Clients.Group(room.RoomId).SendAsync("PlayerJoined", playerInfo);
            
            // If room is full, start the game
            if (room.IsFull())
            {
                await StartGame(room);
            }
            
            return new { success = true, roomId = room.RoomId, position };
        }
        
        return new { success = false, message = "Room is full" };
    }

    public async Task<IEnumerable<object>> GetAvailableRooms()
    {
        var rooms = _roomManager.GetAvailableRooms();
        return rooms.Select(r => new
        {
            roomId = r.RoomId,
            roomName = r.RoomName,
            playerCount = r.PlayerCount(),
            maxPlayers = 4
        });
    }

    public async Task LeaveRoom()
    {
        var room = _roomManager.FindRoomByConnectionId(Context.ConnectionId);
        if (room != null)
        {
            room.RemovePlayer(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.RoomId);
            await Clients.Group(room.RoomId).SendAsync("PlayerLeft", Context.ConnectionId);
        }
    }

    public async Task SendBid(string roomId, string bidType)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;
        
        // Find the player and set their bid
        var playerEntry = room.Players.FirstOrDefault(p => p.Value?.ConnectionId == Context.ConnectionId);
        if (playerEntry.Value != null)
        {
            // This would be handled by the SignalRPlayer when game is running
            await Clients.Group(room.RoomId).SendAsync("BidReceived", new
            {
                position = playerEntry.Key,
                bidType
            });
        }
    }

    public async Task PlayCard(string roomId, string cardSuit, string cardType)
    {
        var room = _roomManager.GetRoom(roomId);
        if (room == null) return;
        
        var playerEntry = room.Players.FirstOrDefault(p => p.Value?.ConnectionId == Context.ConnectionId);
        if (playerEntry.Value != null)
        {
            await Clients.Group(room.RoomId).SendAsync("CardPlayed", new
            {
                position = playerEntry.Key,
                cardSuit,
                cardType
            });
        }
    }

    private async Task StartGame(GameRoom room)
    {
        room.State = GameRoomState.InProgress;
        await Clients.Group(room.RoomId).SendAsync("GameStarting", new
        {
            players = room.Players.Select(p => new
            {
                position = p.Key,
                name = p.Value?.Name
            })
        });
        
        // Game logic will be implemented here
        // For now, just notify that game is starting
    }
}
