using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Services;

/// <summary>Rezultatul analizei unei mărimi de climat (temperatură sau umiditate a aerului) pentru toată sera.</summary>
/// <param name="Comun">Intervalul în care toate plantele sunt în optim, sau null dacă nu există.</param>
/// <param name="Tinta">Intervalul la care trebuie ținută sera: cel comun sau, dacă nu există, compromisul dintre extreme.</param>
/// <param name="LimitaJos">Planta cu cel mai mare minim (cea mai pretențioasă „de jos”).</param>
/// <param name="LimitaSus">Planta cu cel mai mic maxim (cea mai pretențioasă „de sus”).</param>
public sealed record AnalizaClimat(
    TipSenzor Tip,
    IntervalValori? Comun,
    IntervalValori Tinta,
    ProfilPlanta? LimitaJos,
    ProfilPlanta? LimitaSus);

/// <summary>
/// Aerul serei e comun tuturor parcelelor, deci temperatura și umiditatea aerului trebuie să convină
/// tuturor plantelor din seră, nu doar celor din aceeași zonă.
/// </summary>
public static class ClimatComun
{
    public static IntervalValori ImplicitPentru(TipSenzor tip) =>
        tip == TipSenzor.Temperatura ? new IntervalValori(18, 26) : new IntervalValori(55, 75);

    public static IReadOnlyList<ProfilPlanta> PlanteDinSera(ConfiguratieSera configuratie, ICatalogPlante catalog) =>
        configuratie.Zone
            .SelectMany(z => new[] { z.PlantaPrincipalaId, z.PlantaSecundaraId })
            .Select(catalog.Gaseste)
            .OfType<ProfilPlanta>()
            .DistinctBy(p => p.IdPlanta)
            .ToList();

    public static AnalizaClimat Analizeaza(IEnumerable<ProfilPlanta> plante, TipSenzor tip)
    {
        var lista = plante.ToList();
        if (lista.Count == 0)
            return new AnalizaClimat(tip, null, ImplicitPentru(tip), null, null);

        var jos = lista.MaxBy(p => p.IntervalPentru(tip).Min)!;
        var sus = lista.MinBy(p => p.IntervalPentru(tip).Max)!;
        double min = jos.IntervalPentru(tip).Min;
        double max = sus.IntervalPentru(tip).Max;

        if (max > min)
        {
            var comun = new IntervalValori(min, max);
            return new AnalizaClimat(tip, comun, comun, jos, sus);
        }

        // Nu există interval comun: ținta e zona dintre cele două limite care se ciocnesc
        return new AnalizaClimat(tip, null, new IntervalValori(max, min), jos, sus);
    }
}

/// <summary>
/// Estimează valoarea unei mărimi de climat într-un punct oarecare din seră (de exemplu centrul unei parcele),
/// din cele mai apropiate stații, prin ponderare cu inversul pătratului distanței.
/// </summary>
public static class EstimatorClimat
{
    public const int Vecini = 3;

    public static double? Estimeaza(IEnumerable<CitireSenzor> citiri, TipSenzor tip, double x, double y)
    {
        var ponderi = Ponderi(citiri, tip, x, y);
        return ponderi.Count == 0 ? null : ponderi.Sum(p => p.Pondere * p.Citire.Valoare);
    }

    /// <summary>
    /// Stațiile folosite pentru estimarea dintr-un punct și ponderea fiecăreia (suma ponderilor = 1).
    /// Pondere ~ 1 / distanța²: o stație de două ori mai aproape contează de 4 ori mai mult.
    /// </summary>
    public static IReadOnlyList<(CitireSenzor Citire, double Distanta, double Pondere)> Ponderi(
        IEnumerable<CitireSenzor> citiri, TipSenzor tip, double x, double y)
    {
        var apropiate = citiri
            .Where(c => c.Tip == tip)
            .Select(c => (Citire: c, Distanta: Math.Sqrt((c.X - x) * (c.X - x) + (c.Y - y) * (c.Y - y))))
            .OrderBy(t => t.Distanta)
            .Take(Vecini)
            .ToList();

        if (apropiate.Count == 0) return Array.Empty<(CitireSenzor, double, double)>();

        // Punctul coincide cu un senzor: contează doar acela
        if (apropiate[0].Distanta < 0.01) return new[] { (apropiate[0].Citire, apropiate[0].Distanta, 1.0) };

        double suma = apropiate.Sum(a => 1.0 / (a.Distanta * a.Distanta));
        return apropiate
            .Select(a => (a.Citire, a.Distanta, 1.0 / (a.Distanta * a.Distanta) / suma))
            .ToList();
    }
}
