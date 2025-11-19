namespace Belot.UI.Web.Services;

using System.Collections.Concurrent;
using Belot.UI.Web.Models;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    
    public GameRoom CreateRoom(string roomName)
    {
        var roomId = Guid.NewGuid().ToString();
        var room = new GameRoom
        {
            RoomId = roomId,
            RoomName = roomName
        };
        
        _rooms.TryAdd(roomId, room);
        return room;
    }
    
    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }
    
    public IEnumerable<GameRoom> GetAvailableRooms()
    {
        return _rooms.Values.Where(r => r.State == GameRoomState.Waiting && !r.IsFull());
    }
    
    public IEnumerable<GameRoom> GetAllRooms()
    {
        return _rooms.Values;
    }
    
    public bool RemoveRoom(string roomId)
    {
        return _rooms.TryRemove(roomId, out _);
    }
    
    public GameRoom? FindRoomByConnectionId(string connectionId)
    {
        return _rooms.Values.FirstOrDefault(r => 
            r.Players.Values.Any(p => p?.ConnectionId == connectionId));
    }
}
