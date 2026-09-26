namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    // Random legal decisions from a seat's own view: what a person might tap. Mostly passes (so
    // deals get played), any combination set without a shared card, and sometimes no belote claim.
    internal static class RandomMoves
    {
        private static readonly BidType[] Bids =
        {
            BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades,
            BidType.NoTrumps, BidType.AllTrumps, BidType.Double, BidType.ReDouble,
        };

        private static readonly ValidAnnouncesService AnnouncesService = new();

        public static BelotAction Choose(BelotSeatView view, Random random) => view.Decision switch
        {
            BelotDecision.Bid => BelotAction.Bid(ChooseBid(view, random)),
            BelotDecision.Announce => BelotAction.Declare(ChooseAnnounces(view, random).Select(a => new Announce(a.Type, a.Card))),
            _ => BelotAction.PlayCard(view.PlayableCards[random.Next(view.PlayableCards.Count)], random.Next(4) != 0),
        };

        public static BidType ChooseBid(BelotSeatView view, Random random)
        {
            var options = Bids.Where(b => view.AvailableBids.HasFlag(b)).ToList();
            return options.Count == 0 || random.Next(3) != 0 ? BidType.Pass : options[random.Next(options.Count)];
        }

        public static IReadOnlyList<BelotAnnounce> ChooseAnnounces(BelotSeatView view, Random random)
        {
            var chosen = new List<BelotAnnounce>();
            foreach (var offered in view.AvailableAnnounces)
            {
                if (random.Next(3) != 0 && chosen.All(c => !Conflict(c, offered)))
                {
                    chosen.Add(offered);
                }
            }

            return chosen;
        }

        public static bool Conflict(BelotAnnounce first, BelotAnnounce second) =>
            AnnouncesService.HaveCommonCards(new Announce(first.Type, first.Card), new Announce(second.Type, second.Card));
    }
}
