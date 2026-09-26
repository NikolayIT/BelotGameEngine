namespace Belot.UI.Localization
{
    using System;

    using Microsoft.Maui.Controls;
    using Microsoft.Maui.Controls.Xaml;

    /// <summary>
    /// XAML markup extension for localized text: <c>Text="{loc:Tr Start_Play}"</c>.
    /// Returns a one-way binding to <see cref="LocalizationManager"/>'s string indexer so the text
    /// updates live when the language is switched.
    /// </summary>
    [ContentProperty(nameof(Key))]
    [AcceptEmptyServiceProvider]
    public sealed class TrExtension : IMarkupExtension<BindingBase>
    {
        public string Key { get; set; } = string.Empty;

        public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
            new Binding($"[{this.Key}]", BindingMode.OneWay, source: LocalizationManager.Instance);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
            ((IMarkupExtension<BindingBase>)this).ProvideValue(serviceProvider);
    }
}
