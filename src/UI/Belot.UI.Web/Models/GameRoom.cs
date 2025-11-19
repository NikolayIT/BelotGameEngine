namespace Belot.UI.Web.Models;

using Belot.Engine;
using Belot.Engine.Players;

public class GameRoom
{
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public Dictionary<PlayerPosition, PlayerInfo?> Players { get; set; } = new();
    public GameRoomState State { get; set; } = GameRoomState.Waiting;
    public IBelotGame? Game { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public GameRoom()
    {
        Players[PlayerPosition.South] = null;
        Players[PlayerPosition.East] = null;
        Players[PlayerPosition.North] = null;
        Players[PlayerPosition.West] = null;
    }
    
    public bool IsFull() => Players.Values.All(p => p != null);
    
    public int PlayerCount() => Players.Values.Count(p => p != null);
    
    public PlayerPosition? GetAvailablePosition()
    {
        return Players.FirstOrDefault(p => p.Value == null).Key;
    }
    
    public bool AddPlayer(string connectionId, string playerName, out PlayerPosition position)
    {
        var availablePosition = GetAvailablePosition();
        if (availablePosition == null)
        {
            position = default;
            return false;
        }
        
        position = availablePosition.Value;
        Players[position] = new PlayerInfo
        {
            ConnectionId = connectionId,
            Name = playerName,
            Position = position
        };
        
        return true;
    }
    
    public bool RemovePlayer(string connectionId)
    {
        var player = Players.FirstOrDefault(p => p.Value?.ConnectionId == connectionId);
        if (player.Value != null)
        {
            Players[player.Key] = null;
            return true;
        }
        return false;
    }
}

public enum GameRoomState
{
    Waiting,
    InProgress,
    Completed
}
