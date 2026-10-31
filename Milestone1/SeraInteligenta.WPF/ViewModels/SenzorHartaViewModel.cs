using SeraInteligenta.Core.Models;

namespace SeraInteligenta.WPF.ViewModels;

/// <summary>
/// Un dispozitiv cu senzori: desenat ca punct pe hartă și afișat ca rând în tabul „Senzori”.
/// </summary>
public sealed class SenzorHartaViewModel : ViewModelBase
{
    private string _valori = "fără citiri";
    private string _folosire = "–";

    public SenzorHartaViewModel(DispozitivSenzor dispozitiv, double scala)
    {
        Dispozitiv = dispozitiv;
        Tip = dispozitiv.EsteStatieClimat ? "climat"
            : dispozitiv.Tipuri.Contains(TipSenzor.Lumina) ? "lumina"
            : "sol";
        Marime = dispozitiv.EsteStatieClimat ? 18 : 11;
        // Canvas poziționează colțul stânga-sus; centrăm punctul pe poziția dispozitivului
        X = dispozitiv.X * scala - Marime / 2;
        Y = dispozitiv.Y * scala - Marime / 2;

        if (!dispozitiv.EsteStatieClimat) _folosire = $"măsoară Zona {dispozitiv.ZonaId}";
    }

    public DispozitivSenzor Dispozitiv { get; }
    public string Id => Dispozitiv.Id;

    /// <summary>„climat”, „sol” sau „lumina”; folosit în XAML pentru culoarea punctului.</summary>
    public string Tip { get; }

    public string TipText => Tip switch
    {
        "climat" => "Stație de climat",
        "lumina" => "Senzor de lumină",
        _ => "Senzor de sol"
    };

    public string PozitieText => $"{Dispozitiv.X:0.#} × {Dispozitiv.Y:0.#} m";

    public double Marime { get; }
    public double X { get; }
    public double Y { get; }

    public string Valori
    {
        get => _valori;
        private set { if (SetProperty(ref _valori, value)) OnPropertyChanged(nameof(Descriere)); }
    }

    /// <summary>
    /// La stațiile de climat: zonele la care e folosită pentru estimare și cu ce pondere
    /// (de exemplu „Zona 1: 51% · Zona 2: 16%”). La senzorii de parcelă: zona pe care o măsoară.
    /// </summary>
    public string Folosire
    {
        get => _folosire;
        set { if (SetProperty(ref _folosire, value)) OnPropertyChanged(nameof(Descriere)); }
    }

    public string Descriere =>
        $"{Dispozitiv.Descriere}\nPoziție: {PozitieText}\n{Valori}\nFolosire: {Folosire}";

    public void Actualizeaza(IEnumerable<CitireSenzor> citiri) =>
        Valori = string.Join(" · ", citiri.Select(c => $"{InfoSenzori.Eticheta(c.Tip)} {c.Valoare:0.0} {c.Unitate}"));
}
