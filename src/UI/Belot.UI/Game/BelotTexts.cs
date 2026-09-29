namespace Belot.UI.Game
{
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.UI.Localization;

    /// <summary>How the table names bids, contracts, cards and combinations, in the current language.</summary>
    public static class BelotTexts
    {
        public const string Red = "#D6453C";

        public const string Dark = "#1A1006";

        private static readonly string[] RankKeys =
        {
            "Rank_Seven", "Rank_Eight", "Rank_Nine", "Rank_Ten", "Rank_Jack", "Rank_Queen", "Rank_King", "Rank_Ace",
        };

        private static LocalizationManager Text => LocalizationManager.Instance;

        public static string SuitGlyph(CardSuit suit) => suit switch
        {
            CardSuit.Club => "♣",
            CardSuit.Diamond => "♦",
            CardSuit.Heart => "♥",
            _ => "♠",
        };

        public static bool IsRed(CardSuit suit) => suit == CardSuit.Diamond || suit == CardSuit.Heart;

        /// <summary>A card as "K♠" ("Р♠" in Bulgarian).</summary>
        public static string CardText(Card card) => Text[RankKeys[(int)card.Type]] + SuitGlyph(card.Suit);

        /// <summary>A card's spoken name, without relying on suit glyphs or abbreviated ranks.</summary>
        public static string CardDescription(Card card)
        {
            var suitKey = card.Suit switch
            {
                CardSuit.Club => "Bid_Clubs",
                CardSuit.Diamond => "Bid_Diamonds",
                CardSuit.Heart => "Bid_Hearts",
                _ => "Bid_Spades",
            };
            return Text.Format("Card_Format", Text[RankKeys[(int)card.Type].Replace("Rank_", "Card_")], Text[suitKey]);
        }

        /// <summary>A bid as said at the table: "♥ Hearts", "No trumps", "Double", "Pass".</summary>
        public static string BidText(BidType bid) => bid switch
        {
            BidType.Pass => Text["Bid_Pass"],
            BidType.Clubs => $"♣ {Text["Bid_Clubs"]}",
            BidType.Diamonds => $"♦ {Text["Bid_Diamonds"]}",
            BidType.Hearts => $"♥ {Text["Bid_Hearts"]}",
            BidType.Spades => $"♠ {Text["Bid_Spades"]}",
            BidType.NoTrumps => Text["Bid_NoTrumps"],
            BidType.AllTrumps => Text["Bid_AllTrumps"],
            BidType.Double => Text["Bid_Double"],
            BidType.ReDouble => Text["Bid_ReDouble"],
            _ => bid.ToString(),
        };

        /// <summary>A bid in a speech bubble: "♥", "NT", "AT", "Double", "Pass".</summary>
        public static string ShortBidText(BidType bid) => bid switch
        {
            BidType.Clubs => "♣",
            BidType.Diamonds => "♦",
            BidType.Hearts => "♥",
            BidType.Spades => "♠",
            BidType.NoTrumps => Text["Bid_NoTrumpsShort"],
            BidType.AllTrumps => Text["Bid_AllTrumpsShort"],
            BidType.Double => Text["Bid_DoubleShort"],
            BidType.ReDouble => Text["Bid_ReDoubleShort"],
            _ => Text["Bid_Pass"],
        };

        /// <summary>A contract: "♥ ×2", "No trumps", "All trumps ×4".</summary>
        public static string ContractText(BidType contract)
        {
            var plain = contract & ~(BidType.Double | BidType.ReDouble);
            var text = plain switch
            {
                BidType.Clubs or BidType.Diamonds or BidType.Hearts or BidType.Spades => BidText(plain),
                BidType.NoTrumps => Text["Bid_NoTrumps"],
                BidType.AllTrumps => Text["Bid_AllTrumps"],
                _ => Text["Bid_Pass"],
            };
            return contract.HasFlag(BidType.ReDouble) ? $"{text} ×4" : contract.HasFlag(BidType.Double) ? $"{text} ×2" : text;
        }

        /// <summary>The colour a contract is shown in: red for hearts and diamonds.</summary>
        public static string ContractColor(BidType contract) =>
            contract.HasFlag(BidType.Hearts) || contract.HasFlag(BidType.Diamonds) ? Red : Dark;

        /// <summary>A combination: "Tierce", "Quarte to K♠", "Four jacks", "Belote ♥".</summary>
        public static string CombinationText(AnnounceType type, Card? card)
        {
            var name = type switch
            {
                AnnounceType.Belot => Text["Ann_Belote"],
                AnnounceType.SequenceOf3 => Text["Ann_Tierce"],
                AnnounceType.SequenceOf4 => Text["Ann_Quarte"],
                AnnounceType.FourJacks => Text["Ann_FourJacks"],
                AnnounceType.FourNines => Text["Ann_FourNines"],
                AnnounceType.FourOfAKind => Text["Ann_Carre"],
                _ => Text["Ann_Quint"],
            };

            if (card == null || type == AnnounceType.FourJacks || type == AnnounceType.FourNines)
            {
                return name;
            }

            if (type == AnnounceType.Belot)
            {
                return $"{name} {SuitGlyph(card.Suit)}";
            }

            if (type == AnnounceType.FourOfAKind)
            {
                return Text.Format("Ann_CarreOf", Text[RankKeys[(int)card.Type] + "_Plural"]);
            }

            return Text.Format("Ann_To", name, CardText(card));
        }
    }
}
