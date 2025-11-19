namespace Belot.UI.Web.Services;

using Belot.Engine;
using Belot.Engine.Game;
using Belot.Engine.GameMechanics;
using Belot.Engine.Players;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class SignalRPlayer : IPlayer
{
    private readonly string _connectionId;
    private readonly string _name;
    private TaskCompletionSource<BidType>? _bidTcs;
    private TaskCompletionSource<IList<Announce>>? _announceTcs;
    private TaskCompletionSource<PlayCardAction>? _playCardTcs;

    public string ConnectionId => _connectionId;
    public string Name => _name;
    
    public event Action<PlayerGetBidContext>? OnGetBidRequest;
    public event Action<PlayerGetAnnouncesContext>? OnGetAnnouncesRequest;
    public event Action<PlayerPlayCardContext>? OnPlayCardRequest;
    public event Action<IEnumerable<PlayCardAction>>? OnEndOfTrick;
    public event Action<RoundResult>? OnEndOfRound;
    public event Action<GameResult>? OnEndOfGame;

    public SignalRPlayer(string connectionId, string name)
    {
        _connectionId = connectionId;
        _name = name;
    }

    public BidType GetBid(PlayerGetBidContext context)
    {
        _bidTcs = new TaskCompletionSource<BidType>();
        OnGetBidRequest?.Invoke(context);
        
        // Wait for the bid to be set via SetBid method
        return _bidTcs.Task.Result;
    }

    public void SetBid(BidType bid)
    {
        _bidTcs?.TrySetResult(bid);
    }

    public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
    {
        _announceTcs = new TaskCompletionSource<IList<Announce>>();
        OnGetAnnouncesRequest?.Invoke(context);
        
        // Wait for announces to be set via SetAnnounces method
        return _announceTcs.Task.Result;
    }

    public void SetAnnounces(IList<Announce> announces)
    {
        _announceTcs?.TrySetResult(announces);
    }

    public PlayCardAction PlayCard(PlayerPlayCardContext context)
    {
        _playCardTcs = new TaskCompletionSource<PlayCardAction>();
        OnPlayCardRequest?.Invoke(context);
        
        // Wait for the card to be played via SetPlayCard method
        return _playCardTcs.Task.Result;
    }

    public void SetPlayCard(PlayCardAction action)
    {
        _playCardTcs?.TrySetResult(action);
    }

    public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
    {
        OnEndOfTrick?.Invoke(trickActions);
    }

    public void EndOfRound(RoundResult roundResult)
    {
        OnEndOfRound?.Invoke(roundResult);
    }

    public void EndOfGame(GameResult gameResult)
    {
        OnEndOfGame?.Invoke(gameResult);
    }
}
