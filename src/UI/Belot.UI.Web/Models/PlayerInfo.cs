namespace Belot.UI.Web.Models;

using Belot.Engine.Players;

public class PlayerInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PlayerPosition Position { get; set; }
}
