namespace Belot.UI.Scaling
{
    using System;

    using Belot.UI.Game;

    /// <summary>
    /// A design font size on the fixed game board, including bounded system font scaling.
    /// Pair with <c>FontAutoScalingEnabled="False"</c> to apply the system factor exactly once.
    /// Scrolling pages and result overlays keep ordinary <see cref="SizeExtension"/> or
    /// <see cref="TableSizeExtension"/> sizing and native font scaling.
    /// </summary>
    [ContentProperty(nameof(Value))]
    public sealed class TableTextSizeExtension : IMarkupExtension<BindingBase>
    {
        public string Value { get; set; } = "0";

        public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
            new Binding(nameof(UiScale.TableText), source: UiScale.Current, converter: ScaleConverter.Instance, converterParameter: this.Value);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this.ProvideValue(serviceProvider);
    }
}
