namespace Belot.UI.Scaling
{
    using Belot.UI.Game;

    /// <summary>
    /// Design sizes scaled for the views the scrolling pages build in code (the XAML uses
    /// <c>{ui:Size n}</c>). The values are taken when a view is built, so those pages rebuild their
    /// lists when <see cref="UiScale.Page"/> changes.
    /// </summary>
    public static class Ui
    {
        public static double Size(double design) => design * UiScale.Current.Page;

        public static Thickness Pad(double uniform) => new(Size(uniform));

        public static Thickness Pad(double horizontal, double vertical) => new(Size(horizontal), Size(vertical));

        public static Thickness Pad(double left, double top, double right, double bottom) =>
            new(Size(left), Size(top), Size(right), Size(bottom));
    }
}
