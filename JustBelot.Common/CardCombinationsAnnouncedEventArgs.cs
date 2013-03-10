using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JustBelot.Common
{
    public class CardCombinationsAnnouncedEventArgs
    {
        public CardCombinationsAnnouncedEventArgs(PlayerPosition position, IEnumerable<CardsCombinationType> cardCombinations)
        {
            this.Position = position;
            this.CardCombinations = cardCombinations;
        }

        public PlayerPosition Position { get; set; }

        public IEnumerable<CardsCombinationType> CardCombinations { get; set; }
    }
}
