using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Services;

/// <summary>
/// Decide unde se montează senzorii. Totul se calculează din dimensiuni, deci la orice modificare
/// a serei sau a unei parcele senzorii se re-amplasează automat.
///
/// Stații de climat (temperatură + umiditate aer), comune serei: o grilă care începe în colțuri
/// (la 1 m de pereți) și pune stații în interior, pe ambele laturi și pe mijloc, la cel mult 8 m una de alta.
///
/// Pe fiecare parcelă: senzori de umiditate a solului (unul la 20 m²) și de lumină (unul la 40 m²),
/// distribuiți uniform pe suprafața ei.
/// </summary>
public sealed class ServiciuAmplasareSenzori : IServiciuAmplasareSenzori
{
    public const double DistantaMaximaClimat = 8.0;
    public const double DistantaDePerete = 1.0;
    public const double SuprafataPerSenzorSol = 20.0;
    public const double SuprafataPerSenzorLumina = 40.0;
    public const int MaximSenzoriPeParcela = 12;

    private static readonly TipSenzor[] TipuriClimat = { TipSenzor.Temperatura, TipSenzor.UmiditateAer };
    private static readonly TipSenzor[] TipuriSol = { TipSenzor.UmiditateSol };
    private static readonly TipSenzor[] TipuriLumina = { TipSenzor.Lumina };

    public IReadOnlyList<DispozitivSenzor> Calculeaza(ConfiguratieSera configuratie, PlanAmplasare plan)
    {
        var rezultat = new List<DispozitivSenzor>();
        var sera = configuratie.Sera;
        if (sera.Lungime <= 0 || sera.Latime <= 0) return rezultat;

        rezultat.AddRange(StatiiClimat(sera.Lungime, sera.Latime));

        foreach (var parcela in plan.Zone.Where(p => p.Lungime > 0 && p.Latime > 0))
        {
            double suprafata = parcela.Lungime * parcela.Latime;

            int nrSol = NumarPentru(suprafata, SuprafataPerSenzorSol);
            var puncteSol = PuncteUniforme(parcela, nrSol);
            for (int i = 0; i < puncteSol.Count; i++)
                rezultat.Add(new DispozitivSenzor($"Z{parcela.ZonaId}-S{i + 1}", parcela.ZonaId, puncteSol[i].X, puncteSol[i].Y, TipuriSol));

            int nrLumina = NumarPentru(suprafata, SuprafataPerSenzorLumina);
            var puncteLumina = PuncteUniforme(parcela, nrLumina);
            for (int i = 0; i < puncteLumina.Count; i++)
            {
                // ușor decalat față de senzorii de sol, ca să nu se suprapună pe hartă
                double x = Math.Min(puncteLumina[i].X + 0.4, parcela.X + parcela.Lungime);
                double y = Math.Max(puncteLumina[i].Y - 0.4, parcela.Y);
                rezultat.Add(new DispozitivSenzor($"Z{parcela.ZonaId}-L{i + 1}", parcela.ZonaId, x, y, TipuriLumina));
            }
        }

        return rezultat;
    }

    /// <summary>Grila stațiilor de climat; include întotdeauna cele 4 colțuri.</summary>
    public static IReadOnlyList<DispozitivSenzor> StatiiClimat(double lungime, double latime)
    {
        double dx = Math.Min(DistantaDePerete, lungime / 4);
        double dy = Math.Min(DistantaDePerete, latime / 4);
        int coloane = PuncteNecesare(lungime - 2 * dx);
        int randuri = PuncteNecesare(latime - 2 * dy);

        var statii = new List<DispozitivSenzor>(coloane * randuri);
        int nr = 1;
        for (int r = 0; r < randuri; r++)
        {
            for (int c = 0; c < coloane; c++)
            {
                double x = dx + (lungime - 2 * dx) * c / (coloane - 1);
                double y = dy + (latime - 2 * dy) * r / (randuri - 1);
                statii.Add(new DispozitivSenzor($"CL{nr++}", null, x, y, TipuriClimat));
            }
        }
        return statii;
    }

    /// <summary>Câte puncte trebuie pe o latură ca distanța dintre ele să nu depășească 8 m (minim 2: capetele).</summary>
    private static int PuncteNecesare(double lungime) =>
        Math.Max(2, (int)Math.Ceiling(lungime / DistantaMaximaClimat) + 1);

    private static int NumarPentru(double suprafata, double suprafataPerSenzor) =>
        Math.Clamp((int)Math.Ceiling(suprafata / suprafataPerSenzor - 1e-9), 1, MaximSenzoriPeParcela);

    /// <summary>
    /// Împarte parcela într-o grilă de celule cât mai pătrate și pune câte un senzor în centrul primelor <paramref name="numar"/> celule.
    /// </summary>
    public static IReadOnlyList<(double X, double Y)> PuncteUniforme(AmplasareZona parcela, int numar)
    {
        int coloane = Math.Clamp((int)Math.Round(Math.Sqrt(numar * parcela.Lungime / parcela.Latime)), 1, numar);
        int randuri = (numar + coloane - 1) / coloane;

        var puncte = new List<(double, double)>(numar);
        for (int i = 0; i < numar; i++)
        {
            int r = i / coloane;
            int peRand = r == randuri - 1 ? numar - r * coloane : coloane; // ultimul rând poate fi incomplet
            int c = i % coloane;
            double x = parcela.X + parcela.Lungime * (c + 0.5) / peRand;
            double y = parcela.Y + parcela.Latime * (r + 0.5) / randuri;
            puncte.Add((x, y));
        }
        return puncte;
    }
}
