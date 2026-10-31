using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Services;

/// <summary>
/// Așază parcelele pe două coloane, rând după rând, în ordinea ID-urilor:
///
///   alee | Zona 1 | alee | Zona 2 | alee
///   alee | Zona 3 | alee | Zona 4 | alee
///
/// Între rânduri și pe margini există câte o alee de lățimea configurată.
/// Lățimea unei coloane = cea mai lungă parcelă din ea; înălțimea unui rând = cea mai lată parcelă din el.
/// </summary>
public sealed class ServiciuAmplasare : IServiciuAmplasare
{
    public const int NumarColoane = 2;

    public PlanAmplasare Calculeaza(ConfiguratieSera configuratie)
    {
        var sera = configuratie.Sera;
        var zone = configuratie.Zone.OrderBy(z => z.Id).ToList();
        double alee = Math.Max(0, sera.LatimeAlee);

        if (zone.Count == 0)
            return new PlanAmplasare(Array.Empty<AmplasareZona>(), 2 * alee, 2 * alee);

        int coloane = Math.Min(NumarColoane, zone.Count);
        int randuri = (zone.Count + coloane - 1) / coloane;

        var latimeColoana = new double[coloane];
        var inaltimeRand = new double[randuri];
        for (int i = 0; i < zone.Count; i++)
        {
            latimeColoana[i % coloane] = Math.Max(latimeColoana[i % coloane], Math.Max(0, zone[i].Lungime));
            inaltimeRand[i / coloane] = Math.Max(inaltimeRand[i / coloane], Math.Max(0, zone[i].Latime));
        }

        var startColoana = new double[coloane];
        double x = alee;
        for (int c = 0; c < coloane; c++)
        {
            startColoana[c] = x;
            x += latimeColoana[c] + alee;
        }

        var startRand = new double[randuri];
        double y = alee;
        for (int r = 0; r < randuri; r++)
        {
            startRand[r] = y;
            y += inaltimeRand[r] + alee;
        }

        var amplasari = new List<AmplasareZona>(zone.Count);
        for (int i = 0; i < zone.Count; i++)
        {
            var zona = zone[i];
            double zx = startColoana[i % coloane];
            double zy = startRand[i / coloane];
            double lungime = Math.Max(0, zona.Lungime);
            double latime = Math.Max(0, zona.Latime);

            // Parcela trebuie să lase și aleea de pe margine liberă
            double depasireLungime = Math.Max(0, zx + lungime + alee - sera.Lungime);
            double depasireLatime = Math.Max(0, zy + latime + alee - sera.Latime);

            amplasari.Add(new AmplasareZona(zona.Id, zx, zy, lungime, latime, depasireLungime, depasireLatime));
        }

        // x și y au ajuns exact la capătul ultimei alei
        return new PlanAmplasare(amplasari, x, y);
    }
}
