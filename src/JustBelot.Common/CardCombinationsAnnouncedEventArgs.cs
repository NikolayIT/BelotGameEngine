namespace JustBelot.Common
{
    using System.Collections.Generic;

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
