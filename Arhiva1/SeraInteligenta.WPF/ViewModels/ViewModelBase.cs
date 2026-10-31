using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SeraInteligenta.WPF.ViewModels;

/// <summary>
/// Baza tuturor ViewModel-urilor: notificări de proprietăți (INotifyPropertyChanged)
/// și erori de introducere a datelor (INotifyDataErrorInfo), afișate automat de WPF cu chenar roșu.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, string> _erori = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _erori.Count > 0;

    public IEnumerable GetErrors(string? propertyName) =>
        propertyName is not null && _erori.TryGetValue(propertyName, out var eroare)
            ? new[] { eroare }
            : Array.Empty<string>();

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T camp, T valoare, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(camp, valoare)) return false;
        camp = valoare;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>Setează (sau șterge, cu null) eroarea afișată lângă un câmp.</summary>
    protected void SeteazaEroare(string proprietate, string? mesaj)
    {
        bool schimbat = mesaj is null ? _erori.Remove(proprietate) : !_erori.TryGetValue(proprietate, out var vechi) || vechi != mesaj;
        if (mesaj is not null) _erori[proprietate] = mesaj;
        if (schimbat)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(proprietate));
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}

/// <summary>Comandă simplă pentru butoane (ICommand).</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _executa;
    private readonly Func<object?, bool>? _poateExecuta;

    public RelayCommand(Action<object?> executa, Func<object?, bool>? poateExecuta = null)
    {
        _executa = executa;
        _poateExecuta = poateExecuta;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _poateExecuta?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _executa(parameter);
}
