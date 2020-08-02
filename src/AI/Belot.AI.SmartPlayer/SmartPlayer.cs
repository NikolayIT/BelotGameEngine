namespace Belot.AI.SmartPlayer
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

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

        private readonly IPlayStrategy allTrumpsPlaying1StStrategy;
        private readonly IPlayStrategy allTrumpsPlaying2NdStrategy;
        private readonly IPlayStrategy allTrumpsPlaying3RdStrategy;
        private readonly IPlayStrategy allTrumpsPlaying4ThStrategy;

        private readonly IPlayStrategy noTrumpsPlaying1StStrategy;
        private readonly IPlayStrategy noTrumpsPlaying2NdStrategy;
        private readonly IPlayStrategy noTrumpsPlaying3RdStrategy;
        private readonly IPlayStrategy noTrumpsPlaying4ThStrategy;

        private readonly IPlayStrategy trumpPlaying1StStrategy;
        private readonly IPlayStrategy trumpPlaying2NdStrategy;
        private readonly IPlayStrategy trumpPlaying3RdStrategy;
        private readonly IPlayStrategy trumpPlaying4ThStrategy;

        public SmartPlayer()
        {
            this.trickWinnerService = new TrickWinnerService();
            this.validAnnouncesService = new ValidAnnouncesService();

            this.allTrumpsPlaying1StStrategy = new AllTrumpsPlaying1StPlayStrategy();
            this.allTrumpsPlaying2NdStrategy = new AllTrumpsPlaying2NdPlayStrategy();
            this.allTrumpsPlaying3RdStrategy = new AllTrumpsPlaying3RdPlayStrategy();
            this.allTrumpsPlaying4ThStrategy = new AllTrumpsPlaying4ThPlayStrategy();

            this.noTrumpsPlaying1StStrategy = new NoTrumpsPlaying1StPlayStrategy();
            this.noTrumpsPlaying2NdStrategy = new NoTrumpsPlaying2NdPlayStrategy();
            this.noTrumpsPlaying3RdStrategy = new NoTrumpsPlaying3RdPlayStrategy();
            this.noTrumpsPlaying4ThStrategy = new NoTrumpsPlaying4ThPlayStrategy(this.trickWinnerService);

            this.trumpPlaying1StStrategy = new TrumpPlaying1StPlayStrategy();
            this.trumpPlaying2NdStrategy = new TrumpPlaying2NdPlayStrategy();
            this.trumpPlaying3RdStrategy = new TrumpPlaying3RdPlayStrategy();
            this.trumpPlaying4ThStrategy = new TrumpPlaying4ThPlayStrategy(this.trickWinnerService);
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var announcePoints = this.validAnnouncesService.GetAvailableAnnounces(context.MyCards).Sum(x => x.Value);
            var bids = new Dictionary<BidType, int>();
            if (context.AvailableBids.HasFlag(BidType.Clubs))
            {
                bids.Add(BidType.Clubs, CalculateTrumpBidPoints(context.MyCards, CardSuit.Club, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.Diamonds))
            {
                bids.Add(BidType.Diamonds, CalculateTrumpBidPoints(context.MyCards, CardSuit.Diamond, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.Hearts))
            {
                bids.Add(BidType.Hearts, CalculateTrumpBidPoints(context.MyCards, CardSuit.Heart, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.Spades))
            {
                bids.Add(BidType.Spades, CalculateTrumpBidPoints(context.MyCards, CardSuit.Spade, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.AllTrumps))
            {
                bids.Add(
                    BidType.AllTrumps,
                    CalculateAllTrumpsBidPoints(context.MyCards, context.Bids, context.MyPosition.GetTeammate(), announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.NoTrumps))
            {
                bids.Add(BidType.NoTrumps, CalculateNoTrumpsBidPoints(context.MyCards));
            }

            var bid = bids.Where(x => x.Value >= 100).OrderByDescending(x => x.Value)
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

            var currentTricksActionsCount = context.CurrentTrickActions.Count();

            // All trumps
            if (context.CurrentContract.Type.HasFlag(BidType.AllTrumps))
            {
                return currentTricksActionsCount == 0
                           ? this.allTrumpsPlaying1StStrategy.PlayCard(context, playedCards)
                           : currentTricksActionsCount == 1
                               ? this.allTrumpsPlaying2NdStrategy.PlayCard(context, playedCards)
                               : currentTricksActionsCount == 2
                                   ? this.allTrumpsPlaying3RdStrategy.PlayCard(context, playedCards)
                                   : this.allTrumpsPlaying4ThStrategy.PlayCard(context, playedCards);
            }

            // No trumps
            if (context.CurrentContract.Type.HasFlag(BidType.NoTrumps))
            {
                return currentTricksActionsCount == 0
                           ? this.noTrumpsPlaying1StStrategy.PlayCard(context, playedCards)
                           : currentTricksActionsCount == 1
                               ? this.noTrumpsPlaying2NdStrategy.PlayCard(context, playedCards)
                               : currentTricksActionsCount == 2
                                   ? this.noTrumpsPlaying3RdStrategy.PlayCard(context, playedCards)
                                   : this.noTrumpsPlaying4ThStrategy.PlayCard(context, playedCards);
            }

            // Suit contract
            return currentTricksActionsCount == 0
                       ? this.trumpPlaying1StStrategy.PlayCard(context, playedCards)
                       : currentTricksActionsCount == 1
                           ? this.trumpPlaying2NdStrategy.PlayCard(context, playedCards)
                           : currentTricksActionsCount == 2
                               ? this.trumpPlaying3RdStrategy.PlayCard(context, playedCards)
                               : this.trumpPlaying4ThStrategy.PlayCard(context, playedCards);
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

        private static int CalculateAllTrumpsBidPoints(CardCollection cards, IEnumerable<Bid> previousBids, PlayerPosition teammate, int announcePoints)
        {
            var bidPoints = announcePoints / 3;
            foreach (var card in cards)
            {
                if (card.Type == CardType.Jack)
                {
                    bidPoints += 45;
                }

                if (card.Type == CardType.Nine)
                {
                    bidPoints += cards.Contains(Card.GetCard(card.Suit, CardType.Jack)) ? 25 : 15;
                }

                if (card.Type == CardType.Ace)
                {
                    bidPoints += cards.Contains(Card.GetCard(card.Suit, CardType.Jack))
                                 && cards.Contains(Card.GetCard(card.Suit, CardType.Nine))
                                     ? 10
                                     : 5;
                }
            }

            if (previousBids.Any(
                x => x.Player == teammate && (x.Type == BidType.Clubs || x.Type == BidType.Diamonds
                                                                      || x.Type == BidType.Hearts
                                                                      || x.Type == BidType.Spades)))
            {
                // If the teammate has announced suit, increase all trump bid points
                bidPoints += 5;
            }

            return bidPoints;
        }

        private static int CalculateNoTrumpsBidPoints(CardCollection cards)
        {
            var bidPoints = 0;
            foreach (var card in cards)
            {
                if (card.Type == CardType.Ace)
                {
                    bidPoints += 45;
                }

                if (card.Type == CardType.Ten)
                {
                    bidPoints += cards.Contains(Card.GetCard(card.Suit, CardType.Ace)) ? 25 : 15;
                }

                if (card.Type == CardType.King)
                {
                    bidPoints += cards.Contains(Card.GetCard(card.Suit, CardType.Ace))
                                 && cards.Contains(Card.GetCard(card.Suit, CardType.Ten))
                                     ? 10
                                     : 5;
                }
            }

            return bidPoints;
        }

        private static int CalculateTrumpBidPoints(CardCollection cards, CardSuit trumpSuit, int announcePoints)
        {
            var bidPoints = announcePoints / 2;
            foreach (var card in cards)
            {
                if (card.Type == CardType.Jack && card.Suit == trumpSuit)
                {
                    bidPoints += 50;
                }
                else if (card.Type == CardType.Nine && card.Suit == trumpSuit)
                {
                    bidPoints += 35;
                }
                else if (card.Type == CardType.Ace && card.Suit == trumpSuit)
                {
                    bidPoints += 25;
                }
                else if (card.Type == CardType.Ten && card.Suit == trumpSuit)
                {
                    bidPoints += 20;
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
                    bidPoints += cards.Contains(Card.GetCard(card.Suit, CardType.Ace)) ? 15 : 10;
                }
            }

            return bidPoints;
        }
    }
}
