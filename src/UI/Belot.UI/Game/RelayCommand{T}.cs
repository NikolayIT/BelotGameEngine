namespace Belot.UI.Game
{
    using System;
    using System.Windows.Input;

    public sealed class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> execute;

        private readonly Func<T?, bool>? canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => this.canExecute?.Invoke(Cast(parameter)) ?? true;

        public void Execute(object? parameter) => this.execute(Cast(parameter));

        public void RaiseCanExecuteChanged() => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        private static T? Cast(object? p) => p is T t ? t : default;
    }
}
