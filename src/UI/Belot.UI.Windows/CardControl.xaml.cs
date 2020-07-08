namespace Belot.UI.Windows
{
    using System;

    using Belot.Engine.Cards;

    using global::Windows.UI;
    using global::Windows.UI.Xaml;
    using global::Windows.UI.Xaml.Controls;
    using global::Windows.UI.Xaml.Media;
    using global::Windows.UI.Xaml.Media.Imaging;

    public sealed partial class CardControl : UserControl
    {
        public CardControl()
        {
            this.InitializeComponent();
        }

        public CardControl(Card card)
            : this()
        {
            this.SetCard(card);
        }

        public Card Card { get; private set; }

        public void SetCard(Card card)
        {
            this.Card = card;
            this.Image.Source = ImageFromRelativePath(
                this,
                card != null ? $"Assets/Cards/{this.Card.Type.ToString().ToLower()}{this.Card.Suit.ToString().ToLower()}.png" : "Assets/Cards/back.png");
            this.Enable();
        }

        public void Enable()
        {
            this.Image.Opacity = 1;
        }

        public void Disable()
        {
            this.Image.Opacity = 0.3;
            this.Background = new SolidColorBrush(Colors.Black);
        }

        // http://stackoverflow.com/questions/11814917/how-to-reference-image-source-files-that-are-packaged-with-my-metro-style-app
        private static BitmapImage ImageFromRelativePath(FrameworkElement parent, string path)
        {
            var uri = new Uri(parent.BaseUri, path);
            var bmp = new BitmapImage { UriSource = uri };
            return bmp;
        }
    }
}
