namespace Belot.AI.ClaudePlayer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Heuristic;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// A player of rules, no networks: the advice of the belot.bg academy and the other sources
    /// collected in HEURISTIC_PLAYER.md, for the bids (<see cref="LearnedBidding"/>: a point count
    /// fitted to how every bid did once the deal was over; <see cref="HeuristicBidding"/> where it
    /// has no answer) and the card play (<see cref="HeuristicCardPlay"/>) over what a careful
    /// player remembers of the cards (<see cref="CardMemory"/>). Optionally the last
    /// <see cref="HeuristicSettings.EndgameTricks"/> tricks are played out exactly over the deals
    /// the play allows, the rules choosing among the cards that do equally well. It keeps no state
    /// between decisions, so it decides the same from a <see cref="BelotSeatView"/>.
    /// </summary>
    public class ClaudePlayerHeuristic : IPlayer
    {
        private readonly CardMemory memory = new CardMemory();
        private readonly HeuristicCardPlay play;
        private readonly EndgameSearch endgame = new EndgameSearch();
        private readonly float[] values = new float[32];

        public ClaudePlayerHeuristic()
            : this(new HeuristicSettings())
        {
        }

        public ClaudePlayerHeuristic(HeuristicSettings settings)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.play = new HeuristicCardPlay(this.Settings);
        }

        /// <summary>Gets the rules' settings; changes take effect at the next decision.</summary>
        public HeuristicSettings Settings { get; }

        /// <summary>Gets or sets the random numbers for sampling endgame deals (the rules use none).</summary>
        public Random Rng { get; set; } = new Random();

        /// <summary>Gets the card decisions whose context did not add up (the cheapest card was played).</summary>
        public long Inconsistent { get; private set; }

        /// <summary>Gets the card decisions the endgame search decided.</summary>
        public long EndgameDecisions { get; private set; }

        /// <summary>Gets the card decisions where the endgame changed the rules' card.</summary>
        public long EndgameChanges { get; private set; }

        /// <summary>Gets the rule that chose the last card (for the trainer's diagnostics).</summary>
        internal string LastRule { get; private set; }

        public BidType GetBid(PlayerGetBidContext context) =>
            this.Settings.Bids != null ? LearnedBidding.Choose(context, this.Settings, this.Settings.Bids) : HeuristicBidding.Choose(context, this.Settings);

        /// <summary>Declares every combination offered, carres first, as the other bots do.</summary>
        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var legal = context.AvailableCardsToPlay;
            if (legal.Count == 1)
            {
                this.LastRule = "forced";
                foreach (var only in legal)
                {
                    return new PlayCardAction(only);
                }
            }

            this.memory.BidHonourWeight = this.Settings.BidHonourWeight;
            this.memory.DiscardHonourWeight = this.Settings.DiscardHonourWeight;
            if (!this.memory.Build(context))
            {
                this.LastRule = "inconsistent";
                this.Inconsistent++;
                return new PlayCardAction(legal.Lowest(card => (card.GetValue(context.CurrentContract.Type) * 10) + card.TrumpOrder));
            }

            var card = this.play.Choose(this.memory);
            this.LastRule = this.play.Rule;
            if (this.Settings.EndgameTricks >= 2 && this.memory.TricksPlayed >= 8 - this.Settings.EndgameTricks)
            {
                card = this.Endgame(context, card);
            }

            return new PlayCardAction(Card.AllCards[card]);
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

        // The exact endgame's best card; among those within the tolerance, the one the rules prefer.
        private int Endgame(PlayerPlayCardContext context, int ruled)
        {
            var settings = this.Settings;
            var search = this.endgame;
            search.Tricks = Math.Min(5, settings.EndgameTricks);
            search.UseDeclarations = true;
            search.UseTranspositions = true;
            search.RawTieBreak = true;
            search.ThreeTrickWorldLimit = 1680;
            search.SampledWorlds = settings.EndgameWorlds;
            search.NodeLimit = settings.EndgameNodeLimit;
            search.TimeLimitMilliseconds = settings.EndgameMilliseconds;
            search.PartnerSignalWeight = settings.EndgameSignalWeight;
            search.BidderHonourWeight = settings.EndgameBidWeight;
            var simulator = this.memory.Simulator;
            var legal = this.memory.Legal;
            if (!NeuralDeal.FromPlayContext(context, simulator, out var deal))
            {
                return ruled;
            }

            Array.Clear(this.values);
            if (!search.Evaluate(context, in deal, legal, simulator, this.values, this.Rng))
            {
                return ruled;
            }

            this.EndgameDecisions++;
            var best = float.NegativeInfinity;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                best = Math.Max(best, this.values[BitOperations.TrailingZeroCount(rest)]);
            }

            var limit = best - settings.EndgameTolerance;
            if (this.values[ruled] >= limit)
            {
                return ruled;
            }

            var good = 0u;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                if (this.values[card] >= limit)
                {
                    good |= 1u << card;
                }
            }

            this.EndgameChanges++;
            this.memory.Restrict(good);
            this.LastRule = "endgame";
            return this.play.Choose(this.memory);
        }
    }
}
