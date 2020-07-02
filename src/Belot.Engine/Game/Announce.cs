namespace Belot.Engine.Game
{
    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class Announce
    {
        /// <summary>
        /// Initializes a new announce.
        /// </summary>
        /// <param name="type">The type of the announce.</param>
        /// <param name="card">One of the cards from the announce. For Tierce, Quarte, Quinte the biggest card.</param>
        public Announce(AnnounceType type, Card card)
        {
            this.Type = type;
            this.Card = card;
        }

        public AnnounceType Type { get; }

        public Card Card { get; }

        public PlayerPosition PlayerPosition { get; internal set; }

        public int Value =>
            this.Type switch
                {
                    AnnounceType.Belot => 20,
                    AnnounceType.Tierce => 20,
                    AnnounceType.Quarte => 50,
                    AnnounceType.Quinte => 100,
                    AnnounceType.FourOfAKind => 100,
                    AnnounceType.FourNines => 150,
                    AnnounceType.FourJacks => 200,
                    _ => 0,
                };

        public override string ToString() =>
            this.Type switch
                {
                    AnnounceType.Belot => $"Belot {this.Card.Suit}",
                    AnnounceType.FourJacks => "4 Jacks",
                    AnnounceType.FourNines => "4 Nines",
                    AnnounceType.FourOfAKind => $"4 of a kind {this.Card.Type}",
                    AnnounceType.Quinte => $"Quinte to {this.Card}",
                    AnnounceType.Quarte => $"Tierce to {this.Card}",
                    AnnounceType.Tierce => $"Tierce to {this.Card}",
                    _ => string.Empty,
                };
    }
}
