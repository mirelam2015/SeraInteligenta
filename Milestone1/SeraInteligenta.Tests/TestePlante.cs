using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.Core.Services;
using SeraInteligenta.Core.Validare;
using SeraInteligenta.Core.Validare.Reguli;

namespace SeraInteligenta.Tests;

public class TesteCompatibilitate
{
    private readonly ICatalogPlante _catalog = Fixturi.CatalogReal();

    private List<ProblemaValidare> Verifica(string principala, string secundara) =>
        new RegulaCompatibilitatePlante(_catalog)
            .Verifica(Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, 10, 4, principala, secundara)))
            .ToList();

    [Fact]
    public void RosiiSiCastraveti_SuntIncompatibile()
    {
        var probleme = Verifica("rosii", "castraveti");
        Assert.Contains(probleme, p => p.Cod == Coduri.PlanteIncompatibile && p.Nivel == NivelSeveritate.Eroare);
    }

    [Fact]
    public void IncompatibilitateaEVerificataInAmbeleDirectii()
    {
        // „capsuni” declară incompatibilitatea cu „varza”; ordinea din zonă nu contează
        Assert.Contains(Verifica("capsuni", "varza"), p => p.Cod == Coduri.PlanteIncompatibile);
        Assert.Contains(Verifica("varza", "capsuni"), p => p.Cod == Coduri.PlanteIncompatibile);
    }

    [Fact]
    public void UmiditatiAleSoluluiFaraSuprapunere_DauEroare()
    {
        // Castraveți: sol 70–85%; busuioc: sol 50–70%. Parcela are o singură irigare.
        var probleme = Verifica("castraveti", "ierburi");
        Assert.Contains(probleme, p => p.Cod == Coduri.ConditiiIncompatibile && p.Nivel == NivelSeveritate.Eroare);
    }

    [Fact]
    public void TemperaturaNuMaiEVerificataPeZona_PentruCaAerulEComun()
    {
        // Salată 10–20 °C și vinete 22–30 °C: problema e a întregii sere (RegulaClimatComun), nu a parcelei
        var probleme = Verifica("salata", "vinete");
        Assert.DoesNotContain(probleme, p => p.Cod == Coduri.ConditiiIncompatibile);
    }

    [Fact]
    public void AceeasiFamilie_DaAvertisment()
    {
        var probleme = Verifica("ardei", "vinete");
        Assert.Contains(probleme, p => p.Cod == Coduri.AceeasiFamilie && p.Nivel == NivelSeveritate.Avertisment);
        Assert.DoesNotContain(probleme, p => p.Nivel == NivelSeveritate.Eroare);
    }

    [Fact]
    public void RosiiSiBusuioc_SuntOAsociereRecomandata()
    {
        var probleme = Verifica("rosii", "ierburi");
        Assert.Contains(probleme, p => p.Cod == Coduri.AsociereRecomandata && p.Nivel == NivelSeveritate.Info);
        Assert.DoesNotContain(probleme, p => p.Nivel != NivelSeveritate.Info);
    }

    [Fact]
    public void PlantaNecunoscuta_DaEroare()
    {
        Assert.Contains(Verifica("ananas", ""), p => p.Cod == Coduri.PlantaNecunoscuta);
    }

    [Fact]
    public void ZonaFaraPlanta_DaAvertisment()
    {
        Assert.Contains(Verifica("", ""), p => p.Cod == Coduri.ZonaFaraPlanta);
    }

    [Fact]
    public void ConditiiLaLimita_DauAvertisment()
    {
        var a = new ProfilPlanta { IdPlanta = "a", Nume = "A", UmiditateSol = new(60, 70), Lumina = new(10, 20) };
        var b = new ProfilPlanta { IdPlanta = "b", Nume = "B", UmiditateSol = new(67, 80), Lumina = new(10, 20) };
        var regula = new RegulaCompatibilitatePlante(new CatalogInMemorie(a, b));

        var probleme = regula.Verifica(Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, 10, 4, "a", "b"))).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.ConditiiLaLimita); // doar 3% comun
    }

    [Fact]
    public void SeraPreaJoasa_PentruCastraveti_DaAvertisment()
    {
        var config = Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, 10, 4, "castraveti"));
        config.Sera.Inaltime = 2.5; // castraveții cresc până la 2,5 m, plus 0,5 m spațiu liber

        var probleme = new RegulaInaltimePlante(_catalog).Verifica(config).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.InaltimeInsuficienta);
    }
}

public class TesteValidatorSera
{
    private static ValidatorSera ValidatorComplet(ICatalogPlante catalog) => new(new IRegulaValidare[]
    {
        new RegulaDimensiuniSera(),
        new RegulaDimensiuniZone(),
        new RegulaSuprafata(),
        new RegulaAmplasare(new ServiciuAmplasare()),
        new RegulaCompatibilitatePlante(catalog),
        new RegulaClimatComun(catalog),
        new RegulaInaltimePlante(catalog)
    });

