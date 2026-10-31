using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Validare.Reguli;

internal static class Format
{
    public static string M(double valoare) => $"{valoare:0.##} m";
    public static string M2(double valoare) => $"{valoare:0.#} m²";
    public static string P(double valoare) => $"{valoare:0.#}%";
}

/// <summary>Dimensiunile serei trebuie să fie pozitive, iar aleile suficient de late.</summary>
public sealed class RegulaDimensiuniSera : IRegulaValidare
{
    public const double AleeMinimaAbsoluta = 0.5;
    public const double AleeMinimaRecomandata = 0.8;

    public string Nume => "Dimensiunile serei";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        var s = configuratie.Sera;

        if (s.Lungime <= 0 || s.Latime <= 0 || s.Inaltime <= 0)
        {
            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.SeraDimensiuniInvalide,
                "Lungimea, lățimea și înălțimea serei trebuie să fie mai mari decât 0.");
            yield break;
        }

        if (s.LatimeAlee < AleeMinimaAbsoluta)
        {
            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.AleePreaIngusta,
                $"Aleile de {Format.M(s.LatimeAlee)} sunt prea înguste pentru acces.",
                $"Folosește cel puțin {Format.M(AleeMinimaRecomandata)}.");
        }
        else if (s.LatimeAlee < AleeMinimaRecomandata)
        {
            yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.AleeIngusta,
                $"Aleile de {Format.M(s.LatimeAlee)} permit doar trecerea pe jos.",
                $"Pentru roabă sau cărucior sunt recomandați {Format.M(AleeMinimaRecomandata)}.");
        }

        if (s.ProcentMinimCuloare < 5 || s.ProcentMinimCuloare > 50)
        {
            yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.ProcentCuloarNeobisnuit,
                $"Procentul minim pentru culoare ({Format.P(s.ProcentMinimCuloare)}) e în afara intervalului uzual de 5–50%.");
        }
    }
}

/// <summary>Fiecare parcelă trebuie să aibă dimensiuni pozitive, rezonabile și să nu fie mai mare decât sera.</summary>
public sealed class RegulaDimensiuniZone : IRegulaValidare
{
    public const double LaturaMinima = 1.0;

    public string Nume => "Dimensiunile parcelelor";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        var s = configuratie.Sera;

        foreach (var z in configuratie.Zone)
        {
            if (z.Lungime <= 0 || z.Latime <= 0)
            {
                yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.ZonaDimensiuniInvalide,
                    $"{z.Nume}: lungimea și lățimea trebuie să fie mai mari decât 0.", ZonaId: z.Id);
                continue;
            }

            if (s.Lungime > 0 && s.Latime > 0 && (z.Lungime > s.Lungime || z.Latime > s.Latime))
            {
                yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.ZonaMaiMareDecatSera,
                    $"{z.Nume} ({Format.M(z.Lungime)} × {Format.M(z.Latime)}) nu încape în seră nici singură ({Format.M(s.Lungime)} × {Format.M(s.Latime)}).",
                    "Micșorează parcela.", z.Id);
            }

            if (z.Lungime < LaturaMinima || z.Latime < LaturaMinima)
            {
                yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.ZonaPreaMica,
                    $"{z.Nume} are o latură sub {Format.M(LaturaMinima)}; e greu de lucrat pe ea.", ZonaId: z.Id);
            }
        }
    }
}

/// <summary>Suma parcelelor nu poate depăși sera, iar culoarele trebuie să păstreze procentul minim.</summary>
public sealed class RegulaSuprafata : IRegulaValidare
{
    public string Nume => "Suprafața ocupată";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        var s = configuratie.Sera;
        if (s.SuprafataTotala <= 0) yield break;

        double ocupat = configuratie.Zone.Sum(z => Math.Max(0, z.SuprafataMp));
        double culoar = s.SuprafataTotala - ocupat;

        if (ocupat > s.SuprafataTotala)
        {
            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.SuprafataDepasita,
                $"Parcelele ocupă {Format.M2(ocupat)}, mai mult decât suprafața serei ({Format.M2(s.SuprafataTotala)}).",
                $"Elimină cel puțin {Format.M2(ocupat - s.SuprafataMaximaCultivabila)} din parcele.");
        }
        else if (culoar < s.SuprafataMinimaCuloare)
        {
            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.CuloarSubMinim,
                $"Pentru culoare rămân {Format.M2(culoar)} ({Format.P(culoar / s.SuprafataTotala * 100)}), sub minimul de {Format.P(s.ProcentMinimCuloare)} ({Format.M2(s.SuprafataMinimaCuloare)}).",
                $"Eliberează cel puțin {Format.M2(s.SuprafataMinimaCuloare - culoar)}.");
        }
        else
        {
            yield return new ProblemaValidare(NivelSeveritate.Info, Coduri.SuprafataOk,
                $"Parcelele ocupă {Format.M2(ocupat)} ({Format.P(ocupat / s.SuprafataTotala * 100)}); culoarele au {Format.M2(culoar)}.");
        }
    }
}

/// <summary>
/// Verifică dacă parcelele încap FIZIC, cu aleile între ele.
/// O configurație poate trece de verificarea suprafeței și totuși să nu încapă.
/// </summary>
public sealed class RegulaAmplasare : IRegulaValidare
{
    private readonly IServiciuAmplasare _amplasare;

    public RegulaAmplasare(IServiciuAmplasare amplasare) => _amplasare = amplasare;

    public string Nume => "Amplasarea parcelelor";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        var s = configuratie.Sera;
        if (s.Lungime <= 0 || s.Latime <= 0) yield break;

        var plan = _amplasare.Calculeaza(configuratie);

        foreach (var a in plan.Zone.Where(a => !a.Incape))
        {
            var zona = configuratie.Zone.First(z => z.Id == a.ZonaId);
            if (zona.Lungime <= 0 || zona.Latime <= 0) continue; // raportat deja de RegulaDimensiuniZone

            var directii = new List<string>();
            if (a.DepasireLungime > AmplasareZona.Toleranta) directii.Add($"cu {Format.M(a.DepasireLungime)} pe lungime");
            if (a.DepasireLatime > AmplasareZona.Toleranta) directii.Add($"cu {Format.M(a.DepasireLatime)} pe lățime");

            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.AmplasareDepasita,
                $"{zona.Nume} iese din seră {string.Join(" și ", directii)}, ținând cont de aleile de {Format.M(s.LatimeAlee)}.",
                $"Cu aleile incluse, parcelele au nevoie de {Format.M(plan.LungimeNecesara)} × {Format.M(plan.LatimeNecesara)}; sera are {Format.M(s.Lungime)} × {Format.M(s.Latime)}.",
                zona.Id);
        }
    }
}
