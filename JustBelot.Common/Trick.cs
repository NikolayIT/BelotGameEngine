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
                var firstCard = this[0];
                var bestCard = firstCard;
                var bestPlayer = this.FirstPlayer;
                var currentPlayer = this.FirstPlayer;

                if (this.Contract.Type == ContractType.AllTrumps)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        currentPlayer = currentPlayer.NextPosition();
                        if (this[i].Suit == firstCard.Suit && this[i].Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps())
                        {
                            bestCard = this[i];
                            bestPlayer = currentPlayer;
                        }
                    }
                }
                else if (this.Contract.Type == ContractType.NoTrumps)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        currentPlayer = currentPlayer.NextPosition();
                        if (this[i].Suit == firstCard.Suit && this[i].Type.GetOrderForNoTrumps() > bestCard.Type.GetOrderForNoTrumps())
                        {
                            bestCard = this[i];
                            bestPlayer = currentPlayer;
                        }
                    }
                }
                else
                {
                    if (this.Any(x => x.Suit == this.Contract.Type.ToCardSuit()))
                    {
                        // Trump in the trick cards
                        for (int i = 1; i < 4; i++)
                        {
                            currentPlayer = currentPlayer.NextPosition();
                            if (this[i].Suit == this.Contract.Type.ToCardSuit() && this[i].Type.GetOrderForAllTrumps() > bestCard.Type.GetOrderForAllTrumps())
                            {
                                bestCard = this[i];
                                bestPlayer = currentPlayer;
                            }
                        }
                    }
                    else
                    {
                        // No trick in the cards
                        for (int i = 1; i < 4; i++)
                        {
                            currentPlayer = currentPlayer.NextPosition();
                            if (this[i].Suit == firstCard.Suit && this[i].Type.GetOrderForNoTrumps() > bestCard.Type.GetOrderForNoTrumps())
                            {
                                bestCard = this[i];
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
