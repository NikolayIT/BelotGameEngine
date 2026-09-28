namespace BelotLegacy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Sharp = global::Belot;

    /// <summary>Runs Konstantin Ivanov's SharpBelot AI on seat-visible engine contexts.</summary>
    public sealed class SharpBelotPlayer : IPlayer, ILegacyDiagnostics
    {
        private readonly AIPlayers.AIPlayer[] players;
        private readonly Sharp.BelotGame game;
        private readonly Sharp.Card[] cards;
        private readonly Sharp.CardsCollection deck = new Sharp.CardsCollection();
        private Sharp.Card selected;

        public SharpBelotPlayer(int seed = 1)
        {
            this.players = Enumerable.Range(0, 4).Select(seat => new AIPlayers.AIPlayer($"SharpBelot {seat}")
            {
                RandomSource = new Random(unchecked(seed + (seat * 104729))),
            }).ToArray();
            this.game = new Sharp.BelotGame(this.players[0], this.players[1], this.players[2], this.players[3]);
            this.cards = Card.AllCards.Select(card => new Sharp.Card(
                (Sharp.CardType)(7 - (int)card.Type), (Sharp.CardColor)(3 - (int)card.Suit))).ToArray();
            foreach (var card in this.cards)
            {
                this.deck.Add(card);
            }

            foreach (var player in this.players)
            {
                player.CardPlayed += (_, card) => this.selected = card;
            }
        }

        public string Name => "SharpBelot";

        public long BidDecisions { get; private set; }

        public long CardDecisions { get; private set; }

        public long RejectedBids { get; private set; }

        public long CardFallbacks => 0;

        public long LegalSetDifferences { get; private set; }

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.BidDecisions++;
            var player = this.SetHand(context);
            var manager = this.Auction(context.Bids);
            var announcement = player.MakeAnnouncement(manager);
            var bid = announcement.IsReDoubled ? BidType.ReDouble
                : announcement.IsDoubled ? BidType.Double : ToBid(announcement.Type);
            if (bid != BidType.Pass && !context.AvailableBids.HasFlag(bid))
            {
                this.RejectedBids++;
                return BidType.Pass;
            }

            return bid;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.CardDecisions++;
            var player = this.SetHand(context);

            // In the source AI, card play sees the auction as it stood before this seat's
            // last bid. Reconstruct that snapshot even if our engine auto-passed for it.
            var bids = context.Bids.ToArray();
            var lastOwnBid = Array.FindLastIndex(bids, bid => bid.Player == context.MyPosition);
            player.MakeAnnouncement(this.Auction(bids.Take(Math.Max(0, lastOwnBid))));

            var manager = new EngineLegalManager(ToAnnouncement(context.CurrentContract.Type), this.game, this.deck, context.AvailableCardsToPlay);
            foreach (var action in context.RoundActions)
            {
                manager.Add(this.players[action.Player.Index()], this.cards[action.Card.GetHashCode()]);
            }

            foreach (var card in context.MyCards)
            {
                if (manager.SourceIsValid(player, this.cards[card.GetHashCode()]) != context.AvailableCardsToPlay.Contains(card))
                {
                    this.LegalSetDifferences++;
                    break;
                }
            }

            this.selected = null;
            ((Sharp.Player)player).PlayCard(manager);
            if (this.selected == null)
            {
                throw new InvalidOperationException("SharpBelot returned no card.");
            }

            var result = ToCard(this.selected);
            if (!context.AvailableCardsToPlay.Contains(result))
            {
                throw new InvalidOperationException("SharpBelot returned an illegal card.");
            }

            return new PlayCardAction(result);
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

        private static Card ToCard(Sharp.Card card) =>
            Card.GetCard((CardSuit)(3 - (int)card.CardColor), (CardType)(7 - (int)card.CardType));

        private static Sharp.Announcement ToAnnouncement(BidType bid)
        {
            var kind = bid & ~(BidType.Double | BidType.ReDouble);
            var type = kind switch
            {
                BidType.Clubs => Sharp.AnnouncementTypeEnum.Clubs,
                BidType.Diamonds => Sharp.AnnouncementTypeEnum.Diamonds,
                BidType.Hearts => Sharp.AnnouncementTypeEnum.Hearts,
                BidType.Spades => Sharp.AnnouncementTypeEnum.Spades,
                BidType.NoTrumps => Sharp.AnnouncementTypeEnum.NoTrumps,
                BidType.AllTrumps => Sharp.AnnouncementTypeEnum.AllTrumps,
                _ => Sharp.AnnouncementTypeEnum.Pass,
            };
            return new Sharp.Announcement(type, bid.HasFlag(BidType.Double), bid.HasFlag(BidType.ReDouble));
        }

        private static BidType ToBid(Sharp.AnnouncementTypeEnum type) => type switch
        {
            Sharp.AnnouncementTypeEnum.Clubs => BidType.Clubs,
            Sharp.AnnouncementTypeEnum.Diamonds => BidType.Diamonds,
            Sharp.AnnouncementTypeEnum.Hearts => BidType.Hearts,
            Sharp.AnnouncementTypeEnum.Spades => BidType.Spades,
            Sharp.AnnouncementTypeEnum.NoTrumps => BidType.NoTrumps,
            Sharp.AnnouncementTypeEnum.AllTrumps => BidType.AllTrumps,
            _ => BidType.Pass,
        };

        private AIPlayers.AIPlayer SetHand(BasePlayerContext context)
        {
            // Other hands stay empty. The legacy manager receives public played cards only.
            foreach (var seat in this.players)
            {
                seat.Cards.Clear();
            }

            var player = this.players[context.MyPosition.Index()];
            foreach (var card in context.MyCards)
            {
                player.Cards.Add(this.cards[card.GetHashCode()]);
            }

            player.Cards.Sort(new Sharp.CardComparer(ToAnnouncement(context.CurrentContract.Type).Type));
            return player;
        }

        private Sharp.AnnouncementManager Auction(IEnumerable<Bid> bids)
        {
            var manager = new Sharp.AnnouncementManager();
            var contract = BidType.Pass;
            foreach (var bid in bids)
            {
                var type = bid.Type;
                if (type == BidType.Double || type == BidType.ReDouble)
                {
                    type |= contract;
                }
                else if (type != BidType.Pass)
                {
                    contract = type;
                }

                manager.Add(this.players[bid.Player.Index()], ToAnnouncement(type));
            }

            return manager;
        }

        private sealed class EngineLegalManager : Sharp.PlayingManager
        {
            private readonly CardCollection legal;

            public EngineLegalManager(Sharp.Announcement contract, Sharp.BelotGame game, Sharp.CardsCollection deck, CardCollection legal)
                : base(contract, game, deck)
            {
                this.legal = legal;
                this.BelotFound += (_, _) => { };
            }

            public override bool IsValid(Sharp.Player player, Sharp.Card card) => this.legal.Contains(ToCard(card));

            public bool SourceIsValid(Sharp.Player player, Sharp.Card card) => base.IsValid(player, card);
        }
    }
}
