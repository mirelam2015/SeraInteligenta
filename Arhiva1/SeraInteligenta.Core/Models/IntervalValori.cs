namespace SeraInteligenta.Core.Models;

/// <summary>Un interval închis [Min, Max], de exemplu temperatura optimă 18–27 °C.</summary>
public sealed class IntervalValori
{
    public double Min { get; set; }
    public double Max { get; set; }

    public IntervalValori() { }

    public IntervalValori(double min, double max)
    {
        Min = Math.Min(min, max);
        Max = Math.Max(min, max);
    }

    public double Mijloc => (Min + Max) / 2;
    public double Latime => Max - Min;

    public bool Contine(double valoare) => valoare >= Min && valoare <= Max;

    /// <summary>
    /// Lățimea porțiunii comune a două intervale.
    /// Pozitiv = se suprapun; 0 = doar se ating; negativ = distanța dintre ele.
    /// </summary>
    public double Suprapunere(IntervalValori altul) =>
        Math.Min(Max, altul.Max) - Math.Max(Min, altul.Min);

    public StareValoare Compara(double valoare) =>
        valoare < Min ? StareValoare.SubInterval
        : valoare > Max ? StareValoare.PesteInterval
        : StareValoare.InInterval;

    public override string ToString() => $"{Min:0.#}–{Max:0.#}";
}
