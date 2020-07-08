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
            // Group by suit
            var cardsBySuit = new[] { new List<Card>(8), new List<Card>(8), new List<Card>(8), new List<Card>(8) };
            foreach (var card in context.MyCards)
            {
                cardsBySuit[(int)card.Suit].Add(card);
            }

            if (context.MyCards.Count(x => x.Type == CardType.Jack) >= 3
                && context.AvailableBids.HasFlag(BidType.AllTrumps))
            {
                return BidType.AllTrumps;
            }

            if (context.MyCards.Count(x => x.Type == CardType.Ace) == 4
                && context.AvailableBids.HasFlag(BidType.NoTrumps))
            {
                return BidType.NoTrumps;
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
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.GetValue(context.CurrentContract.Type)).FirstOrDefault());
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
