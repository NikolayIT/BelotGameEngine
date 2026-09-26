namespace Belot.UI.Game
{
    using Belot.Engine.Cards;

    /// <summary>The card images: <c>card_{rank}{suit}.png</c> (lowercase, as MAUI requires) and <c>card_back.png</c>.</summary>
    public static class CardImageProvider
    {
        public const string BackImage = "card_back.png";

        private static readonly string[] Ranks = { "seven", "eight", "nine", "ten", "jack", "queen", "king", "ace" };

        private static readonly string[] Suits = { "club", "diamond", "heart", "spade" };

        public static string For(Card card) => $"card_{Ranks[(int)card.Type]}{Suits[(int)card.Suit]}.png";
    }
}
