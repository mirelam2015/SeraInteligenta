namespace SeraInteligenta.Core.Models;

/// <summary>
/// O citire de la un senzor. <paramref name="ZonaId"/> e null pentru stațiile de climat, care măsoară aerul comun al serei.
/// X și Y sunt poziția senzorului pe plan, în metri.
/// </summary>
public sealed record CitireSenzor(
    string SenzorId,
    string DispozitivId,
    int? ZonaId,
    TipSenzor Tip,
    double Valoare,
    string Unitate,
    double X,
    double Y,
    DateTime Moment);

/// <summary>
/// O problemă găsită de o regulă de validare.
/// <paramref name="Cod"/> e stabil (folosit în teste), <paramref name="Mesaj"/> e pentru utilizator.
/// </summary>
public sealed record ProblemaValidare(
    NivelSeveritate Nivel,
    string Cod,
    string Mesaj,
    string? Recomandare = null,
    int? ZonaId = null);

/// <summary>Rezultatul validării întregii configurații.</summary>
public sealed class RezultatValidare
{
    public RezultatValidare(IReadOnlyList<ProblemaValidare> probleme) => Probleme = probleme;

    public IReadOnlyList<ProblemaValidare> Probleme { get; }

    public int NumarErori => Probleme.Count(p => p.Nivel == NivelSeveritate.Eroare);
    public int NumarAvertismente => Probleme.Count(p => p.Nivel == NivelSeveritate.Avertisment);

    /// <summary>Configurația e validă dacă nu are erori; avertismentele sunt permise.</summary>
    public bool EsteValid => NumarErori == 0;

    public NivelSeveritate? NivelMaximPentruZona(int zonaId) =>
        Probleme.Where(p => p.ZonaId == zonaId && p.Nivel != NivelSeveritate.Info)
                .Select(p => (NivelSeveritate?)p.Nivel)
                .Max();
}

/// <summary>Poziția calculată a unei parcele pe planul serei (în metri, din colțul stânga-sus).</summary>
public sealed record AmplasareZona(
    int ZonaId,
    double X,
    double Y,
    double Lungime,
    double Latime,
    double DepasireLungime,
    double DepasireLatime)
{
    public const double Toleranta = 1e-6;
    public bool Incape => DepasireLungime <= Toleranta && DepasireLatime <= Toleranta;
}

/// <summary>Planul complet: pozițiile tuturor parcelelor și spațiul de care au nevoie împreună cu aleile.</summary>
public sealed record PlanAmplasare(
    IReadOnlyList<AmplasareZona> Zone,
    double LungimeNecesara,
    double LatimeNecesara)
{
    public AmplasareZona? PentruZona(int zonaId) => Zone.FirstOrDefault(z => z.ZonaId == zonaId);
}
