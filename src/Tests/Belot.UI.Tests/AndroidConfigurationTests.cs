namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Android.App;
    using Android.Content.PM;
    using Android.Content.Res;

    using Belot.UI.Game;

    using Microsoft.Maui;

    using Xunit;

    // The production activity source is linked into this test assembly with minimal platform
    // doubles. APK/device checks separately cover Android's real SP conversion and resources.
    [Collection(AppState.Name)]
    public class AndroidConfigurationTests
    {
        public AndroidConfigurationTests() => UiScale.Current.UpdateSystemFontScale(1);

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
                var handler = new FontHandler(() =>
                {
                    Assert.Same(configuration, activity.LastConfiguration);
                    Assert.Equal((double)scale, UiScale.Current.SystemFontScale);
                    Assert.Equal(UiScale.Current.Table * Math.Min(scale, UiScale.MaximumTableTextScale), UiScale.Current.TableText, 10);
                });
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
                UiScale.Current.UpdateSystemFontScale(1);
            }
        }

        [Theory]
        [InlineData(0.85f)]
        [InlineData(2f)]
        public void ConfigurationBeforeTheMauiWindowExistsPublishesTheScaleAfterThePlatformCallback(float scale)
        {
            Microsoft.Maui.Controls.Application.Current = null;
            var activity = new MainActivity();
            var configuration = new Configuration { FontScale = scale };
            var published = false;
            UiScale.Current.PropertyChanged += Changed;
            try
            {
                activity.OnConfigurationChanged(configuration);
                Assert.Same(configuration, activity.LastConfiguration);
                Assert.Equal((double)scale, UiScale.Current.SystemFontScale);
                Assert.True(published);
            }
            finally
            {
                UiScale.Current.PropertyChanged -= Changed;
                UiScale.Current.UpdateSystemFontScale(1);
            }

            void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(UiScale.SystemFontScale))
                {
                    Assert.Same(configuration, activity.LastConfiguration);
                    published = true;
                }
            }
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void InvalidPlatformFontScalesShouldLeaveUsableTableText(float invalid)
        {
            Microsoft.Maui.Controls.Application.Current = null;
            UiScale.Current.UpdateSystemFontScale(1.3);
            var activity = new MainActivity();
            var configuration = new Configuration { FontScale = invalid };

            activity.OnConfigurationChanged(configuration);

            Assert.Same(configuration, activity.LastConfiguration);
            Assert.Equal(1, UiScale.Current.SystemFontScale);
            Assert.Equal(UiScale.Current.Table, UiScale.Current.TableText);
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
