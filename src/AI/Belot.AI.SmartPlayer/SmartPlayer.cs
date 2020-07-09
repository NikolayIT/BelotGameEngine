namespace Belot.AI.SmartPlayer
{
    using System.Collections.Generic;
    using System.Linq;

    using Belot.AI.SmartPlayer.Strategies;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class SmartPlayer : IPlayer
    {
        private readonly TrickWinnerService trickWinnerService;
        private readonly ValidAnnouncesService validAnnouncesService;

        private readonly AllTrumpsPlayStrategy allTrumpsStrategy;
        private readonly AllTrumpsPlayingFirstPlayStrategy allTrumpsPlayingFirstStrategy;

        private readonly NoTrumpsPlayStrategy noTrumpsStrategy;
        private readonly NoTrumpsPlayingFirstPlayStrategy noTrumpsPlayingFirstStrategy;

        private readonly TrumpPlayStrategy trumpStrategy;
        private readonly TrumpPlayingFirstPlayStrategy trumpPlayingFirstStrategy;

        public SmartPlayer()
        {
            this.trickWinnerService = new TrickWinnerService();
            this.validAnnouncesService = new ValidAnnouncesService();

            this.allTrumpsStrategy = new AllTrumpsPlayStrategy();
            this.allTrumpsPlayingFirstStrategy = new AllTrumpsPlayingFirstPlayStrategy();

            this.noTrumpsStrategy = new NoTrumpsPlayStrategy();
            this.noTrumpsPlayingFirstStrategy = new NoTrumpsPlayingFirstPlayStrategy();

            this.trumpStrategy = new TrumpPlayStrategy();
            this.trumpPlayingFirstStrategy = new TrumpPlayingFirstPlayStrategy();
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var bids = new Dictionary<BidType, int>();
            if (context.AvailableBids.HasFlag(BidType.Clubs))
            {
                bids.Add(BidType.Clubs, CalculateTrumpBidPoints(context.MyCards, CardSuit.Club));
            }

            if (context.AvailableBids.HasFlag(BidType.Diamonds))
            {
                bids.Add(BidType.Diamonds, CalculateTrumpBidPoints(context.MyCards, CardSuit.Diamond));
            }

            if (context.AvailableBids.HasFlag(BidType.Hearts))
            {
                bids.Add(BidType.Hearts, CalculateTrumpBidPoints(context.MyCards, CardSuit.Heart));
            }

            if (context.AvailableBids.HasFlag(BidType.Spades))
            {
                bids.Add(BidType.Spades, CalculateTrumpBidPoints(context.MyCards, CardSuit.Spade));
            }

            if (context.AvailableBids.HasFlag(BidType.AllTrumps))
            {
                bids.Add(BidType.AllTrumps, CalculateAllTrumpBidPoints(context.MyCards));
            }

            if (context.AvailableBids.HasFlag(BidType.NoTrumps))
            {
                bids.Add(BidType.NoTrumps, CalculateNoTrumpBidPoints(context.MyCards));
            }

            var bid = bids.Where(x => x.Value >= 90).OrderByDescending(x => x.Value)
                .Select(e => (KeyValuePair<BidType, int>?)e).FirstOrDefault();
            return bid?.Key ?? BidType.Pass;
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var playedCards = new CardCollection();
            foreach (var card in context.RoundActions.Where(x => x.TrickNumber < context.CurrentTrickNumber)
                .Select(x => x.Card))
            {
                playedCards.Add(card);
            }

            // All trumps
            if (context.CurrentContract.Type.HasFlag(BidType.AllTrumps))
            {
                return context.CurrentTrickActions.Any()
                           ? this.allTrumpsStrategy.PlayCard(context, playedCards)
                           : this.allTrumpsPlayingFirstStrategy.PlayCard(context, playedCards);
            }

            // No trumps
            if (context.CurrentContract.Type.HasFlag(BidType.NoTrumps))
            {
                return context.CurrentTrickActions.Any()
                           ? this.noTrumpsStrategy.PlayCard(context, playedCards)
                           : this.noTrumpsPlayingFirstStrategy.PlayCard(context, playedCards);
            }

            // Suit contract
            return context.CurrentTrickActions.Any()
                       ? this.trumpStrategy.PlayCard(context, playedCards)
                       : this.trumpPlayingFirstStrategy.PlayCard(context, playedCards);
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

        private static int CalculateAllTrumpBidPoints(CardCollection cards)
        {
            var bidPoints = 0;
            foreach (var card in cards)
            {
                if (card.Type == CardType.Jack)
                {
                    bidPoints += 35;
                }

                if (card.Type == CardType.Nine)
                {
                    bidPoints += cards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit) ? 25 : 15;
                }

                if (card.Type == CardType.Ace)
                {
                    bidPoints += cards.Any(x => x.Type == CardType.Jack && x.Suit == card.Suit)
                                 && cards.Any(x => x.Type == CardType.Nine && x.Suit == card.Suit)
                                     ? 10
                                     : 5;
                }
            }

            return bidPoints;
        }

        private static int CalculateNoTrumpBidPoints(CardCollection cards)
        {
            var bidPoints = 0;
            foreach (var card in cards)
            {
                if (card.Type == CardType.Ace)
                {
                    bidPoints += 30;
                }

                if (card.Type == CardType.Ten)
                {
                    bidPoints += cards.Any(x => x.Type == CardType.Ace && x.Suit == card.Suit) ? 22 : 15;
                }
            }

            return bidPoints;
        }

        private static int CalculateTrumpBidPoints(CardCollection cards, CardSuit trumpSuit)
        {
            var bidPoints = 0;
            foreach (var card in cards)
            {
                if (card.Type == CardType.Jack && card.Suit == trumpSuit)
                {
                    bidPoints += 40;
                }
                else if (card.Type == CardType.Nine && card.Suit == trumpSuit)
                {
                    bidPoints += 30;
                }
                else if (card.Type == CardType.Ace && card.Suit == trumpSuit)
                {
                    bidPoints += 25;
                }
                else if (card.Suit == trumpSuit)
                {
                    bidPoints += 15;
                }
                else if (card.Type == CardType.Ace)
                {
                    bidPoints += 20;
                }
                else if (card.Type == CardType.Ten)
                {
                    bidPoints += 10;
                }
            }

            //// if (this.validAnnouncesService.GetAvailableAnnounces(cards).Sum())

            return bidPoints;
        }
    }
}