    [Fact]
    public void ConfiguratiaImplicita_EsteValida()
    {
        var rezultat = ValidatorComplet(Fixturi.CatalogReal()).Valideaza(ConfiguratieSera.Implicita());

        Assert.True(rezultat.EsteValid);
        Assert.Equal(0, rezultat.NumarErori);
    }

    [Fact]
    public void ProblemeleSuntOrdonateDupaGravitate()
    {
        var config = Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, 10, 4, "rosii", "castraveti"), Fixturi.Zona(2, 10, 4, "ardei", "vinete"));

        var probleme = ValidatorComplet(Fixturi.CatalogReal()).Valideaza(config).Probleme;

        var niveluri = probleme.Select(p => (int)p.Nivel).ToList();
        Assert.Equal(niveluri.OrderByDescending(n => n), niveluri);
        Assert.Equal(NivelSeveritate.Eroare, probleme[0].Nivel);
    }

    [Fact]
    public void ValidatorulFolosesteDoarRegulilePrimite()
    {
        // Fără nicio regulă, orice configurație e validă: validatorul nu „știe” reguli proprii
        var rezultat = new ValidatorSera(Array.Empty<IRegulaValidare>())
            .Valideaza(Fixturi.Sera(1, 1, 1.0, Fixturi.Zona(1, 100, 100)));

        Assert.True(rezultat.EsteValid);
        Assert.Empty(rezultat.Probleme);
    }

    [Fact]
    public void NivelulMaximAlZonei_IgnoraMesajeleInfo()
    {
        var rezultat = new RezultatValidare(new[]
        {
            new ProblemaValidare(NivelSeveritate.Info, "X", "info", ZonaId: 1),
            new ProblemaValidare(NivelSeveritate.Avertisment, "Y", "avertisment", ZonaId: 2)
        });

        Assert.Null(rezultat.NivelMaximPentruZona(1));
        Assert.Equal(NivelSeveritate.Avertisment, rezultat.NivelMaximPentruZona(2));
    }
}

public class TesteClimatComun
{
    private readonly ICatalogPlante _catalog = Fixturi.CatalogReal();

    private List<ProblemaValidare> Verifica(params ZonaCultura[] zone) =>
        new RegulaClimatComun(_catalog).Verifica(Fixturi.Sera(24, 12, 1.0, zone)).ToList();

    [Fact]
    public void PlanteDinZoneDiferite_FaraTemperaturaComuna_DauAvertisment()
    {
        // Aerul e comun: salata (max 20 °C) și vinetele (min 22 °C) nu pot fi ambele în optim, chiar dacă sunt în zone diferite
        var probleme = Verifica(Fixturi.Zona(1, 10, 4, "salata"), Fixturi.Zona(2, 10, 4, "vinete"));

        var p = Assert.Single(probleme, x => x.Cod == Coduri.ClimatFaraIntervalComun && x.Mesaj.Contains("temperatur"));
        Assert.Equal(NivelSeveritate.Avertisment, p.Nivel);
        Assert.Null(p.ZonaId);
    }

    [Fact]
    public void PlanteCuClimatApropiat_DauDoarInfo()
    {
        var probleme = Verifica(Fixturi.Zona(1, 10, 4, "rosii"), Fixturi.Zona(2, 10, 4, "ardei"));

        var p = Assert.Single(probleme);
        Assert.Equal(Coduri.ClimatComunOk, p.Cod);
    }

    [Fact]
    public void FaraIntervalComun_TintaEsteCompromisulDintreLimite()
    {
        var plante = new[] { _catalog.Gaseste("salata")!, _catalog.Gaseste("vinete")! };

        var analiza = ClimatComun.Analizeaza(plante, TipSenzor.Temperatura);

        Assert.Null(analiza.Comun);
        Assert.Equal(20, analiza.Tinta.Min);
        Assert.Equal(22, analiza.Tinta.Max);
        Assert.Equal("vinete", analiza.LimitaJos!.IdPlanta);
        Assert.Equal("salata", analiza.LimitaSus!.IdPlanta);
    }

    [Fact]
    public void IntervalComun_EsteIntersectiaTuturor()
    {
        var plante = new[] { "rosii", "ardei", "ierburi" }.Select(id => _catalog.Gaseste(id)!).ToList();

        var analiza = ClimatComun.Analizeaza(plante, TipSenzor.Temperatura);

        Assert.NotNull(analiza.Comun);
        Assert.Equal(20, analiza.Comun!.Min);   // ardei
        Assert.Equal(27, analiza.Comun.Max);    // roșii
    }

    [Fact]
    public void ConfiguratiaImplicita_AreAvertismentDeClimat_DarRamaneValida()
    {
        var probleme = new RegulaClimatComun(_catalog).Verifica(ConfiguratieSera.Implicita()).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.ClimatFaraIntervalComun);
        Assert.DoesNotContain(probleme, p => p.Nivel == NivelSeveritate.Eroare);
    }
}
