namespace Belot.Engine.GameMechanics
{
    using Belot.Engine.Players;

    /// <summary>
    /// Plays the step-by-step machines (<see cref="Auction"/>, <see cref="TrickPlay"/>,
    /// <see cref="Round"/>) with <see cref="IPlayer"/>s: asks the seat to move for its decision,
    /// with the live context, and applies it. A bot's illegal answer is a bug, not a game
    /// situation, so it throws <see cref="BelotGameException"/>.
    /// </summary>
    internal static class PlayerDriver
    {
        public static void Bid(IPlayer[] players, Auction auction)
        {
            var seat = auction.ToMove;
            var bid = players[seat.Index()].GetBid(auction.Context);
            switch (auction.Check(bid))
            {
                case BidCheck.MoreThanOneFlag:
                    throw new BelotGameException($"Invalid bid from {seat} player. More than 1 flags returned.");
                case BidCheck.NotPermitted:
                    throw new BelotGameException($"Invalid bid from {seat} player. This bid is not permitted.");
            }

            auction.Apply(bid);
        }

        public static void Announce(IPlayer[] players, TrickPlay tricks)
        {
            tricks.ApplyAnnounces(players[tricks.ToMove.Index()].GetAnnounces(tricks.AnnounceContext));
        }

        public static void PlayCard(IPlayer[] players, TrickPlay tricks)
        {
            var seat = tricks.ToMove;
            if (!tricks.TryPlayCard(players[seat.Index()].PlayCard(tricks.PlayContext)))
            {
                throw new BelotGameException($"Invalid card played from {seat} player.");
            }
        }

        /// <summary>Plays the round to its end.</summary>
        public static void Play(IPlayer[] players, Round round)
        {
            while (!round.IsFinished)
            {
                Step(players, round);
            }
        }

        /// <summary>Makes the round's next decision.</summary>
        public static void Step(IPlayer[] players, Round round)
        {
            if (round.Tricks == null)
            {
                Bid(players, round.Auction);
                round.AfterBid();
                return;
            }

            if (round.Decision == BelotDecision.Announce)
            {
                Announce(players, round.Tricks);
            }
            else
            {
                PlayCard(players, round.Tricks);
            }

            round.AfterPlay();
        }
    }
}
