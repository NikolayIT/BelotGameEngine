namespace Belot.UI.Tests
{
    using System.Collections.Generic;

    using Belot.Engine.Players;
    using Belot.UI.Game;

    using Xunit;

    public class SeatPresentationTests
    {
        [Fact]
        public void DeclarationsShouldReplaceTheSpeechBubbleWithoutHidingThePersistentSummary()
        {
            var seat = new SeatViewModel(PlayerPosition.North) { BubbleText = "Tierce" };
            Assert.True(seat.HasBubble);
            var changed = new List<string?>();
            seat.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            seat.DeclaredText = "Tierce";

            Assert.False(seat.HasBubble);
            Assert.True(seat.HasDeclared);
            Assert.Equal("Tierce", seat.DeclaredText);
            Assert.Contains(nameof(SeatViewModel.HasBubble), changed);
            seat.DeclaredText = "Tierce, Belote";
            Assert.False(seat.HasBubble);
        }

        [Fact]
        public void TheNextAuctionShouldShowBidsAfterTheOldDeclarationsAreCleared()
        {
            var seat = new SeatViewModel(PlayerPosition.East) { BubbleText = "Tierce", DeclaredText = "Tierce" };
            seat.BubbleText = string.Empty;
            seat.DeclaredText = string.Empty;
            Assert.False(seat.HasBubble);

            seat.BubbleText = "Pass";

            Assert.True(seat.HasBubble);
            Assert.False(seat.HasDeclared);
        }
    }
}
