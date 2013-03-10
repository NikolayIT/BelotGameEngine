namespace JustBelot.Common
{
    using System.Linq;

    using JustBelot.Common.Extensions;

    public class Trick : CardsCollection
    {
        public Trick(Contract contract, PlayerPosition firstPlayer)
        {
            this.Contract = contract;
            this.FirstPlayer = firstPlayer;
        }

        public PlayerPosition WinnerPlayer
        {
            get
            {
                if (cards.Count == 0)
                {
                    return this.FirstPlayer;
                }

                var firstCard = cards[0];
                var bestCard = firstCard;
                var bestPlayer = this.FirstPlayer;
                var currentPlayer = this.FirstPlayer;

                if (this.Contract.Type == ContractType.AllTrumps)
                {
                    for (int i = 1; i < cards.Count; i++)
                    {
                        currentPlayer = currentPlayer.NextPosition();
                        if (cards[i].Suit == firstCard.Suit && cards[i].Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps())
                        {
                            bestCard = cards[i];
                            bestPlayer = currentPlayer;
                        }
                    }
                }
                else if (this.Contract.Type == ContractType.NoTrumps)
                {
                    for (int i = 1; i < cards.Count; i++)
                    {
                        currentPlayer = currentPlayer.NextPosition();
                        if (cards[i].Suit == firstCard.Suit && cards[i].Type.GetOrderForNoTrumps() > bestCard.Type.GetOrderForNoTrumps())
                        {
                            bestCard = this.cards[i];
                            bestPlayer = currentPlayer;
                        }
                    }
                }
                else
                {
                    if (cards.Any(x => x.Suit == this.Contract.Type.ToCardSuit()))
                    {
                        // Trump in the trick cards
                        for (int i = 1; i < cards.Count; i++)
                        {
                            currentPlayer = currentPlayer.NextPosition();
                            if (cards[i].Suit == this.Contract.Type.ToCardSuit() && cards[i].Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps())
                            {
                                bestCard = this.cards[i];
                                bestPlayer = currentPlayer;
                            }
                        }
                    }
                    else
                    {
                        // No trick in the cards
                        for (int i = 1; i < cards.Count; i++)
                        {
                            currentPlayer = currentPlayer.NextPosition();
                            if (cards[i].Suit == firstCard.Suit && cards[i].Type.GetOrderForNoTrumps() > bestCard.Type.GetOrderForNoTrumps())
                            {
                                bestCard = this.cards[i];
                                bestPlayer = currentPlayer;
                            }
                        }
                    }
                }

                return bestPlayer;
            }
        }

        public Contract Contract { get; private set; }

        public PlayerPosition FirstPlayer { get; private set; }
    }
}
