using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenIPRadar.Presentation.Mvvm;

/// <summary>
/// A minimal <see cref="INotifyPropertyChanged"/> base class for view models, replacing an
/// external MVVM toolkit. Provides <see cref="SetProperty{T}"/> to assign a backing field and
/// raise change notifications only when the value actually changes.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Assigns <paramref name="value"/> to <paramref name="field"/> and notifies if changed.</summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="field">The backing field, passed by reference.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The property name (supplied automatically).</param>
    /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Raises <see cref="PropertyChanged"/> for the given property.</summary>
    /// <param name="propertyName">The property name (supplied automatically).</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
