namespace Belot.UI.Game
{
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>Property-change plumbing for the table's view models.</summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Sets the field and, if it changed, raises the property and the ones depending on it.</summary>
        /// <returns>Whether the value changed.</returns>
        protected bool SetField<T>(ref T field, T value, params string[] propertyNames)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.Raise(propertyNames);
            return true;
        }

        protected void Raise(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
