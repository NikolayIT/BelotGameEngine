namespace Belot.AI.SmartPlayer
{
    using System.Collections.Generic;

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

        // Scratch collection rebuilt on every PlayCard call. The engine drives a player
        // sequentially within a game, so reusing one instance instead of allocating is safe.
        private readonly CardCollection playedCards = new CardCollection();

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
            var availableAnnounces = this.validAnnouncesService.GetAvailableAnnounces(context.MyCards);
            var announcePoints = 0;
            for (var i = 0; i < availableAnnounces.Count; i++)
            {
                announcePoints += availableAnnounces[i].Value;
            }

            var cards = context.MyCards;
            var availableBids = context.AvailableBids;

            // Candidates are evaluated in the same order the bids dictionary used to be filled
            // in, and a tie keeps the earlier candidate, so the chosen bid is exactly what the
            // old Where(>=100)/OrderByDescending (stable) chain produced.
            var bestBid = BidType.Pass;
            var bestPoints = 99;
            if (availableBids.HasFlag(BidType.Clubs))
            {
                Consider(BidType.Clubs, CalculateTrumpBidPoints(cards, CardSuit.Club, announcePoints), ref bestBid, ref bestPoints);
            }

            if (availableBids.HasFlag(BidType.Diamonds))
            {
                Consider(BidType.Diamonds, CalculateTrumpBidPoints(cards, CardSuit.Diamond, announcePoints), ref bestBid, ref bestPoints);
            }

            if (availableBids.HasFlag(BidType.Hearts))
            {
                Consider(BidType.Hearts, CalculateTrumpBidPoints(cards, CardSuit.Heart, announcePoints), ref bestBid, ref bestPoints);
            }

            if (availableBids.HasFlag(BidType.Spades))
            {
                Consider(BidType.Spades, CalculateTrumpBidPoints(cards, CardSuit.Spade, announcePoints), ref bestBid, ref bestPoints);
            }

            if (availableBids.HasFlag(BidType.AllTrumps))
            {
                Consider(
                    BidType.AllTrumps,
                    CalculateAllTrumpsBidPoints(cards, context.Bids, context.MyPosition.GetTeammate(), announcePoints),
                    ref bestBid,
                    ref bestPoints);
            }

            if (availableBids.HasFlag(BidType.NoTrumps))
            {
                Consider(BidType.NoTrumps, CalculateNoTrumpsBidPoints(cards), ref bestBid, ref bestPoints);
            }

            return bestBid;
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            var playedCards = this.playedCards;
            playedCards.Clear();
            if (context.RoundActions is IList<PlayCardAction> roundActions)
            {
                // The engine always passes a List, so take the allocation-free indexed path.
                for (var i = 0; i < roundActions.Count; i++)
                {
                    var action = roundActions[i];
                    if (action.TrickNumber < context.CurrentTrickNumber)
                    {
                        playedCards.Add(action.Card);
                    }
                }
            }
            else
            {
                foreach (var action in context.RoundActions)
                {
                    if (action.TrickNumber < context.CurrentTrickNumber)
                    {
                        playedCards.Add(action.Card);
                    }
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

        private static void Consider(BidType bid, int points, ref BidType bestBid, ref int bestPoints)
        {
            if (points > bestPoints)
            {
                bestPoints = points;
                bestBid = bid;
            }
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

            if (TeammateHasSuitBid(previousBids, teammate))
            {
                // If the teammate has announced suit, increase all trump bid points
                bidPoints += 5;
            }

            return bidPoints;
        }

        private static bool TeammateHasSuitBid(IEnumerable<Bid> previousBids, PlayerPosition teammate)
        {
            // The engine always passes a List<Bid>, so take the allocation-free indexed path.
            if (previousBids is IList<Bid> bidsList)
            {
                for (var i = 0; i < bidsList.Count; i++)
                {
                    if (IsSuitBidByTeammate(bidsList[i], teammate))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var bid in previousBids)
            {
                if (IsSuitBidByTeammate(bid, teammate))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSuitBidByTeammate(Bid bid, PlayerPosition teammate)
        {
            return bid.Player == teammate && (bid.Type == BidType.Clubs || bid.Type == BidType.Diamonds
                                                                        || bid.Type == BidType.Hearts
                                                                        || bid.Type == BidType.Spades);
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
