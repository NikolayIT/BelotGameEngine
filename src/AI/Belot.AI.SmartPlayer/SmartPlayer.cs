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
        private readonly ValidAnnouncesService validAnnouncesService;
        private readonly TrickWinnerService trickWinnerService;

        private readonly IPlayStrategy allTrumpsOursContractStrategy;
        private readonly IPlayStrategy allTrumpsTheirsContractStrategy;
        private readonly IPlayStrategy noTrumpsOursContractStrategy;
        private readonly IPlayStrategy noTrumpsTheirsContractStrategy;
        private readonly IPlayStrategy trumpOursContractStrategy;
        private readonly IPlayStrategy trumpTheirsContractStrategy;

        public SmartPlayer()
        {
            this.validAnnouncesService = new ValidAnnouncesService();
            this.trickWinnerService = new TrickWinnerService();
            this.allTrumpsOursContractStrategy = new AllTrumpsOursContractStrategy();
            this.allTrumpsTheirsContractStrategy = new AllTrumpsTheirsContractStrategy();
            this.noTrumpsOursContractStrategy = new NoTrumpsOursContractStrategy();
            this.noTrumpsTheirsContractStrategy = new NoTrumpsTheirsContractStrategy();
            this.trumpOursContractStrategy = new TrumpOursContractStrategy();
            this.trumpTheirsContractStrategy = new TrumpTheirsContractStrategy();
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            var announcePoints = this.validAnnouncesService.GetAvailableAnnounces(context.MyCards).Sum(x => x.Value);
            var cards = context.MyCards.ToList();
            var bids = new Dictionary<BidType, int>();
            if (context.AvailableBids.HasFlag(BidType.Clubs))
            {
                bids.Add(BidType.Clubs, CalculateTrumpBidPoints(cards, CardSuit.Club, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.Diamonds))
            {
                bids.Add(BidType.Diamonds, CalculateTrumpBidPoints(cards, CardSuit.Diamond, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.Hearts))
            {
                bids.Add(BidType.Hearts, CalculateTrumpBidPoints(cards, CardSuit.Heart, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.Spades))
            {
                bids.Add(BidType.Spades, CalculateTrumpBidPoints(cards, CardSuit.Spade, announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.AllTrumps))
            {
                bids.Add(
                    BidType.AllTrumps,
                    CalculateAllTrumpsBidPoints(cards, context.Bids, context.MyPosition.GetTeammate(), announcePoints));
            }

            if (context.AvailableBids.HasFlag(BidType.NoTrumps))
            {
                bids.Add(BidType.NoTrumps, CalculateNoTrumpsBidPoints(cards));
            }

            var bid = bids.Where(x => x.Value >= 100).OrderByDescending(x => x.Value)
                .Select(e => (KeyValuePair<BidType, int>?)e).FirstOrDefault();
            return bid?.Key ?? BidType.Pass;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var playedCards = new CardCollection();
            foreach (var action in context.RoundActions)
            {
                if (action.TrickNumber < context.CurrentTrickNumber)
                {
                    playedCards.Add(action.Card);
                }
            }

            IPlayStrategy strategy;
            if (context.CurrentContract.Type.HasFlag(BidType.AllTrumps))
            {
                strategy = context.CurrentContract.Player.IsInSameTeamWith(context.MyPosition)
                               ? this.allTrumpsOursContractStrategy
                               : this.allTrumpsTheirsContractStrategy;
            }
            else if (context.CurrentContract.Type.HasFlag(BidType.NoTrumps))
            {
                strategy = context.CurrentContract.Player.IsInSameTeamWith(context.MyPosition)
                               ? this.noTrumpsOursContractStrategy
                               : this.noTrumpsTheirsContractStrategy;
            }
            else
            {
                // Trump contract
                strategy = context.CurrentContract.Player.IsInSameTeamWith(context.MyPosition)
                               ? this.trumpOursContractStrategy
                               : this.trumpTheirsContractStrategy;
            }

            return context.CurrentTrickActions.Count switch
                {
                    0 => strategy.PlayFirst(context, playedCards),
                    1 => strategy.PlaySecond(context, playedCards),
                    2 => strategy.PlayThird(
                        context,
                        playedCards,
                        this.trickWinnerService.GetWinner(context.CurrentContract, context.CurrentTrickActions)),
                    _ => strategy.PlayFourth(
                        context,
                        playedCards,
                        this.trickWinnerService.GetWinner(context.CurrentContract, context.CurrentTrickActions)),
                };
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

        private static int CalculateAllTrumpsBidPoints(List<Card> cards, IEnumerable<Bid> previousBids, PlayerPosition teammate, int announcePoints)
        {
            var bidPoints = announcePoints / 3;
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
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

        private static int CalculateNoTrumpsBidPoints(List<Card> cards)
        {
            var bidPoints = 0;
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
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

        private static int CalculateTrumpBidPoints(List<Card> cards, CardSuit trumpSuit, int announcePoints)
        {
            var bidPoints = announcePoints / 2;
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card.Suit == trumpSuit)
                {
                    switch (card.Type)
                    {
                        case CardType.Jack:
                            bidPoints += 55;
                            break;
                        case CardType.Nine:
                            bidPoints += 35;
                            break;
                        case CardType.Ace:
                            bidPoints += 25;
                            break;
                        case CardType.Ten:
                            bidPoints += 20;
                            break;
                        case CardType.Queen when cards.Contains(Card.GetCard(trumpSuit, CardType.King)):
                            bidPoints += 25;
                            break;
                        case CardType.King:
                        case CardType.Queen:
                            bidPoints += 16;
                            break;
                        case CardType.Seven:
                        case CardType.Eight:
                            bidPoints += 15;
                            break;
                    }
                }
                else
                {
                    switch (card.Type)
                    {
                        case CardType.Ace:
                            bidPoints += 20;
                            break;
                        case CardType.Ten when cards.Contains(Card.GetCard(card.Suit, CardType.Ace)):
                            bidPoints += 15;
                            break;
                        case CardType.Ten:
                            bidPoints += 10;
                            break;
                    }
                }
            }

            return bidPoints;
        }
    }
}
