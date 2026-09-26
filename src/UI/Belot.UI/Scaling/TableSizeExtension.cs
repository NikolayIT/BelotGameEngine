namespace Belot.UI.Scaling
{
    using System;

    using Belot.UI.Game;

    /// <summary>
    /// A design size on the game table, scaled so the whole table fits the window:
    /// <c>FontSize="{ui:TableSize 13}"</c>, <c>Padding="{ui:TableSize '10,4'}"</c> (see <see cref="UiScale.Table"/>).
    /// </summary>
    [ContentProperty(nameof(Value))]
    public sealed class TableSizeExtension : IMarkupExtension<BindingBase>
    {
        public string Value { get; set; } = "0";

        public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
            new Binding(nameof(UiScale.Table), source: UiScale.Current, converter: ScaleConverter.Instance, converterParameter: this.Value);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => this.ProvideValue(serviceProvider);
    }
}
