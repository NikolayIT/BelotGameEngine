namespace Belot.Engine.Game
{
    using System;

    using Belot.Engine.Cards;
    using Belot.Engine.Players;

    public class Announce : IComparable<Announce>
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

        public PlayerPosition Player { get; internal set; }

        public int Value =>
            this.Type switch
                {
                    AnnounceType.Belot => 20,
                    AnnounceType.SequenceOf3 => 20,
                    AnnounceType.SequenceOf4 => 50,
                    AnnounceType.SequenceOf5 => 100,
                    AnnounceType.SequenceOf6 => 100,
                    AnnounceType.SequenceOf7 => 100,
                    AnnounceType.SequenceOf8 => 100,
                    AnnounceType.FourOfAKind => 100,
                    AnnounceType.FourNines => 150,
                    AnnounceType.FourJacks => 200,
                    _ => 0,
                };

        internal Card Card { get; }

        internal bool? ToBeScored { get; set; }

        public override string ToString() =>
            this.Type switch
                {
                    AnnounceType.Belot => $"Belot {this.Card.Suit}",
                    AnnounceType.FourJacks => "4 Jacks",
                    AnnounceType.FourNines => "4 Nines",
                    AnnounceType.FourOfAKind => $"4 of a kind {this.Card.Type}",
                    AnnounceType.SequenceOf8 => $"Quinte(8) to {this.Card}",
                    AnnounceType.SequenceOf7 => $"Quinte(7) to {this.Card}",
                    AnnounceType.SequenceOf6 => $"Quinte(6) to {this.Card}",
                    AnnounceType.SequenceOf5 => $"Quinte to {this.Card}",
                    AnnounceType.SequenceOf4 => $"Quarte to {this.Card}",
                    AnnounceType.SequenceOf3 => $"Tierce to {this.Card}",
                    _ => throw new BelotGameException($"Invalid announce type {this.Type} ({(int)this.Type})"),
                };

        public int CompareTo(Announce other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            if (this.Value > other.Value)
            {
                return 1;
            }

            if (other.Value > this.Value)
            {
                return -1;
            }

            if (this.Type > other.Type)
            {
                return 1;
            }

            if (other.Type > this.Type)
            {
                return -1;
            }

            return this.Card.Type.CompareTo(other.Card.Type);
        }
    }
}
