using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Core.Validare.Reguli;

/// <summary>
/// Verifică dacă plantele din aceeași parcelă pot crește împreună:
/// incompatibilități declarate în catalog, umiditatea solului, lumina, familia botanică.
/// </summary>
public sealed class RegulaCompatibilitatePlante : IRegulaValidare
{
    /// <summary>Sub acest interval comun de umiditate a solului, irigarea trebuie să fie foarte precisă.</summary>
    public const double PragUmiditateLaLimita = 5.0;

    private readonly ICatalogPlante _catalog;

    public RegulaCompatibilitatePlante(ICatalogPlante catalog) => _catalog = catalog;

    public string Nume => "Compatibilitatea plantelor";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        foreach (var zona in configuratie.Zone)
        {
            foreach (var problema in VerificaZona(zona))
                yield return problema;
        }
    }

    private IEnumerable<ProblemaValidare> VerificaZona(ZonaCultura zona)
    {
        if (string.IsNullOrWhiteSpace(zona.PlantaPrincipalaId))
        {
            yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.ZonaFaraPlanta,
                $"{zona.Nume} nu are o plantă principală aleasă.", "Alege o plantă din listă.", zona.Id);
            yield break;
        }

        var a = _catalog.Gaseste(zona.PlantaPrincipalaId);
        if (a is null)
        {
            yield return Necunoscuta(zona, zona.PlantaPrincipalaId);
            yield break;
        }

        if (!zona.AreDouaPlante) yield break;

        var b = _catalog.Gaseste(zona.PlantaSecundaraId);
        if (b is null)
        {
            yield return Necunoscuta(zona, zona.PlantaSecundaraId);
            yield break;
        }

        if (string.Equals(a.IdPlanta, b.IdPlanta, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ProblemaValidare(NivelSeveritate.Info, Coduri.PlantaDublata,
                $"{zona.Nume}: {a.Nume} e aleasă și ca plantă secundară.", "Alege altă plantă sau lasă zona cu o singură cultură.", zona.Id);
            yield break;
        }

        // 1. Incompatibilitate declarată în catalog (în oricare direcție)
        if (a.EsteIncompatibilaCu(b.IdPlanta) || b.EsteIncompatibilaCu(a.IdPlanta))
        {
            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.PlanteIncompatibile,
                $"{zona.Nume}: {a.Nume} și {b.Nume} nu se recomandă în aceeași zonă.",
                "Mută una dintre plante în altă zonă.", zona.Id);
        }

        // 2. Solul: plantele din aceeași parcelă au același sol și aceeași irigare.
        //    (Temperatura și umiditatea aerului sunt comune întregii sere: le verifică RegulaClimatComun.)
        var sol = a.UmiditateSol.Suprapunere(b.UmiditateSol);
        if (sol <= 0)
        {
            yield return new ProblemaValidare(NivelSeveritate.Eroare, Coduri.ConditiiIncompatibile,
                $"{zona.Nume}: umiditățile optime ale solului nu se suprapun ({a.Nume} {a.UmiditateSol}%, {b.Nume} {b.UmiditateSol}%).",
                "Parcela are o singură irigare; pune plantele în zone separate.", zona.Id);
        }
        else if (sol < PragUmiditateLaLimita)
        {
            yield return LaLimita(zona, "umiditatea solului", $"{sol:0.#}%");
        }

        // 3. Lumina se poate ajusta (umbrire, lămpi), deci e doar avertisment
        if (a.Lumina.Suprapunere(b.Lumina) <= 0)
        {
            yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.LuminaDiferita,
                $"{zona.Nume}: {a.Nume} și {b.Nume} au nevoie de niveluri de lumină diferite ({a.Lumina} klx, {b.Lumina} klx).",
                "Așază planta mai pretențioasă spre sud sau folosește umbrire parțială.", zona.Id);
        }

        // 4. Aceeași familie: boli și dăunători comuni
        if (!string.IsNullOrWhiteSpace(a.Familie) &&
            string.Equals(a.Familie, b.Familie, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.AceeasiFamilie,
                $"{zona.Nume}: {a.Nume} și {b.Nume} sunt din aceeași familie ({a.Familie}) și au boli și dăunători comuni.",
                "Se poate, dar urmărește atent starea plantelor.", zona.Id);
        }

        // 5. Asociere recomandată (mesaj pozitiv)
        if (a.SeAsociazaCu(b.IdPlanta) || b.SeAsociazaCu(a.IdPlanta))
        {
            yield return new ProblemaValidare(NivelSeveritate.Info, Coduri.AsociereRecomandata,
                $"{zona.Nume}: {a.Nume} și {b.Nume} se asociază bine.", ZonaId: zona.Id);
        }
    }

    private static ProblemaValidare Necunoscuta(ZonaCultura zona, string id) =>
        new(NivelSeveritate.Eroare, Coduri.PlantaNecunoscuta,
            $"{zona.Nume}: planta „{id}” nu există în catalog.", "Alege o plantă din listă.", zona.Id);

    private static ProblemaValidare LaLimita(ZonaCultura zona, string ce, string comun) =>
        new(NivelSeveritate.Avertisment, Coduri.ConditiiLaLimita,
            $"{zona.Nume}: intervalul comun pentru {ce} e foarte îngust ({comun}).",
            "Controlul climatului din zonă trebuie să fie foarte precis.", zona.Id);
}

