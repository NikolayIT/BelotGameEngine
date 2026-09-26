namespace Belot.UI.Scaling
{
    using System;

    using Belot.UI.Game;

    /// <summary>
    /// A design size on the scrolling pages, scaled to the window:
    /// <c>FontSize="{ui:Size 13}"</c>, <c>Padding="{ui:Size '16,12'}"</c> (see <see cref="UiScale.Page"/>).
    /// </summary>
    [ContentProperty(nameof(Value))]
    public sealed class SizeExtension : IMarkupExtension<BindingBase>
    {
        public string Value { get; set; } = "0";

        /// <summary>A binding to a design size on the scrolling pages (for views built in code).</summary>
        public static BindingBase Bind(string value) =>
            new Binding(nameof(UiScale.Page), source: UiScale.Current, converter: ScaleConverter.Instance, converterParameter: value);

        public BindingBase ProvideValue(IServiceProvider serviceProvider) => Bind(this.Value);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this.ProvideValue(serviceProvider);
    }
}
