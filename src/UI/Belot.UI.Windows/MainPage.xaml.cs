namespace Belot.UI.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Belot.AI.SmartPlayer;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using global::Windows.UI.Core;
    using global::Windows.UI.Xaml;
    using global::Windows.UI.Xaml.Controls;
    using global::Windows.UI.Xaml.Input;

    public sealed partial class MainPage : Page
    {
        private readonly UiPlayer uiPlayer = new UiPlayer();
        private readonly OpenCardsPlayerDecorator eastPlayer = new OpenCardsPlayerDecorator(new SmartPlayer());
        private readonly OpenCardsPlayerDecorator northPlayer = new OpenCardsPlayerDecorator(new SmartPlayer());
        private readonly OpenCardsPlayerDecorator westPlayer = new OpenCardsPlayerDecorator(new SmartPlayer());
        private readonly BelotGame game;

        private readonly ValidAnnouncesService validAnnouncesService;

        public MainPage()
        {
            this.InitializeComponent();
            this.game = new BelotGame(this.uiPlayer, this.eastPlayer, this.northPlayer, this.westPlayer);
            this.uiPlayer.InfoChangedInGetBid += this.UiPlayerOnInfoChangedInGetBid;
            this.uiPlayer.InfoChangedInGetAnnounces += this.UiPlayerOnInfoChangedInGetAnnounces;
            this.uiPlayer.InfoChangedInPlayCard += this.UiPlayerOnInfoChangedInPlayCard;
            Task.Run(() => this.game.PlayGame(PlayerPosition.East));
            this.ProgramVersion.Text = "Belot v1.0";
        }

        private async void UiPlayerOnInfoChangedInGetBid(object sender, PlayerGetBidContext e)
        {
            await this.UpdateBaseInfo(e);
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.High,
                () =>
                    {
                        this.BidsPanel.Children.Clear();
                        foreach (BidType bidType in Enum.GetValues(typeof(BidType)))
                        {
                            var button = new Button
                                             {
                                                 Content = bidType.ToString(),
                                                 IsEnabled = e.AvailableBids.HasFlag(bidType),
                                                 Tag = bidType
                                             };
                            button.Tapped += this.BidTapped;
                            this.BidsPanel.Children.Add(button);
                        }

                        this.BidsPanel.Visibility = Visibility.Visible;
                    });
            await this.UpdateCurrentTrickActions(new List<PlayCardAction>());
        }

        private async void UiPlayerOnInfoChangedInGetAnnounces(object sender, PlayerGetAnnouncesContext e)
        {
            await this.UpdateBaseInfo(e);
            await this.UpdateCurrentTrickActions(e.CurrentTrickActions);
        }

        private async void UiPlayerOnInfoChangedInPlayCard(object sender, PlayerPlayCardContext e)
        {
            await this.UpdateBaseInfo(e, e.AvailableCardsToPlay);
            await this.UpdateCurrentTrickActions(e.CurrentTrickActions);
        }

        private async Task UpdateCurrentTrickActions(IEnumerable<PlayCardAction> currentTrickActions)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.High,
                () =>
                    {
                        this.southCardPlayed.Visibility = Visibility.Collapsed;
                        this.eastCardPlayed.Visibility = Visibility.Collapsed;
                        this.northCardPlayed.Visibility = Visibility.Collapsed;
                        this.westCardPlayed.Visibility = Visibility.Collapsed;
                        foreach (var trickAction in currentTrickActions)
                        {
                            switch (trickAction.Player)
                            {
                                case PlayerPosition.South:
                                    this.southCardPlayed.SetCard(trickAction.Card);
                                    this.southCardPlayed.Visibility = Visibility.Visible;
                                    break;
                                case PlayerPosition.East:
                                    this.eastCardPlayed.SetCard(trickAction.Card);
                                    this.eastCardPlayed.Visibility = Visibility.Visible;
                                    break;
                                case PlayerPosition.North:
                                    this.northCardPlayed.SetCard(trickAction.Card);
                                    this.northCardPlayed.Visibility = Visibility.Visible;
                                    break;
                                case PlayerPosition.West:
                                    this.westCardPlayed.SetCard(trickAction.Card);
                                    this.westCardPlayed.Visibility = Visibility.Visible;
                                    break;
                            }
                        }
                    });
        }

        private async Task UpdateBaseInfo(BasePlayerContext basePlayerContext, CardCollection availableCards = null)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.High,
                () =>
                    {
                        this.ProgramVersion.Text = basePlayerContext.CurrentContract.ToString();
                        this.TotalResult.Text =
                            $"{basePlayerContext.SouthNorthPoints} - {basePlayerContext.EastWestPoints}";

                        this.UpdateOtherPlayerCards();

                        // South player cards
                        this.SouthCardsPanel.Children.Clear();
                        var playerCards = basePlayerContext.MyCards.ToList();
                        for (var i = 0; i < playerCards.Count; i++)
                        {
                            var cardControl = new CardControl { Margin = new Thickness(i == 0 ? 0 : -50, 0, 0, 0) };
                            cardControl.Tapped += this.PlayerCardTapped;
                            cardControl.SetCard(playerCards[i]);
                            if (availableCards != null && !availableCards.Contains(playerCards[i]))
                            {
                                cardControl.Disable();
                            }
                            else
                            {
                                cardControl.Enable();
                            }

                            this.SouthCardsPanel.Children.Add(cardControl);
                        }
                    });
        }

        private void UpdateOtherPlayerCards()
        {
            // East player cards
            this.EastCardsPanel.Children.Clear();
            for (var i = 0; i < this.eastPlayer.Cards.ToList().Count; i++)
            {
                var cardControl = new CardControl { Margin = new Thickness(0, i == 0 ? 0 : -80, 0, 0), Width = 100 };
                if (this.OpenCardsCheckBox.IsChecked == true)
                {
                    var card = this.eastPlayer.Cards.ToList()[i];
                    cardControl.SetCard(card);
                }

                this.EastCardsPanel.Children.Add(cardControl);
            }

            // North player cards
            this.NorthCardsPanel.Children.Clear();
            for (var i = 0; i < this.northPlayer.Cards.ToList().Count; i++)
            {
                var cardControl = new CardControl { Margin = new Thickness(i == 0 ? 0 : -50, 0, 0, 0), Width = 100 };
                if (this.OpenCardsCheckBox.IsChecked == true)
                {
                    var card = this.northPlayer.Cards.ToList()[i];
                    cardControl.SetCard(card);
                }

                this.NorthCardsPanel.Children.Add(cardControl);
            }

            // West player cards
            this.WestCardPanel.Children.Clear();
            for (var i = 0; i < this.westPlayer.Cards.ToList().Count; i++)
            {
                var cardControl = new CardControl { Margin = new Thickness(0, i == 0 ? 0 : -80, 0, 0), Width = 100 };
                if (this.OpenCardsCheckBox.IsChecked == true)
                {
                    var card = this.westPlayer.Cards.ToList()[i];
                    cardControl.SetCard(card);
                }

                this.WestCardPanel.Children.Add(cardControl);
            }
        }

        private void PlayerCardTapped(object sender, TappedRoutedEventArgs eventArgs)
        {
            // TODO: Ask for belot - var dialog = new MessageDialog(content, title);
            this.uiPlayer.PlayCardAction = new PlayCardAction((sender as CardControl)?.Card);
        }

        private void BidTapped(object sender, TappedRoutedEventArgs eventArgs)
        {
            this.BidsPanel.Visibility = Visibility.Collapsed;
            this.uiPlayer.GetBidAction = ((sender as Button)?.Tag as BidType?) ?? BidType.Pass;
        }

        private async void OpenCardsCheckBoxTapped(object sender, TappedRoutedEventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.High, this.UpdateOtherPlayerCards);
        }
    }
}
