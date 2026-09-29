namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Android.App;
    using Android.Content.PM;
    using Android.Content.Res;

    using Microsoft.Maui;

    using Xunit;

    // The production activity source is linked into this test assembly with minimal platform
    // doubles. APK/device checks separately cover Android's real SP conversion and resources.
    [Collection(AppState.Name)]
    public class AndroidConfigurationTests
    {
        [Fact]
        public void FontScaleChangesAreHandledWithoutRecreatingTheActivity()
        {
            var activity = typeof(MainActivity).GetCustomAttribute<ActivityAttribute>()!;
            Assert.True(activity.ConfigurationChanges.HasFlag(ConfigChanges.FontScale));
            Assert.True(activity.ConfigurationChanges.HasFlag(ConfigChanges.Density));
            Assert.True(activity.ConfigurationChanges.HasFlag(ConfigChanges.ScreenSize));
        }

        [Theory]
        [InlineData(0.85f)]
        [InlineData(1.3f)]
        [InlineData(2f)]
        public void ConfigurationChangesRefreshExistingNestedTextAndLayoutAfterTheBaseCallback(float scale)
        {
            var application = new Microsoft.Maui.Controls.Application();
            Microsoft.Maui.Controls.Application.Current = application;
            try
            {
                var activity = new MainActivity();
                var configuration = new Configuration { FontScale = scale };
                var calls = new List<string>();
                var state = new object();
                var table = new TestView("table", calls) { State = state };
                var label = new TextView("label", calls);
                var button = new TextView("button", calls);
                var detachedText = new TextView("detached", calls);
                var handler = new FontHandler(() => Assert.Same(configuration, activity.LastConfiguration));
                label.Handler = handler;
                button.Handler = handler;
                table.Children.Add(label);
                table.Children.Add(button);
                table.Children.Add(detachedText);
                var window = new TestView("window", calls);
                window.Children.Add(table);
                application.Windows.Add(window);

                activity.OnConfigurationChanged(configuration);

                Assert.Same(window, Assert.Single(application.Windows));
                Assert.Same(table, Assert.Single(window.Children));
                Assert.Same(state, table.State);
                Assert.Equal(new[] { "Font", "Font" }, handler.Properties);
                Assert.Equal(new[] { "label", "button", "detached", "table", "window" }, calls);
                Assert.Equal(scale, configuration.FontScale);
            }
            finally
            {
                Microsoft.Maui.Controls.Application.Current = null;
            }
        }

        [Fact]
        public void ConfigurationBeforeTheMauiWindowExistsStillCallsThePlatform()
        {
            Microsoft.Maui.Controls.Application.Current = null;
            var activity = new MainActivity();
            var configuration = new Configuration { FontScale = 1.3f };
            activity.OnConfigurationChanged(configuration);
            Assert.Same(configuration, activity.LastConfiguration);
        }

        private class TestView : IVisualTreeElement, IView
        {
            private readonly string name;

            private readonly List<string> calls;

            public TestView(string name, List<string> calls)
            {
                this.name = name;
                this.calls = calls;
            }

            public List<IVisualTreeElement> Children { get; } = new();

            public object? State { get; set; }

            public IElementHandler? Handler { get; set; }

            public IReadOnlyList<IVisualTreeElement> GetVisualChildren() => this.Children;

            public void InvalidateMeasure() => this.calls.Add(this.name);
        }

        private sealed class TextView : TestView, ITextStyle
        {
            public TextView(string name, List<string> calls)
                : base(name, calls)
            {
            }

            public object Font { get; } = new();
        }

        private sealed class FontHandler : IElementHandler
        {
            private readonly Action checkConfiguration;

            public FontHandler(Action checkConfiguration) => this.checkConfiguration = checkConfiguration;

            public List<string> Properties { get; } = new();

            public void UpdateValue(string property)
            {
                this.checkConfiguration();
                this.Properties.Add(property);
            }
        }
    }
}
