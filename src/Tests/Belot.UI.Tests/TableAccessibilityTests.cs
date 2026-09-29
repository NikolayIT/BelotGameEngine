namespace Belot.UI.Tests
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using Belot.UI.Game;
    using Belot.UI.Localization;

    using Xunit;

    [Collection(AppState.Name)]
    public class TableAccessibilityTests
    {
        public TableAccessibilityTests() => AppState.Reset();

        [Theory]
        [InlineData("en", "Queen of Hearts", "Face-down card")]
        [InlineData("bg", "Дама Купа", "Скрита карта")]
        public void CardDescriptionsShouldNameEveryVisibleCardWithoutRevealingHiddenCards(string language, string queen, string hidden)
        {
            LocalizationManager.Instance.SetLanguage(language);
            var descriptions = new HashSet<string>();
            foreach (var card in Card.AllCards)
            {
                var slot = new CardSlot(card);
                Assert.True(descriptions.Add(slot.Description));
                Assert.DoesNotContain("_", slot.Description);
                Assert.DoesNotContain(BelotTexts.SuitGlyph(card.Suit), slot.Description);
                slot.IsFaceDown = true;
                Assert.Equal(hidden, slot.Description);
            }

            Assert.Equal(queen, new CardSlot(Card.GetCard(CardSuit.Heart, CardType.Queen)).Description);
            Assert.Equal(hidden, new CardSlot(null).Description);
        }

        [Fact]
        public void CardDescriptionsShouldNotifyAndDescribeTheLegalAndHintStates()
        {
            var slot = new CardSlot(Card.GetCard(CardSuit.Spade, CardType.Ace));
            var changed = new List<string?>();
            slot.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            slot.IsPlayable = false;
            Assert.Contains(nameof(CardSlot.Description), changed);
            Assert.Contains(LocalizationManager.Instance["Card_NotPlayable"], slot.Description);

            changed.Clear();
            slot.IsPlayable = true;
            slot.IsHinted = true;
            Assert.Contains(nameof(CardSlot.Description), changed);
            Assert.Contains(LocalizationManager.Instance["Card_Hinted"], slot.Description);

            changed.Clear();
            slot.IsFaceDown = true;
            Assert.Contains(nameof(CardSlot.Description), changed);
            Assert.Equal(LocalizationManager.Instance["Card_FaceDown"], slot.Description);
        }

        [Theory]
        [InlineData("en", "Cards remaining: 5")]
        [InlineData("bg", "Оставащи карти: 5")]
        public void OpponentCardCountsShouldNotifyAndSpeakOnlyThePublicCount(string language, string description)
        {
            LocalizationManager.Instance.SetLanguage(language);
            var seat = new SeatViewModel(PlayerPosition.North);
            var changed = new List<string?>();
            seat.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            seat.CardCount = 5;

            Assert.Equal(description, seat.CardsDescription);
            Assert.Contains(nameof(SeatViewModel.CardsDescription), changed);
            Assert.True(seat.HasCards);
            seat.CardCount = 0;
            Assert.False(seat.HasCards);
        }

        [Fact]
        public void DeclarationDescriptionsShouldExposeAndNotifyTheSelectionState()
        {
            var option = new AnnounceOption(new BelotAnnounce { Type = AnnounceType.SequenceOf3, Card = Card.GetCard(CardSuit.Club, CardType.Ace) });
            Assert.Contains(option.Text, option.Description);
            Assert.Contains(LocalizationManager.Instance["Common_NotSelected"], option.Description);
            var changed = new List<string?>();
            option.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            option.IsSelected = true;

            Assert.Contains(nameof(AnnounceOption.Description), changed);
            Assert.Contains(LocalizationManager.Instance["Common_Selected"], option.Description);
        }
    }
}
