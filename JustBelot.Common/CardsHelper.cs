using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustBelot.Common
{
    public static class CardsHelper
    {
        public static List<Card> GetFullCardDeck()
        {
            var cards = new List<Card>();
            foreach (CardColor cardColor in Enum.GetValues(typeof(CardColor)))
            {
                foreach (CardType cardType in Enum.GetValues(typeof(CardType)))
                {
                    cards.Add(new Card(cardType, cardColor));
                }
            }

            return cards;
        }
    }
}
