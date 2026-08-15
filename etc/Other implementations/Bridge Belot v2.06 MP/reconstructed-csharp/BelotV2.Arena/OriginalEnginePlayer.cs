namespace BelotArena
{
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    using V2 = BelotV2;

    /// <summary>
    /// The 2001 game's AI, wearing the modern engine's <see cref="IPlayer"/> interface, so that
    /// `belot.exe`'s decisions can be played against SmartPlayer inside BelotGame.
    ///
    /// Nothing about the AI is re-implemented here: bidding goes to <c>BiddingAi</c> (verified
    /// 1:1 against the binary on 17,499 decisions) and card play to whichever brain is supplied —
    /// either <c>OriginalPlayAdapter</c>, the transcription that matches the binary on all 5,160
    /// recorded decisions, or <c>OriginalAiPlayer</c>, which asks the real x86 routine running
    /// under emulation. This class only translates between the two engines' vocabularies.
    ///
    /// The two happen to agree on the things that matter: both number the suits clubs, diamonds,
    /// hearts, spades from zero, both seat the players south, east, north, west and rotate in
    /// that order, and both play to 151. So a seat index and a suit index cross over unchanged;
    /// what needs work is the rank base (0..7 against 7..14), the bid encoding (flags against a
    /// small ordinal), and the round history, which the engine hands over as one flat list
    /// including the current trick while the AI wants the completed tricks separately.
    /// </summary>
    public sealed class OriginalEnginePlayer : IPlayer
    {
        private readonly Func<V2.PlayContext, V2.Card> brain;
        private readonly V2.DelphiRandom rng;

        public OriginalEnginePlayer(string name, Func<V2.PlayContext, V2.Card> brain, uint seed = 1)
        {
            this.Name = name;
            this.brain = brain;
            this.rng = new V2.DelphiRandom(seed);
        }

        public string Name { get; }

        /// <summary>
        /// Bids the original wanted to make but the engine would not accept. The original's
        /// auction is its own; when it names a contract that is not on the modern engine's
        /// available list the bid becomes a pass rather than an exception. Reported after a run
        /// because it is the one place this bridge can silently change the original's mind.
        /// </summary>
        public int RejectedBids { get; private set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var history = new List<V2.BidAction>();
            int contractSeat = -1;
            V2.BidType? highest = null;
            foreach (Bid b in context.Bids)
            {
                V2.BidType v2 = ToV2Bid(b.Type);
                history.Add(new V2.BidAction(b.Player.Index(), v2));
                if (V2.Bids.IsContract(v2))
                {
                    highest = v2;
                    contractSeat = b.Player.Index();
                }
            }

            var ctx = new V2.BidContext
            {
                Seat = context.MyPosition.Index(),
                Hand = ToV2Cards(context.MyCards),
                History = history,
                HighestContract = highest,
                ContractSeat = contractSeat,
                CurrentlyDoubled = context.CurrentContract.Type.HasFlag(BidType.Double)
                                   || context.CurrentContract.Type.HasFlag(BidType.ReDouble),
                Board = new[] { context.SouthNorthPoints, context.EastWestPoints },
            };

            BidType bid = ToEngineBid(V2.BiddingAi.ChooseBid(ctx, this.rng));
            if (bid != BidType.Pass && !context.AvailableBids.HasFlag(bid))
            {
                this.RejectedBids++;
                return BidType.Pass;
            }

            return bid;
        }

        // The original declares everything it holds, as the engine's own players do.
        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var trick = new List<V2.Play>(4);
            foreach (PlayCardAction a in context.CurrentTrickActions)
            {
                trick.Add(new V2.Play(a.Player.Index(), ToV2(a.Card)));
            }

            // RoundActions already contains the cards played so far in the current trick, and the
            // AI wants only the completed ones — they are the leading entries, since the engine
            // appends in play order.
            var completed = new List<V2.Play>(28);
            foreach (PlayCardAction a in context.RoundActions)
            {
                completed.Add(new V2.Play(a.Player.Index(), ToV2(a.Card)));
            }

            completed.RemoveRange(completed.Count - trick.Count, trick.Count);

            var ctx = new V2.PlayContext
            {
                Seat = context.MyPosition.Index(),
                Hand = ToV2Cards(context.MyCards),
                Contract = BuildContract(context),
                CurrentTrick = trick,
                PlayedHistory = completed,
                Legal = ToV2Cards(context.AvailableCardsToPlay),
                BidSuits = BidSuits(context.Bids),
                HumanSeat = -1,
            };

            return new PlayCardAction(ToEngine(this.brain(ctx)));
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(Belot.Engine.GameMechanics.RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }

        /// <summary>
        /// The declarer is NOT CurrentContract.Player: the engine overwrites that with the
        /// doubler's seat when someone doubles. It is the seat of the last actual contract bid.
        /// A few of the original's branches key on it, so it has to be right.
        /// </summary>
        private static V2.Contract BuildContract(PlayerPlayCardContext context)
        {
            int declarer = 0;
            foreach (Bid b in context.Bids)
            {
                if (b.Type != BidType.Pass && b.Type != BidType.Double && b.Type != BidType.ReDouble)
                {
                    declarer = b.Player.Index();
                }
            }

            BidType type = context.CurrentContract.Type;
            return new V2.Contract(
                ToV2Bid(type & ~(BidType.Double | BidType.ReDouble)),
                declarer,
                type.HasFlag(BidType.Double),
                type.HasFlag(BidType.ReDouble));
        }

        /// <summary>The suit each seat named in the auction; the original reads this back as
        /// "the suit my partner asked for". No-trumps and all-trumps name no suit.</summary>
        private static V2.Suit?[] BidSuits(IEnumerable<Bid> bids)
        {
            var suits = new V2.Suit?[4];
            foreach (Bid b in bids)
            {
                V2.Suit? s = V2.Bids.TrumpSuitOf(ToV2Bid(b.Type));
                if (s.HasValue)
                {
                    suits[b.Player.Index()] = s;
                }
            }

            return suits;
        }

        private static V2.Card ToV2(Card c) => new((V2.Suit)(byte)c.Suit, (V2.Rank)((byte)c.Type + 7));

        private static Card ToEngine(V2.Card c) =>
            Card.GetCard((CardSuit)(byte)c.Suit, (CardType)((int)c.Rank - 7));

        private static List<V2.Card> ToV2Cards(IEnumerable<Card> cards)
        {
            var list = new List<V2.Card>(8);
            foreach (Card c in cards)
            {
                list.Add(ToV2(c));
            }

            return list;
        }

        private static V2.BidType ToV2Bid(BidType b) => b switch
        {
            BidType.Clubs => V2.BidType.Clubs,
            BidType.Diamonds => V2.BidType.Diamonds,
            BidType.Hearts => V2.BidType.Hearts,
            BidType.Spades => V2.BidType.Spades,
            BidType.NoTrumps => V2.BidType.NoTrumps,
            BidType.AllTrumps => V2.BidType.AllTrumps,
            BidType.Double => V2.BidType.Double,
            BidType.ReDouble => V2.BidType.Redouble,
            _ => V2.BidType.Pass,
        };

        private static BidType ToEngineBid(V2.BidType b) => b switch
        {
            V2.BidType.Clubs => BidType.Clubs,
            V2.BidType.Diamonds => BidType.Diamonds,
            V2.BidType.Hearts => BidType.Hearts,
            V2.BidType.Spades => BidType.Spades,
            V2.BidType.NoTrumps => BidType.NoTrumps,
            V2.BidType.AllTrumps => BidType.AllTrumps,
            V2.BidType.Double => BidType.Double,
            V2.BidType.Redouble => BidType.ReDouble,
            _ => BidType.Pass,
        };
    }
}
