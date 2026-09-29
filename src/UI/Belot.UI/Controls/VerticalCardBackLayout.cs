namespace Belot.UI.Controls
{
    using System;
    using System.Linq;

    using Belot.UI.Game;

    using Microsoft.Maui;
    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Graphics;
    using Microsoft.Maui.Layouts;

    /// <summary>Keeps every side-seat card back inside the space left below its labels.</summary>
    public sealed class VerticalCardBackLayout : Layout
    {
        public static readonly BindableProperty CardScaleProperty = BindableProperty.Create(
            nameof(CardScale),
            typeof(double),
            typeof(VerticalCardBackLayout),
            1.0,
            propertyChanged: OnCardScaleChanged);

        public double CardScale
        {
            get => (double)this.GetValue(CardScaleProperty);
            set => this.SetValue(CardScaleProperty, value);
        }

        protected override ILayoutManager CreateLayoutManager() => new CardBackLayoutManager(this);

        private static void OnCardScaleChanged(BindableObject bindable, object oldValue, object newValue) =>
            ((VerticalCardBackLayout)bindable).InvalidateMeasure();

        private sealed class CardBackLayoutManager : LayoutManager
        {
            private readonly VerticalCardBackLayout layout;

            public CardBackLayoutManager(VerticalCardBackLayout layout)
                : base(layout)
            {
                this.layout = layout;
            }

            public override Size Measure(double widthConstraint, double heightConstraint)
            {
                var padding = this.layout.Padding;
                var children = this.VisibleChildren();
                var geometry = VerticalCardBackGeometry.Fit(
                    children.Length,
                    widthConstraint - padding.HorizontalThickness,
                    heightConstraint - padding.VerticalThickness,
                    this.layout.CardScale);
                foreach (var child in children)
                {
                    child.Measure(geometry.CardWidth, geometry.CardHeight);
                }

                return new Size(geometry.CardWidth + padding.HorizontalThickness, geometry.Height + padding.VerticalThickness);
            }

            public override Size ArrangeChildren(Rect bounds)
            {
                var padding = this.layout.Padding;
                var children = this.VisibleChildren();
                var width = Math.Max(0, bounds.Width - padding.HorizontalThickness);
                var geometry = VerticalCardBackGeometry.Fit(
                    children.Length,
                    width,
                    bounds.Height - padding.VerticalThickness,
                    this.layout.CardScale);
                var left = bounds.Left + padding.Left + ((width - geometry.CardWidth) / 2);
                var top = bounds.Top + padding.Top;
                for (var index = 0; index < children.Length; index++)
                {
                    // Grid can arrange less space than an earlier speculative measure offered.
                    // Re-measure images to their final aspect-preserving boxes before arranging.
                    children[index].Measure(geometry.CardWidth, geometry.CardHeight);
                    children[index].Arrange(new Rect(left, top + (index * geometry.Step), geometry.CardWidth, geometry.CardHeight));
                }

                return bounds.Size;
            }

            private IView[] VisibleChildren() => this.layout.Where(child => child.Visibility != Visibility.Collapsed).ToArray();
        }
    }
}
