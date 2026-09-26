namespace Belot.AI.ClaudePlayer
{
    using Belot.Engine.Cards;

    /// <summary>
    /// A legal card and what it is worth: the game points the player's team gets from the deal
    /// minus the other team's, if the player plays it (see <see cref="ClaudePlayerNeural.EvaluateCards"/>).
    /// </summary>
    public readonly struct CardValue
    {
        public CardValue(Card card, double value)
        {
            this.Card = card;
            this.Value = value;
        }

        public Card Card { get; }

        public double Value { get; }

        public override string ToString() => $"{this.Card}: {this.Value:0.00}";
    }
}
