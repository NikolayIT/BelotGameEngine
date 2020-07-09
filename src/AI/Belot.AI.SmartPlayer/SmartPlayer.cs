namespace Belot.AI.SmartPlayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    public class SmartPlayer : IPlayer
    {
        private readonly TrickWinnerService trickWinnerService;

        public SmartPlayer()
        {
            this.trickWinnerService = new TrickWinnerService();
        }

        public BidType GetBid(PlayerGetBidContext context)
        {
            if (context.AvailableBids.HasFlag(BidType.Clubs)
                && CalculateTrumpBidPoints(context.MyCards, CardSuit.Club) >= 100)
            {
                return BidType.Clubs;
            }

            if (context.AvailableBids.HasFlag(BidType.Diamonds)
                && CalculateTrumpBidPoints(context.MyCards, CardSuit.Diamond) >= 100)
            {
                return BidType.Diamonds;
            }

            if (context.AvailableBids.HasFlag(BidType.Hearts)
                && CalculateTrumpBidPoints(context.MyCards, CardSuit.Heart) >= 100)
            {
                return BidType.Hearts;
            }

            if (context.AvailableBids.HasFlag(BidType.Spades)
                && CalculateTrumpBidPoints(context.MyCards, CardSuit.Spade) >= 100)
            {
                return BidType.Spades;
            }

            if (context.AvailableBids.HasFlag(BidType.AllTrumps) && CalculateAllTrumpBidPoints(context.MyCards) >= 100)
            {
                return BidType.AllTrumps;
            }

            if (context.AvailableBids.HasFlag(BidType.NoTrumps) && CalculateNoTrumpBidPoints(context.MyCards) >= 100)
            {
                return BidType.NoTrumps;
            }

            return BidType.Pass;
        }

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context)
        {
            return context.AvailableAnnounces;
        }

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            return new PlayCardAction(
                context.AvailableCardsToPlay.OrderBy(x => x.GetValue(context.CurrentContract.Type))
                    .FirstOrDefault());
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
                    bidPoints += 30;
                }

                if (card.Type == CardType.Nine)
                {
                    bidPoints += 15;
                }

                if (card.Type == CardType.Ace)
                {
                    bidPoints += 5;
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
                    bidPoints += 15;
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

            return bidPoints;
        }
    }
}