/// <summary>
/// Aerul serei e comun: temperatura și umiditatea aerului trebuie să convină TUTUROR plantelor din seră,
/// chiar dacă sunt în zone diferite.
/// </summary>
public sealed class RegulaClimatComun : IRegulaValidare
{
    public const double PragTemperaturaLaLimita = 2.0;
    public const double PragUmiditateLaLimita = 5.0;

    private readonly ICatalogPlante _catalog;

    public RegulaClimatComun(ICatalogPlante catalog) => _catalog = catalog;

    public string Nume => "Climatul comun al serei";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        var plante = Services.ClimatComun.PlanteDinSera(configuratie, _catalog);
        if (plante.Count < 2) yield break;

        var temp = Services.ClimatComun.Analizeaza(plante, TipSenzor.Temperatura);
        var umid = Services.ClimatComun.Analizeaza(plante, TipSenzor.UmiditateAer);

        var probleme = new[]
        {
            Analiza(temp, "temperatură", " °C", PragTemperaturaLaLimita),
            Analiza(umid, "umiditatea aerului", "%", PragUmiditateLaLimita)
        }.OfType<ProblemaValidare>().ToList();

        foreach (var p in probleme) yield return p;

        if (probleme.Count == 0)
        {
            yield return new ProblemaValidare(NivelSeveritate.Info, Coduri.ClimatComunOk,
                $"Toate plantele au un climat comun: {temp.Tinta} °C și {umid.Tinta}% umiditate a aerului.");
        }
    }

    private static ProblemaValidare? Analiza(Services.AnalizaClimat a, string ce, string unitate, double prag)
    {
        if (a.LimitaJos is null || a.LimitaSus is null) return null;

        if (a.Comun is null)
        {
            return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.ClimatFaraIntervalComun,
                $"Aerul serei e comun, dar nu există o {ce} bună pentru toate plantele: {a.LimitaJos.Nume} cere minimum {a.LimitaJos.IntervalPentru(a.Tip).Min:0.#}{unitate}, iar {a.LimitaSus.Nume} maximum {a.LimitaSus.IntervalPentru(a.Tip).Max:0.#}{unitate}.",
                $"Sera va fi ținută la compromisul de {a.Tinta}{unitate}. Pentru condiții optime, alege plante cu cerințe apropiate.");
        }

        if (a.Comun.Latime < prag)
        {
            return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.ConditiiLaLimita,
                $"Intervalul de {ce} comun tuturor plantelor e foarte îngust ({a.Comun}{unitate}).",
                "Controlul climatului trebuie să fie foarte precis.");
        }

        return null;
    }
}

/// <summary>Plantele înalte au nevoie de spațiu sub acoperișul serei.</summary>
public sealed class RegulaInaltimePlante : IRegulaValidare
{
    /// <summary>Spațiul liber minim între vârful plantei și acoperiș.</summary>
    public const double SpatiuLiber = 0.5;

    private readonly ICatalogPlante _catalog;

    public RegulaInaltimePlante(ICatalogPlante catalog) => _catalog = catalog;

    public string Nume => "Înălțimea plantelor";

    public IEnumerable<ProblemaValidare> Verifica(ConfiguratieSera configuratie)
    {
        double inaltime = configuratie.Sera.Inaltime;
        if (inaltime <= 0) yield break;

        foreach (var zona in configuratie.Zone)
        {
            foreach (var id in new[] { zona.PlantaPrincipalaId, zona.PlantaSecundaraId }.Distinct())
            {
                var planta = _catalog.Gaseste(id);
                if (planta is null || planta.InaltimeMaxima + SpatiuLiber <= inaltime) continue;

                yield return new ProblemaValidare(NivelSeveritate.Avertisment, Coduri.InaltimeInsuficienta,
                    $"{zona.Nume}: {planta.Nume} crește până la {Format.M(planta.InaltimeMaxima)}, iar sera are {Format.M(inaltime)}.",
                    $"E nevoie de cel puțin {Format.M(planta.InaltimeMaxima + SpatiuLiber)} înălțime sau de o varietate mai joasă.",
                    zona.Id);
            }
        }
    }
}
