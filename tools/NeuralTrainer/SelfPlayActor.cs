namespace Belot.NeuralTrainer
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Human;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;

    /// <summary>
    /// Plays deals in self-play (<see cref="NeuralDeal"/>, the engine's rules on card masks) and
    /// turns decisions into training samples. At a labelled decision every action open to the
    /// seat is tried: the deal is copied, the action taken, and the rest of the deal played out
    /// by every seat's networks (each deciding from what it can see) in this very deal, so the
    /// label of an action is the game points its team then gets minus the other team's. All the
    /// actions of a decision share the deal, so the luck of the cards cancels out of their
    /// differences, and the networks learn the value of each action under the current play:
    /// Monte Carlo policy iteration. The deal itself goes on with the best action (or, now and
    /// then, a random one, to see other positions).
    /// </summary>
    internal sealed class SelfPlayActor
    {
        private readonly TrainingSettings settings;
        private readonly Random random;
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly DeclaredAnnounce[] announces = new DeclaredAnnounce[AnnounceScorer.MaxAnnounces];
        private readonly NeuralEvaluator[] seats = new NeuralEvaluator[4];
        private readonly int[] deck = new int[32];
        private readonly float[] values = new float[FeatureEncoder.CardOutputs];
        private readonly float[] rolloutValues = new float[FeatureEncoder.CardOutputs];
        private readonly float[] labels = new float[FeatureEncoder.CardOutputs];
        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly float[] featureValues = new float[FeatureEncoder.MaxActive];
        private int first;
        private int hanging;
        private int southNorthTotal;
        private int eastWestTotal;

        public SelfPlayActor(TrainingSettings settings, int seed)
        {
            this.settings = settings;
            this.random = new Random(seed);
            for (var card = 0; card < 32; card++)
            {
                this.deck[card] = card;
            }

            this.first = this.random.Next(4);
        }

        public long Deals { get; private set; }

        public long CardSamples { get; private set; }

        public long BidSamples { get; private set; }

        /// <summary>Gets how many decisions the rollouts asked the networks for.</summary>
        public long RolloutDecisions { get; private set; }

        /// <summary>Gets how many decisions the deals had (bids, and cards with a choice).</summary>
        public long Decisions { get; private set; }

        /// <summary>
        /// Plays one deal (a match goes on from deal to deal, carrying the hanging points).
        /// </summary>
        /// <param name="models">Each seat's networks.</param>
        /// <param name="learners">The seats (bits) whose decisions become samples.</param>
        /// <param name="buffers">The samples of each network (the bidding one, then the card ones).</param>
        public void PlayDeal(NeuralModels[] models, int learners, SampleBuffer[] buffers)
        {
            for (var seat = 0; seat < 4; seat++)
            {
                this.seats[seat] ??= new NeuralEvaluator(models[seat]);
                this.seats[seat].Models = models[seat];
            }

            for (var i = this.deck.Length - 1; i > 0; i--)
            {
                var j = this.random.Next(i + 1);
                (this.deck[i], this.deck[j]) = (this.deck[j], this.deck[i]);
            }

            var deal = NeuralDeal.Deal(this.deck, this.first, this.hanging);
            while (!deal.AuctionFinished)
            {
                var seat = deal.ToBid;
                var candidates = this.Candidates(in deal);
                if ((learners & (1 << seat)) != 0 && this.random.NextDouble() < this.settings.BidLabelChance)
                {
                    this.LabelBids(in deal, candidates, buffers[NeuralModels.BidTag]);
                }

                var bid = this.random.NextDouble() < this.settings.BidExploration
                    ? FeatureEncoder.BidOfIndex(this.RandomBit(candidates))
                    : this.BestBid(in deal, this.values);
                deal.Bid(bid);
                this.Decisions++;
            }

            if (deal.Contract != BidType.Pass)
            {
                deal.StartPlay(this.simulator, this.announces);
                var network = 1 + FeatureEncoder.CardNetwork(deal.Kind);
                while (!deal.IsFinished)
                {
                    deal.DeclareIfFirstCard();
                    var legal = this.simulator.LegalMoves(in deal.Play);
                    int card;
                    if ((legal & (legal - 1)) == 0)
                    {
                        card = BitOperations.TrailingZeroCount(legal);
                    }
                    else
                    {
                        var seat = deal.Play.Turn;
                        if ((learners & (1 << seat)) != 0 && this.random.NextDouble() < this.settings.CardLabelChance)
                        {
                            this.LabelCards(in deal, legal, buffers[network]);
                        }

                        card = this.random.NextDouble() < this.settings.CardExploration
                            ? this.RandomBit(legal)
                            : this.seats[seat].BestCard(in deal, legal, this.values);
                        this.Decisions++;
                    }

                    deal.PlayCard(this.simulator, card, legal);
                }
            }

            deal.Score(this.simulator, out var southNorth, out var eastWest, out var newHanging);
            this.Deals++;
            this.hanging = newHanging;
            this.southNorthTotal += southNorth;
            this.eastWestTotal += eastWest;
            if (this.southNorthTotal >= 151 || this.eastWestTotal >= 151)
            {
                this.southNorthTotal = 0;
                this.eastWestTotal = 0;
                this.hanging = 0;
                this.first = this.random.Next(4);
            }
            else
            {
                this.first = (this.first + 1) & 3;
            }
        }

        // Every legal card, played out to the end of this deal.
        private void LabelCards(in NeuralDeal deal, uint legal, SampleBuffer buffer)
        {
            var team = deal.Play.Turn & 1;
            var rotation = FeatureEncoder.Rotation(deal.Kind);
            Array.Clear(this.labels);
            var mask = 0u;
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var copy = deal;
                copy.PlayCard(this.simulator, card, legal);
                this.Finish(ref copy);
                var output = FeatureEncoder.ToNetwork(card, rotation);
                this.labels[output] = this.Result(in copy, team) / NeuralEvaluator.ValueScale;
                mask |= 1u << output;
            }

            var count = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.featureValues);
            var owners = buffer.TracksOwners ? CardOwnership.Encode(in deal) : 0;
            if (buffer.Add(this.indices.AsSpan(0, count), this.featureValues.AsSpan(0, count), this.labels, mask, owners))
            {
                this.CardSamples++;
            }
        }

        // Every bid open, the auction and the deal played out.
        private void LabelBids(in NeuralDeal deal, uint candidates, SampleBuffer buffer)
        {
            var team = deal.ToBid & 1;
            Array.Clear(this.labels);
            var mask = 0u;
            for (var rest = candidates; rest != 0; rest &= rest - 1)
            {
                var index = BitOperations.TrailingZeroCount(rest);
                var copy = deal;
                copy.Bid(FeatureEncoder.BidOfIndex(index));
                while (!copy.AuctionFinished)
                {
                    copy.Bid(this.BestBid(in copy, this.rolloutValues));
                    this.RolloutDecisions++;
                }

                if (copy.Contract != BidType.Pass)
                {
                    copy.StartPlay(this.simulator, this.announces);
                    this.Finish(ref copy);
                }

                this.labels[index] = this.Result(in copy, team) / NeuralEvaluator.ValueScale;
                mask |= 1u << index;
            }

            var count = FeatureEncoder.EncodeBid(in deal, this.indices, this.featureValues);
            if (buffer.Add(this.indices.AsSpan(0, count), this.featureValues.AsSpan(0, count), this.labels.AsSpan(0, FeatureEncoder.BidOutputs), mask))
            {
                this.BidSamples++;
            }
        }

        // The bids open to the seat to bid: all of them, or with NaturalBidding those a person could read.
        private uint Candidates(in NeuralDeal deal) => this.settings.NaturalBidding
            ? NaturalBidding.Candidates(deal.AvailableBids(), deal.Play.Hands[deal.ToBid])
            : NeuralEvaluator.BidCandidates(deal.AvailableBids());

        // The best bid of the seat to bid among its candidates.
        private BidType BestBid(in NeuralDeal deal, float[] bidValues)
        {
            if (!this.settings.NaturalBidding)
            {
                return this.seats[deal.ToBid].BestBid(in deal, bidValues);
            }

            this.seats[deal.ToBid].EvaluateBids(in deal, bidValues);
            return FeatureEncoder.BidOfIndex(NeuralEvaluator.Best(bidValues, this.Candidates(in deal)));
        }

        // Plays the rest of the deal as the seats' networks would.
        private void Finish(ref NeuralDeal deal)
        {
            while (!deal.IsFinished)
            {
                deal.DeclareIfFirstCard();
                var legal = this.simulator.LegalMoves(in deal.Play);
                int card;
                if ((legal & (legal - 1)) == 0)
                {
                    card = BitOperations.TrailingZeroCount(legal);
                }
                else
                {
                    card = this.seats[deal.Play.Turn].BestCard(in deal, legal, this.rolloutValues);
                    this.RolloutDecisions++;
                }

                deal.PlayCard(this.simulator, card, legal);
            }
        }

        // The team's game points from the finished deal minus the other team's.
        private float Result(in NeuralDeal deal, int team)
        {
            deal.Score(this.simulator, out var southNorth, out var eastWest, out _);
            return team == 0 ? southNorth - eastWest : eastWest - southNorth;
        }

        private int RandomBit(uint bits)
        {
            for (var skip = this.random.Next(BitOperations.PopCount(bits)); skip > 0; skip--)
            {
                bits &= bits - 1;
            }

            return BitOperations.TrailingZeroCount(bits);
        }
    }
}
