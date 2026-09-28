namespace Belot.NeuralTrainer
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>Public histories for external training opponents; never exposes the simulator's other hands.</summary>
    internal sealed class TrainingDealContext
    {
        private readonly List<Bid> bids = new List<Bid>();
        private readonly List<BelotPlayedCard> played = new List<BelotPlayedCard>();
        private readonly List<BelotAnnounce> combinations = new List<BelotAnnounce>();
        private readonly int round;
        private readonly int southNorth;
        private readonly int eastWest;

        public TrainingDealContext(int round, int southNorth, int eastWest)
        {
            this.round = round;
            this.southNorth = southNorth;
            this.eastWest = eastWest;
        }

        public void RecordBid(int seat, BidType bid) => this.bids.Add(new Bid(Position(seat), bid));

        public void StartPlay(in NeuralDeal deal)
        {
            if (deal.Kind == SimTables.NoTrumps)
            {
                return;
            }

            var service = new ValidAnnouncesService();
            for (var seat = 0; seat < 4; seat++)
            {
                var hand = new CardCollection();
                foreach (var card in Cards(deal.Play.Hands[seat]))
                {
                    hand.Add(card);
                }

                var accepted = new List<Announce>();
                foreach (var announce in service.GetAvailableAnnounces(hand))
                {
                    if (!accepted.Any(previous => service.HaveCommonCards(previous, announce)))
                    {
                        accepted.Add(announce);
                        this.combinations.Add(new BelotAnnounce { Player = Position(seat), Type = announce.Type });
                    }
                }
            }
        }

        public void RecordCard(int seat, int card, bool belote) =>
            this.played.Add(new BelotPlayedCard { Player = Position(seat), Card = Card.AllCards[card], Belote = belote });

        public PlayerGetBidContext BidContext(in NeuralDeal deal)
        {
            var view = this.View(in deal, deal.ToBid);
            view.Decision = BelotDecision.Bid;
            view.AvailableBids = deal.AvailableBids();
            return view.CreateBidContext();
        }

        public PlayerPlayCardContext PlayContext(in NeuralDeal deal, uint legal)
        {
            var view = this.View(in deal, deal.Play.Turn);
            view.Decision = BelotDecision.PlayCard;
            view.PlayableCards = Cards(legal);
            var tricks = new List<BelotTrick>();
            var complete = this.played.Count / 4;
            for (var index = 0; index < complete; index++)
            {
                tricks.Add(new BelotTrick { Cards = this.played.GetRange(index * 4, 4) });
            }

            view.Tricks = tricks;
            view.CurrentTrick = this.played.GetRange(complete * 4, this.played.Count % 4);
            var announces = new List<BelotAnnounce>();
            foreach (var combination in this.combinations)
            {
                if ((deal.DeclaredSeats & (1 << combination.Player.Index())) != 0)
                {
                    announces.Add(new BelotAnnounce
                    {
                        Player = combination.Player,
                        Type = combination.Type,
                        Card = combination.Player == view.Seat ? combination.Card : null,
                    });
                }
            }

            foreach (var card in this.played)
            {
                if (card.Belote)
                {
                    announces.Add(new BelotAnnounce { Player = card.Player, Type = AnnounceType.Belot, Card = card.Card, IsScored = true });
                }
            }

            view.Announces = announces;
            return view.CreatePlayCardContext();
        }

        private static PlayerPosition Position(int seat) => (PlayerPosition)(1 << seat);

        private static List<Card> Cards(uint mask)
        {
            var cards = new List<Card>(8);
            for (var remaining = mask; remaining != 0; remaining &= remaining - 1)
            {
                cards.Add(Card.AllCards[BitOperations.TrailingZeroCount(remaining)]);
            }

            return cards;
        }

        private BelotSeatView View(in NeuralDeal deal, int seat) => new BelotSeatView
        {
            Seat = Position(seat),
            ToMove = Position(seat),
            FirstToPlayInTheRound = Position(deal.First),
            RoundNumber = this.round,
            SouthNorthPoints = this.southNorth,
            EastWestPoints = this.eastWest,
            HangingPoints = deal.Hanging,
            Hand = Cards(deal.Play.Hands[seat]),
            Bids = this.bids,
            Contract = new Bid(Position(deal.Declarer), deal.Contract),
        };
    }
}
