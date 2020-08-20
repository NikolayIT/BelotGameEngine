namespace Belot.AI.DummyPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class DummyPlayer : IPlayer
    {
        public BidType GetBid(PlayerGetBidContext context)
        {
            if (context.MyCards.Count(x => x.Type == CardType.Ace) == 4
                && context.AvailableBids.HasFlag(BidType.NoTrumps))
            {
                return BidType.NoTrumps;
            }

            if (context.MyCards.Count(x => x.Type == CardType.Jack) >= 3
                && context.AvailableBids.HasFlag(BidType.AllTrumps))
            {
                return BidType.AllTrumps;
            }

            // Group by suit
            var cardsBySuit = new[] { new List<Card>(8), new List<Card>(8), new List<Card>(8), new List<Card>(8) };
            foreach (var card in context.MyCards)
            {
                cardsBySuit[(int)card.Suit].Add(card);
            }

            foreach (var cards in cardsBySuit)
            {
                if (cards.Count >= 4 && context.AvailableBids.HasFlag(cards.First().Suit.ToBidType()))
                {
                    return cards.First().Suit.ToBidType();
                }
            }

            return BidType.Pass;
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            if (context.CurrentContract.Type.HasFlag(BidType.AllTrumps))
            {
                return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.TrumpOrder));
            }

            if (context.CurrentContract.Type.HasFlag(BidType.NoTrumps))
            {
                return new PlayCardAction(context.AvailableCardsToPlay.Lowest(x => x.NoTrumpOrder));
            }

            var trumpSuit = context.CurrentContract.Type.ToCardSuit();
            return new PlayCardAction(
                context.AvailableCardsToPlay.Lowest(x => x.Suit == trumpSuit ? (x.TrumpOrder + 8) : x.NoTrumpOrder));
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }
    }
}
