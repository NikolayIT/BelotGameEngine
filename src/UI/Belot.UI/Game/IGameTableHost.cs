namespace Belot.UI.Game
{
    using System;

    /// <summary>
    /// What the game table (<see cref="GameViewModel"/>) needs from the page around it: timers on
    /// the UI thread, the device's vibration and leaving the page. The game page implements it;
    /// the UI tests use a fake, so no MAUI types here.
    /// </summary>
    public interface IGameTableHost
    {
        /// <summary>Runs <paramref name="action"/> on the UI thread after <paramref name="delay"/>.</summary>
        void After(TimeSpan delay, Action action);

        /// <summary>A short vibration for a turn, a long one for a result (if the device has one).</summary>
        void Vibrate(bool isLong);

        /// <summary>Goes back from the game page.</summary>
        void Leave();
    }
}
