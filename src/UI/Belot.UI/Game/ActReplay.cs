namespace Belot.UI.Game
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Turns one <see cref="BelotMatch.Act"/> into what happened at the table, in order. One
    /// decision can do a lot: after a bid the seats that can only pass pass; a card is followed by
    /// every card the rules force until somebody has a choice, possibly across tricks and to the end
    /// of the deal, which is then scored and the next one dealt. So this compares the person's view
    /// before and after the act (public information plus the person's hand) and lists every bid,
    /// declaration, card, trick and deal end between them. A finished deal's auction and tricks
    /// come from its summary (<see cref="BelotSeatView.PreviousRounds"/>).
    /// <para>No MAUI types here: the UI tests compile this file.</para>
    /// </summary>
    public static class ActReplay
    {
        public static IReadOnlyList<GameEvent> Steps(BelotSeatView before, BelotSeatView after, PlayerPosition actor, BelotAction action)
        {
            var steps = new List<GameEvent>();
            var dealEnded = after.PreviousRounds.Count > before.PreviousRounds.Count;
            var ended = dealEnded ? after.PreviousRounds[after.PreviousRounds.Count - 1] : null;

            // The auction: the actor's bid, then the passes of the seats that could only pass.
            var bids = ended?.Bids ?? after.Bids;
            var contractPlayer = before.Contract.Player;
            var contractType = before.Contract.Type;
            for (var i = before.Bids.Count; i < bids.Count; i++)
            {
                var bid = bids[i];
                if (bid.Type == BidType.Double || bid.Type == BidType.ReDouble)
                {
                    contractType = (contractType & ~(BidType.Double | BidType.ReDouble)) | bid.Type;
                }
                else if (bid.Type != BidType.Pass)
                {
                    contractPlayer = bid.Player;
                    contractType = bid.Type;
                }

                var isAuto = !(i == before.Bids.Count && action.Type == BelotActionType.Bid);
                steps.Add(new BidInfo(bid.Player, bid.Type, isAuto, contractPlayer, contractType));
            }

            if (before.Decision == BelotDecision.Bid && !dealEnded && after.Decision != BelotDecision.Bid)
            {
                steps.Add(new ContractInfo(after.Contract.Player, after.Contract.Type, after.Hand.ToArray(), after.CardCounts.ToArray()));
            }

            // The actor's declarations (the belotes travel with their cards).
            if (action.Type == BelotActionType.Announce)
            {
                var announces = ended?.Announces ?? after.Announces;
                var declared = announces.Skip(before.Announces.Count)
                    .Where(x => x.Type != AnnounceType.Belot && x.Player == actor)
                    .Select(ToCombination)
                    .ToArray();
                steps.Add(new AnnounceInfo(actor, declared));
            }

            // The cards: the actor's, then every forced one, with a trick after each fourth.
            var tricks = ended?.Tricks ?? after.Tricks;
            var played = (before.Tricks.Count * 4) + before.CurrentTrick.Count;
            var cards = tricks.SelectMany(x => x.Cards).Concat(ended == null ? after.CurrentTrick : Enumerable.Empty<BelotPlayedCard>()).ToList();
            for (var k = played; k < cards.Count; k++)
            {
                var card = cards[k];
                var isAuto = !(k == played && action.Type == BelotActionType.PlayCard);
                steps.Add(new CardInfo(card.Player, card.Card, card.Belote, (k / 4) + 1, k % 4, isAuto));
                if (k % 4 == 3)
                {
                    var trick = tricks[k / 4];
                    steps.Add(new TrickInfo(
                        (k / 4) + 1,
                        trick.Cards.Select(x => new PlayedCard(x.Player, x.Card, x.Belote)).ToArray(),
                        trick.Winner,
                        k / 4 == 7));
                }
            }

            if (ended != null)
            {
                steps.Add(EndOfDeal(before, after, ended));
            }

            return steps;
        }

        private static Combination ToCombination(BelotAnnounce announce) =>
            new(announce.Player, announce.Type, announce.Card, announce.IsScored);

        private static RoundEndInfo EndOfDeal(BelotSeatView before, BelotSeatView after, BelotRoundSummary summary)
        {
            var declarer = summary.Contract.Player;
            var contract = summary.Contract.Type;
            var outcome = RoundOutcome.PassedOut;
            if (contract != BidType.Pass)
            {
                var declarers = Seats.IsUs(declarer) ? summary.SouthNorthTotalInRoundPoints : summary.EastWestTotalInRoundPoints;
                var defenders = Seats.IsUs(declarer) ? summary.EastWestTotalInRoundPoints : summary.SouthNorthTotalInRoundPoints;
                outcome = declarers > defenders ? RoundOutcome.Made : declarers < defenders ? RoundOutcome.Inside : RoundOutcome.Hanging;
            }

            var combinations = summary.Announces.Select(ToCombination).ToArray();
            var scored = combinations.Where(x => x.IsScored == true).ToArray();
            var tricks = summary.Tricks ?? new List<BelotTrick>();
            return new RoundEndInfo(
                before.RoundNumber,
                declarer,
                contract,
                outcome,
                summary.SouthNorthTotalInRoundPoints,
                summary.EastWestTotalInRoundPoints,
                scored.Where(x => Seats.IsUs(x.Seat)).Sum(x => x.Value),
                scored.Where(x => !Seats.IsUs(x.Seat)).Sum(x => x.Value),
                tricks.Count(x => Seats.IsUs(x.Winner)),
                tricks.Count(x => !Seats.IsUs(x.Winner)),
                summary.NoTricksForOneOfTheTeams,
                summary.SouthNorthPoints,
                summary.EastWestPoints,
                after.SouthNorthPoints,
                after.EastWestPoints,
                before.HangingPoints,
                summary.HangingPoints,
                combinations,
                after.IsMatchFinished);
        }
    }
}
