// Minimal platform contracts for compiling the real MainActivity callback in the desktop tests.
// They model callback ordering and handler invalidation, not Android resource scaling itself.
#pragma warning disable SA1402, SA1403, SA1649 // Keep the small platform doubles together, outside production.

namespace Android.Content.PM
{
    using System;

    [Flags]
    public enum ConfigChanges
    {
        ScreenSize = 1,
        Orientation = 2,
        UiMode = 4,
        ScreenLayout = 8,
        SmallestScreenSize = 16,
        Density = 32,
        FontScale = 64,
    }

    public enum LaunchMode
    {
        SingleTop,
    }

    public enum ScreenOrientation
    {
        SensorPortrait,
    }
}

namespace Android.App
{
    using System;

    using Android.Content.PM;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ActivityAttribute : Attribute
    {
        public string Theme { get; set; } = string.Empty;

        public bool MainLauncher { get; set; }

        public LaunchMode LaunchMode { get; set; }

        public ScreenOrientation ScreenOrientation { get; set; }

        public ConfigChanges ConfigurationChanges { get; set; }
    }
}

namespace Android.Content.Res
{
    public sealed class Configuration
    {
        public float FontScale { get; set; }
    }
}

namespace Microsoft.Maui
{
    using System.Collections.Generic;

    using Android.Content.Res;

    public interface IVisualTreeElement
    {
        IReadOnlyList<IVisualTreeElement> GetVisualChildren();
    }

    public interface IElement
    {
        IElementHandler? Handler { get; set; }
    }

    public interface IElementHandler
    {
        void UpdateValue(string property);
    }

    public interface ITextStyle
    {
        object Font { get; }
    }

    public interface IView : IElement
    {
        void InvalidateMeasure();
    }

    public class MauiAppCompatActivity
    {
        public Configuration? LastConfiguration { get; private set; }

        public virtual void OnConfigurationChanged(Configuration newConfig) => this.LastConfiguration = newConfig;
    }
}

namespace Microsoft.Maui.Controls
{
    using System.Collections.Generic;

    public sealed class Application
    {
        public static Application? Current { get; set; }

        public IList<IVisualTreeElement> Windows { get; } = new List<IVisualTreeElement>();
    }
}
