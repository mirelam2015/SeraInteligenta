using SeraInteligenta.Core.Models;
using SeraInteligenta.Core.Services;
using SeraInteligenta.Core.Validare;
using SeraInteligenta.Core.Validare.Reguli;

namespace SeraInteligenta.Tests;

public class TesteAmplasare
{
    private readonly ServiciuAmplasare _amplasare = new();

    [Fact]
    public void ConfiguratiaImplicita_IncapeInSera()
    {
        var plan = _amplasare.Calculeaza(ConfiguratieSera.Implicita());

        Assert.All(plan.Zone, z => Assert.True(z.Incape));
        Assert.Equal(23, plan.LungimeNecesara, 3);   // 1 + 10 + 1 + 10 + 1
        Assert.Equal(11, plan.LatimeNecesara, 3);    // 1 + 4 + 1 + 4 + 1
    }

    [Fact]
    public void ZoneleSuntAsezatePeDouaColoane_CuAleiIntreEle()
    {
        var plan = _amplasare.Calculeaza(Fixturi.Sera(24, 12, 1.0, Fixturi.PatruZone(10, 4)));

        var z1 = plan.PentruZona(1)!;
        var z2 = plan.PentruZona(2)!;
        var z3 = plan.PentruZona(3)!;
        Assert.Equal((1.0, 1.0), (z1.X, z1.Y));
        Assert.Equal((12.0, 1.0), (z2.X, z2.Y));  // 1 + 10 + 1
        Assert.Equal((1.0, 6.0), (z3.X, z3.Y));   // 1 + 4 + 1
    }

    [Fact]
    public void ZonaPreaLunga_IeseDinSera_CuDepasireaCorecta()
    {
        // 1 + 12 + 1 + 10 + 1 = 25 m > 24 m
        var plan = _amplasare.Calculeaza(Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, 12, 4), Fixturi.Zona(2, 10, 4)));

        var z2 = plan.PentruZona(2)!;
        Assert.False(z2.Incape);
        Assert.Equal(1.0, z2.DepasireLungime, 3);
        Assert.True(plan.PentruZona(1)!.Incape);
    }

    [Fact]
    public void FaraAlei_ParceleleSeLipescDeMargine()
    {
        var plan = _amplasare.Calculeaza(Fixturi.Sera(20, 10, 0, Fixturi.PatruZone(10, 5)));

        Assert.All(plan.Zone, z => Assert.True(z.Incape));
        Assert.Equal(0.0, plan.PentruZona(1)!.X);
    }
}

public class TesteReguliSpatiu
{
    [Fact]
    public void SuprafataPesteSera_DaEroare()
    {
        var config = Fixturi.Sera(10, 10, 1.0, Fixturi.PatruZone(6, 5)); // 120 m² > 100 m²
        var probleme = new RegulaSuprafata().Verifica(config).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.SuprafataDepasita && p.Nivel == NivelSeveritate.Eroare);
    }

    [Fact]
    public void CuloarSubMinim_DaEroare()
    {
        var config = Fixturi.Sera(10, 10, 1.0, Fixturi.PatruZone(5, 4.5)); // 90 m², rămân 10 m² < 15 m²
        var probleme = new RegulaSuprafata().Verifica(config).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.CuloarSubMinim);
    }

    [Fact]
    public void SuprafataInRegula_DaDoarInfo()
    {
        var probleme = new RegulaSuprafata().Verifica(ConfiguratieSera.Implicita()).ToList();

        Assert.Single(probleme);
        Assert.Equal(Coduri.SuprafataOk, probleme[0].Cod);
        Assert.Equal(NivelSeveritate.Info, probleme[0].Nivel);
    }

    [Fact]
    public void ParceleCareIncapPeSuprafata_DarNuFizic_DauEroareDeAmplasare()
    {
        // 4 × 11 × 4,5 = 198 m² (sub cei 244,8 m² cultivabili), dar pe lungime e nevoie de 25 m
        var config = Fixturi.Sera(24, 12, 1.0, Fixturi.PatruZone(11, 4.5));

        var suprafata = new RegulaSuprafata().Verifica(config).ToList();
        var amplasare = new RegulaAmplasare(new ServiciuAmplasare()).Verifica(config).ToList();

        Assert.DoesNotContain(suprafata, p => p.Nivel == NivelSeveritate.Eroare);
        Assert.Contains(amplasare, p => p.Cod == Coduri.AmplasareDepasita);
    }

    [Fact]
    public void ZonaMaiMareDecatSera_DaEroare()
    {
        var config = Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, 30, 4));
        var probleme = new RegulaDimensiuniZone().Verifica(config).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.ZonaMaiMareDecatSera && p.ZonaId == 1);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(10, -2)]
    public void DimensiuniZonaInvalide_DauEroare(double lungime, double latime)
    {
        var config = Fixturi.Sera(24, 12, 1.0, Fixturi.Zona(1, lungime, latime));
        var probleme = new RegulaDimensiuniZone().Verifica(config).ToList();

        Assert.Contains(probleme, p => p.Cod == Coduri.ZonaDimensiuniInvalide);
    }

    [Theory]
    [InlineData(0.3, Coduri.AleePreaIngusta, NivelSeveritate.Eroare)]
    [InlineData(0.6, Coduri.AleeIngusta, NivelSeveritate.Avertisment)]
    public void AleiInguste_SuntSemnalate(double alee, string cod, NivelSeveritate nivel)
    {
        var config = Fixturi.Sera(24, 12, alee, Fixturi.PatruZone(10, 4));
        var probleme = new RegulaDimensiuniSera().Verifica(config).ToList();

        Assert.Contains(probleme, p => p.Cod == cod && p.Nivel == nivel);
    }

    [Fact]
    public void AleiDeUnMetru_NuDauProbleme()
    {
        var probleme = new RegulaDimensiuniSera().Verifica(ConfiguratieSera.Implicita());
        Assert.Empty(probleme);
    }
}
