namespace SeraInteligenta.Core.Models;

/// <summary>
/// O specie din catalogul de plante (Date/plante_catalog.json).
/// Condițiile sunt date ca intervale, ca să putem verifica dacă două plante pot sta în aceeași zonă.
/// </summary>
public sealed class ProfilPlanta
{
    public string IdPlanta { get; set; } = string.Empty;
    public string Nume { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;

    /// <summary>Familia botanică. Plantele din aceeași familie au boli și dăunători comuni.</summary>
    public string Familie { get; set; } = string.Empty;

    /// <summary>Temperatura aerului, în °C.</summary>
    public IntervalValori Temperatura { get; set; } = new();

    /// <summary>Umiditatea relativă a aerului, în %.</summary>
    public IntervalValori UmiditateAer { get; set; } = new();

    /// <summary>Umiditatea solului, în %.</summary>
    public IntervalValori UmiditateSol { get; set; } = new();

    /// <summary>Iluminarea, în kilolucși (klx).</summary>
    public IntervalValori Lumina { get; set; } = new();

    /// <summary>Înălțimea maximă a plantei la maturitate, în metri.</summary>
    public double InaltimeMaxima { get; set; }

    /// <summary>ID-urile plantelor care nu trebuie cultivate în aceeași zonă.</summary>
    public List<string> IncompatibilaCu { get; set; } = new();

    /// <summary>ID-urile plantelor cu care se asociază bine (cultură asociată).</summary>
    public List<string> AsocieriRecomandate { get; set; } = new();

    public string Descriere { get; set; } = string.Empty;

    public IntervalValori IntervalPentru(TipSenzor tip) => tip switch
    {
        TipSenzor.Temperatura => Temperatura,
        TipSenzor.UmiditateAer => UmiditateAer,
        TipSenzor.UmiditateSol => UmiditateSol,
        TipSenzor.Lumina => Lumina,
        _ => throw new ArgumentOutOfRangeException(nameof(tip))
    };

    public bool EsteIncompatibilaCu(string idPlanta) =>
        IncompatibilaCu.Contains(idPlanta, StringComparer.OrdinalIgnoreCase);

    public bool SeAsociazaCu(string idPlanta) =>
        AsocieriRecomandate.Contains(idPlanta, StringComparer.OrdinalIgnoreCase);

    public override string ToString() => Nume;
}
