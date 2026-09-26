namespace Belot.AI.ClaudePlayer.Neural
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// One deal as the neural player follows it: the auction, the combinations declared, the
    /// cards each seat has played and what they showed (<see cref="PlayInference"/>), and the
    /// position of the card play. It is a struct, so a training rollout copies it to try each
    /// action. In self-play it holds all four hands and plays the whole deal by the engine's
    /// rules (the auction, the declarations, the tricks, the score); built from a player's
    /// context it knows that player's hand only. The features (<see cref="FeatureEncoder"/>)
    /// read the hand of the seat deciding and nothing else that is hidden from it.
    /// </summary>
    internal struct NeuralDeal
    {
        public const uint RankMask = 0x01010101u;

        /// <summary>The first to bid and to lead.</summary>
        public int First;

        /// <summary>The points hanging from earlier deals, for the winner of this one.</summary>
        public int Hanging;

        /// <summary>The contract so far (with the doubling flags); Pass while nobody has bid.</summary>
        public BidType Contract;

        public int Declarer;

        public int ToBid;

        public int ConsecutivePasses;

        public int BidCount;

        public bool AuctionFinished;

        /// <summary>Each seat's bids, as flags.</summary>
        public SeatBytes BidsMade;

        /// <summary>How many times each seat passed.</summary>
        public SeatBytes Passes;

        /// <summary>The contract's kind (see <see cref="SimTables"/>), once the card play starts.</summary>
        public int Kind;

        /// <summary>The card play (only the known hands are in it).</summary>
        public SimState Play;

        public uint Played;

        public SeatMasks PlayedBy;

        /// <summary>The cards each seat cannot hold, as the play showed.</summary>
        public SeatMasks Excluded;

        /// <summary>The cards each seat surely holds (belotes, four jacks or nines).</summary>
        public SeatMasks Known;

        /// <summary>The kinds of combinations each seat declared (see <see cref="DeclarationBit"/>).</summary>
        public SeatBytes Declared;

        /// <summary>The card each seat played to the trick in progress, plus one (0 = none yet).</summary>
        public SeatBytes TrickCards;

        /// <summary>Self-play: the three cards each seat gets after the auction.</summary>
        public SeatMasks LastThree;

        /// <summary>Self-play: what each seat declares when it first plays.</summary>
        public SeatBytes ToDeclare;

        /// <summary>Self-play: the seats (bits) that have declared.</summary>
        public int DeclaredSeats;

        /// <summary>Self-play: the points of the combinations that score.</summary>
        public int SouthNorthAnnounces;

        public int EastWestAnnounces;

        /// <summary>Gets a value indicating whether the deal is over (all passed, or eight tricks).</summary>
        public readonly bool IsFinished =>
            this.AuctionFinished && (this.Contract == BidType.Pass || this.Play.TricksPlayed == 8);

        /// <summary>The flag of a combination kind in <see cref="Declared"/> (sequences of 5 to 8 are alike).</summary>
        public static byte DeclarationBit(AnnounceType type) =>
            type switch
                {
                    AnnounceType.SequenceOf3 => 1,
                    AnnounceType.SequenceOf4 => 2,
                    AnnounceType.FourOfAKind => 8,
                    AnnounceType.FourNines => 16,
                    AnnounceType.FourJacks => 32,
                    AnnounceType.Belot => 0,
                    _ => 4,
                };

        /// <summary>
        /// Self-play: deals the shuffled deck the way the engine does (card i to seat i % 4, five
        /// each, then three each after the auction) and starts the auction.
        /// </summary>
        public static NeuralDeal Deal(ReadOnlySpan<int> deck, int first, int hanging)
        {
            var deal = Start(first, hanging);
            for (var i = 0; i < 32; i++)
            {
                if (i < 20)
                {
                    deal.Play.Hands[i & 3] |= 1u << deck[i];
                }
                else
                {
                    deal.LastThree[i & 3] |= 1u << deck[i];
                }
            }

            return deal;
        }

        /// <summary>A deal with no cards yet, its auction about to start.</summary>
        public static NeuralDeal Start(int first, int hanging)
        {
            var deal = default(NeuralDeal);
            deal.First = first;
            deal.Hanging = hanging;
            deal.Contract = BidType.Pass;
            deal.Declarer = first;
            deal.ToBid = first;
            return deal;
        }

        /// <summary>
        /// Rebuilds what the player knows at a bid decision. False when the context does not add
        /// up (then the player decides otherwise).
        /// </summary>
        public static bool FromBidContext(PlayerGetBidContext context, out NeuralDeal deal)
        {
            deal = Start(context.FirstToPlayInTheRound.Index(), context.HangingPoints);
            foreach (var bid in context.Bids)
            {
                if (deal.AuctionFinished || bid.Player.Index() != deal.ToBid)
                {
                    return false;
                }

                deal.RecordBid(bid.Type);
            }

            var me = context.MyPosition.Index();
            deal.Play.Hands[me] = ToMask(context.MyCards);
            return !deal.AuctionFinished && deal.ToBid == me && deal.Contract == context.CurrentContract.Type;
        }

        /// <summary>
        /// Rebuilds what the player knows at a card decision (the simulator gets the contract).
        /// False when the context does not add up.
        /// </summary>
        public static bool FromPlayContext(PlayerPlayCardContext context, BelotSimulator simulator, out NeuralDeal deal)
        {
            deal = Start(context.FirstToPlayInTheRound.Index(), context.HangingPoints);
            foreach (var bid in context.Bids)
            {
                if (deal.AuctionFinished || bid.Player.Index() != deal.ToBid)
                {
                    return false;
                }

                deal.RecordBid(bid.Type);
            }

            if (!deal.AuctionFinished || deal.Contract != context.CurrentContract.Type
                                      || deal.Declarer != context.CurrentContract.Player.Index())
            {
                return false;
            }

            deal.StartPlay(simulator);
            foreach (var announce in context.Announces)
            {
                if (announce.Type != AnnounceType.Belot)
                {
                    deal.Declare(announce.Player.Index(), DeclarationBit(announce.Type));
                }
            }

            if (context.RoundActions is IList<PlayCardAction> actions)
            {
                for (var i = 0; i < actions.Count; i++)
                {
                    if (!deal.Replay(simulator, actions[i]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                foreach (var action in context.RoundActions)
                {
                    if (!deal.Replay(simulator, action))
                    {
                        return false;
                    }
                }
            }

            var me = context.MyPosition.Index();
            var hand = ToMask(context.MyCards);
            deal.Play.Hands[me] = hand;
            return deal.Play.Turn == me && (hand & deal.Played) == 0;
        }

        public static uint ToMask(CardCollection cards)
        {
            var mask = 0u;
            foreach (var card in cards)
            {
                mask |= 1u << card.GetHashCode();
            }

            return mask;
        }

        /// <summary>The bids open to the seat to bid.</summary>
        public readonly BidType AvailableBids() => BidModel.AvailableBids(this.Contract, this.Declarer, this.ToBid);

        /// <summary>Records a bid of the seat to bid, as the engine's auction does.</summary>
        public void RecordBid(BidType bid)
        {
            var seat = this.ToBid;
            if (bid == BidType.Double || bid == BidType.ReDouble)
            {
                // Doubling only multiplies the contract: it stays with the declarer.
                this.Contract = (this.Contract & ~(BidType.Double | BidType.ReDouble)) | bid;
            }
            else if (bid != BidType.Pass)
            {
                this.Contract = bid;
                this.Declarer = seat;
            }

            if (bid == BidType.Pass)
            {
                this.Passes[seat]++;
                this.ConsecutivePasses++;
            }
            else
            {
                this.BidsMade[seat] |= (byte)bid;
                this.ConsecutivePasses = 0;
            }

            this.BidCount++;
            this.ToBid = (seat + 1) & 3;
            if ((this.Contract == BidType.Pass && this.ConsecutivePasses == 4)
                || (this.Contract != BidType.Pass && this.ConsecutivePasses == 3))
            {
                this.AuctionFinished = true;
            }
        }

        /// <summary>
        /// Self-play: makes the bid, then passes for the seats that can only pass, up to the next
        /// seat with a choice or the end of the auction.
        /// </summary>
        public void Bid(BidType bid)
        {
            this.RecordBid(bid);
            while (!this.AuctionFinished && this.AvailableBids() == BidType.Pass)
            {
                this.RecordBid(BidType.Pass);
            }
        }

        /// <summary>Starts the card play of the contract (the simulator gets the contract).</summary>
        public void StartPlay(BelotSimulator simulator)
        {
            this.Kind = SimTables.ToKind(this.Contract);
            simulator.SetContract(this.Contract, this.Declarer, this.Hanging);
            this.Play.Turn = this.First;
        }

        /// <summary>
        /// Self-play: deals the last three cards, starts the card play, and works out what every
        /// seat will declare (everything it is offered) and which combinations score.
        /// </summary>
        public void StartPlay(BelotSimulator simulator, DeclaredAnnounce[] buffer)
        {
            for (var seat = 0; seat < 4; seat++)
            {
                this.Play.Hands[seat] |= this.LastThree[seat];
            }

            this.StartPlay(simulator);
            if (this.Kind == SimTables.NoTrumps)
            {
                return;
            }

            var count = 0;
            for (var seat = 0; seat < 4; seat++)
            {
                var start = count;
                count = AnnounceScorer.AddDeclaredCombinations(this.Play.Hands[seat], seat, buffer, count);
                for (var i = start; i < count; i++)
                {
                    this.ToDeclare[seat] |= DeclarationBit(buffer[i].Type);
                }
            }

            AnnounceScorer.GetPoints(buffer, count, out this.SouthNorthAnnounces, out this.EastWestAnnounces);
        }

        /// <summary>
        /// Self-play: the seat to play declares its combinations if this is its first card (the
        /// engine asks for them then).
        /// </summary>
        public void DeclareIfFirstCard()
        {
            var seat = this.Play.Turn;
            if (this.Play.TricksPlayed == 0 && (this.DeclaredSeats & (1 << seat)) == 0)
            {
                this.DeclaredSeats |= 1 << seat;
                if (this.ToDeclare[seat] != 0)
                {
                    this.Declare(seat, this.ToDeclare[seat]);
                }
            }
        }

        /// <summary>Records declared combinations; four jacks or four nines show the cards.</summary>
        public void Declare(int seat, byte bits)
        {
            this.Declared[seat] |= bits;
            if ((bits & 32) != 0)
            {
                this.Known[seat] |= RankMask << (int)CardType.Jack;
            }

            if ((bits & 16) != 0)
            {
                this.Known[seat] |= RankMask << (int)CardType.Nine;
            }
        }

        /// <summary>Plays a card of the seat to play, with or without a belote.</summary>
        public void PlayCard(BelotSimulator simulator, int card, bool belote)
        {
            var seat = this.Play.Turn;
            var bit = 1u << card;
            PlayInference.Observe(in this.Play, seat, card, belote, this.Kind, true, ref this.Excluded[seat], ref this.Known[seat]);

            // The single bit keeps the simulator from declaring a belote by itself.
            simulator.Play(ref this.Play, card, bit);
            if (belote)
            {
                if ((seat & 1) == 0)
                {
                    this.Play.SouthNorthPoints += 20;
                }
                else
                {
                    this.Play.EastWestPoints += 20;
                }
            }

            this.Played |= bit;
            this.PlayedBy[seat] |= bit;
            if (this.Play.TrickCards == 0)
            {
                this.TrickCards = default;
            }
            else
            {
                this.TrickCards[seat] = (byte)(card + 1);
            }
        }

        /// <summary>Self-play: plays a card of the seat to play, with a belote whenever it may.</summary>
        public void PlayCard(BelotSimulator simulator, int card, uint legal) =>
            this.PlayCard(simulator, card, simulator.IsBelote(in this.Play, card, legal));

        /// <summary>The cards played to the trick in progress.</summary>
        public readonly uint TrickMask()
        {
            var mask = 0u;
            for (var seat = 0; seat < 4; seat++)
            {
                if (this.TrickCards[seat] != 0)
                {
                    mask |= 1u << (this.TrickCards[seat] - 1);
                }
            }

            return mask;
        }

        /// <summary>
        /// Self-play: scores the finished deal. The result is in game points; the new hanging
        /// points are those of a tie.
        /// </summary>
        public readonly void Score(BelotSimulator simulator, out int southNorth, out int eastWest, out int hanging)
        {
            if (this.Contract == BidType.Pass)
            {
                southNorth = 0;
                eastWest = 0;
                hanging = this.Hanging;
                return;
            }

            simulator.Score(in this.Play, this.SouthNorthAnnounces, this.EastWestAnnounces, out southNorth, out eastWest, out hanging);
        }

        private bool Replay(BelotSimulator simulator, PlayCardAction action)
        {
            var card = action.Card.GetHashCode();
            if (action.Player.Index() != this.Play.Turn || (this.Played & (1u << card)) != 0)
            {
                return false;
            }

            this.PlayCard(simulator, card, action.Belote);
            return true;
        }
    }
}
