namespace SeraInteligenta.Core.Models;

/// <summary>Date fixe despre fiecare tip de senzor: etichetă, unitate și domeniul fizic posibil.</summary>
public static class InfoSenzori
{
    public static IReadOnlyList<TipSenzor> Toate { get; } = Enum.GetValues<TipSenzor>();

    /// <summary>
    /// Mărimile care țin de aerul serei, comun tuturor parcelelor. Se măsoară cu stații de climat,
    /// nu pe parcele: între două parcele vecine nu există un perete care să separe aerul.
    /// </summary>
    public static bool EsteDeClimat(TipSenzor tip) =>
        tip is TipSenzor.Temperatura or TipSenzor.UmiditateAer;

    public static string Eticheta(TipSenzor tip) => tip switch
    {
        TipSenzor.Temperatura => "Temperatură",
        TipSenzor.UmiditateAer => "Umiditate aer",
        TipSenzor.UmiditateSol => "Umiditate sol",
        TipSenzor.Lumina => "Lumină",
        _ => tip.ToString()
    };

    public static string Unitate(TipSenzor tip) => tip switch
    {
        TipSenzor.Temperatura => "°C",
        TipSenzor.UmiditateAer => "%",
        TipSenzor.UmiditateSol => "%",
        TipSenzor.Lumina => "klx",
        _ => string.Empty
    };

    /// <summary>Valorile pe care le poate raporta senzorul (limitele fizice ale aparatului).</summary>
    public static IntervalValori DomeniuFizic(TipSenzor tip) => tip switch
    {
        TipSenzor.Temperatura => new IntervalValori(-10, 50),
        TipSenzor.UmiditateAer => new IntervalValori(0, 100),
        TipSenzor.UmiditateSol => new IntervalValori(0, 100),
        TipSenzor.Lumina => new IntervalValori(0, 100),
        _ => new IntervalValori(0, 100)
    };
}
