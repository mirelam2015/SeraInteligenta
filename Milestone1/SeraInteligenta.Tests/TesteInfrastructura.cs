using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.Core.Services;
using SeraInteligenta.Infrastructure.Date;
using SeraInteligenta.Infrastructure.Monitorizare;
using SeraInteligenta.Infrastructure.Senzori;

namespace SeraInteligenta.Tests;

public class TesteIntervalValori
{
    [Theory]
    [InlineData(18, 27, 20, 30, 7)]    // se suprapun pe 20–27
    [InlineData(10, 20, 20, 30, 0)]    // doar se ating
    [InlineData(10, 20, 22, 30, -2)]   // distanță de 2
    public void Suprapunere(double a1, double a2, double b1, double b2, double asteptat)
    {
        Assert.Equal(asteptat, new IntervalValori(a1, a2).Suprapunere(new IntervalValori(b1, b2)), 6);
    }

    [Theory]
    [InlineData(15, StareValoare.SubInterval)]
    [InlineData(22, StareValoare.InInterval)]
    [InlineData(30, StareValoare.PesteInterval)]
    public void Compara(double valoare, StareValoare asteptat)
    {
        Assert.Equal(asteptat, new IntervalValori(18, 27).Compara(valoare));
    }
}

public class TesteDateJson
{
    [Fact]
    public void CatalogulReal_SeIncarca_SiAreZecePlante()
    {
        var catalog = Fixturi.CatalogReal();

        Assert.Equal(10, catalog.Toate.Count);
        Assert.NotNull(catalog.Gaseste("ROSII")); // căutarea nu ține cont de majuscule
        Assert.All(catalog.Toate, p => Assert.True(p.Temperatura.Max > p.Temperatura.Min, p.IdPlanta));
    }

    [Fact]
    public void CatalogulReal_NuAreReferinteSpreIDuriInexistente()
    {
        var catalog = Fixturi.CatalogReal();
        var ids = catalog.Toate.SelectMany(p => p.IncompatibilaCu.Concat(p.AsocieriRecomandate));

        Assert.All(ids, id => Assert.NotNull(catalog.Gaseste(id)));
    }

    [Fact]
    public void Configuratia_SeSalveazaSiSeReincarcaIdentic()
    {
        string folder = Path.Combine(Path.GetTempPath(), "SeraTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var depozit = new DepozitConfiguratieJson(new OptiuniDate(folder));
            var original = ConfiguratieSera.Implicita();
            original.Zone[0].Lungime = 7.5;

            depozit.Salveaza(original);
            var citita = depozit.Incarca();

            Assert.Equal(4, citita.Zone.Count);
            Assert.Equal(7.5, citita.Zone[0].Lungime);
            Assert.Equal("Zona 1 – Nord-Vest", citita.Zone[0].Nume);
            Assert.Equal(original.Sera.LatimeAlee, citita.Sera.LatimeAlee);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void FisierLipsa_DaConfiguratiaImplicita()
    {
        var depozit = new DepozitConfiguratieJson(new OptiuniDate(Path.Combine(Path.GetTempPath(), "nu_exista_" + Guid.NewGuid())));
        Assert.Equal(4, depozit.Incarca().Zone.Count);
    }
}

public class TesteAmplasareSenzori
{
    private readonly ServiciuAmplasare _amplasare = new();
    private readonly ServiciuAmplasareSenzori _senzori = new();

    private IReadOnlyList<DispozitivSenzor> Calculeaza(ConfiguratieSera config) =>
        _senzori.Calculeaza(config, _amplasare.Calculeaza(config));

    [Fact]
    public void Sera24x12_Are12StatiiDeClimat_IncluzandColturile()
    {
        var statii = Calculeaza(ConfiguratieSera.Implicita()).Where(d => d.EsteStatieClimat).ToList();

        Assert.Equal(12, statii.Count); // 4 pe lungime × 3 pe lățime
        foreach (var (x, y) in new[] { (1.0, 1.0), (23.0, 1.0), (1.0, 11.0), (23.0, 11.0) })
            Assert.Contains(statii, s => Math.Abs(s.X - x) < 1e-9 && Math.Abs(s.Y - y) < 1e-9);
    }

    [Fact]
    public void StatiileDeClimat_SuntLaCelMult8mUnaDeAlta()
    {
        var statii = ServiciuAmplasareSenzori.StatiiClimat(40, 15);

        var xs = statii.Select(s => s.X).Distinct().OrderBy(x => x).ToList();
        var ys = statii.Select(s => s.Y).Distinct().OrderBy(y => y).ToList();
        Assert.All(xs.Zip(xs.Skip(1)), p => Assert.True(p.Second - p.First <= ServiciuAmplasareSenzori.DistantaMaximaClimat + 1e-9));
        Assert.All(ys.Zip(ys.Skip(1)), p => Assert.True(p.Second - p.First <= ServiciuAmplasareSenzori.DistantaMaximaClimat + 1e-9));
    }

    [Fact]
    public void SeraMaiMica_AreMaiPutineStatii_SiToateSuntInSera()
    {
        var statii = ServiciuAmplasareSenzori.StatiiClimat(8, 4);

        Assert.Equal(4, statii.Count); // doar colțurile
        Assert.All(statii, s => Assert.InRange(s.X, 0, 8));
        Assert.All(statii, s => Assert.InRange(s.Y, 0, 4));
    }

    [Theory]
    [InlineData(10, 4, 2, 1)]     // 40 m²: 2 senzori de sol, 1 de lumină
    [InlineData(20, 4.5, 5, 3)]   // 90 m²: 5 de sol, 3 de lumină
    [InlineData(3, 2, 1, 1)]      // 6 m²: câte unul
    public void SenzoriiDeParcela_DepindDeSuprafata(double lungime, double latime, int sol, int lumina)
    {
        var config = Fixturi.Sera(30, 12, 1.0, Fixturi.Zona(1, lungime, latime));
        var dispozitive = Calculeaza(config).Where(d => d.ZonaId == 1).ToList();

        Assert.Equal(sol, dispozitive.Count(d => d.Tipuri.Contains(TipSenzor.UmiditateSol)));
        Assert.Equal(lumina, dispozitive.Count(d => d.Tipuri.Contains(TipSenzor.Lumina)));
    }

    [Fact]
    public void SenzoriiDeParcela_SuntInInteriorulParcelei_SiSeMutaCuEa()
    {
        var config = Fixturi.Sera(30, 12, 1.0, Fixturi.Zona(1, 10, 4), Fixturi.Zona(2, 8, 4));
        var plan = _amplasare.Calculeaza(config);
        var a = plan.PentruZona(2)!;

        var dispozitive = _senzori.Calculeaza(config, plan).Where(d => d.ZonaId == 2).ToList();

        Assert.NotEmpty(dispozitive);
        Assert.All(dispozitive, d => Assert.InRange(d.X, a.X, a.X + a.Lungime));
        Assert.All(dispozitive, d => Assert.InRange(d.Y, a.Y, a.Y + a.Latime));
    }
}

public class TesteEstimatorClimat
{
    private static CitireSenzor Citire(double x, double y, double valoare) =>
        new("s", "d", null, TipSenzor.Temperatura, valoare, "°C", x, y, DateTime.Now);

    [Fact]
    public void InPozitiaUnuiSenzor_DaValoareaLui()
    {
        var citiri = new[] { Citire(0, 0, 20), Citire(10, 0, 30) };
        Assert.Equal(20, EstimatorClimat.Estimeaza(citiri, TipSenzor.Temperatura, 0, 0));
    }

    [Fact]
    public void LaMijloc_DaMedia()
    {
        var citiri = new[] { Citire(0, 0, 20), Citire(10, 0, 30) };
        Assert.Equal(25, EstimatorClimat.Estimeaza(citiri, TipSenzor.Temperatura, 5, 0)!.Value, 6);
    }

    [Fact]
    public void SenzorulMaiApropiat_ConteazaMaiMult()
    {
        var citiri = new[] { Citire(0, 0, 20), Citire(10, 0, 30) };
        Assert.True(EstimatorClimat.Estimeaza(citiri, TipSenzor.Temperatura, 2, 0) < 25);
    }

    [Fact]
    public void PonderileParceleiCuRosii_SuntCeleDinDocumentatie()
    {
        // Cele 12 stații ale serei de 24 × 12 m; centrul Zonei 1 e la (6, 3)
        var citiri = ServiciuAmplasareSenzori.StatiiClimat(24, 12)
            .Select(d => new CitireSenzor($"{d.Id}-T", d.Id, null, TipSenzor.Temperatura, 22, "°C", d.X, d.Y, DateTime.Now))
            .ToList();

        var ponderi = EstimatorClimat.Ponderi(citiri, TipSenzor.Temperatura, 6, 3);

        Assert.Equal(new[] { "CL2", "CL6", "CL1" }, ponderi.Select(p => p.Citire.DispozitivId));
        Assert.Equal(1.0, ponderi.Sum(p => p.Pondere), 6);
        Assert.Equal(0.51, ponderi[0].Pondere, 2);
    }

    [Fact]
    public void FaraCitiri_DaNull()
    {
        Assert.Null(EstimatorClimat.Estimeaza(Array.Empty<CitireSenzor>(), TipSenzor.Temperatura, 1, 1));
    }
}

public class TesteSenzori
{
    private static (ConfiguratieSera Config, IReadOnlyList<DispozitivSenzor> Dispozitive) SeraImplicita()
    {
        var config = ConfiguratieSera.Implicita();
        var dispozitive = new ServiciuAmplasareSenzori().Calculeaza(config, new ServiciuAmplasare().Calculeaza(config));
        return (config, dispozitive);
    }

    private static FabricaSenzoriSimulati Fabrica(MediuSeraSimulat mediu) =>
        new(mediu, Fixturi.CatalogReal(), samanta: 1);

    [Fact]
    public void Fabrica_CreeazaCateUnSenzorPentruFiecareMarime()
    {
        var (config, dispozitive) = SeraImplicita();

        var senzori = Fabrica(new MediuSeraSimulat(new Random(1))).Creeaza(config, dispozitive);

        Assert.Equal(dispozitive.Sum(d => d.Tipuri.Count), senzori.Count);
        Assert.All(senzori.Where(s => s.Dispozitiv.EsteStatieClimat), s => Assert.True(InfoSenzori.EsteDeClimat(s.Tip)));
    }

    [Fact]
    public void ValorileRamanInDomeniulFizic()
    {
        var (config, dispozitive) = SeraImplicita();
        var mediu = new MediuSeraSimulat(new Random(42));
        var senzori = Fabrica(mediu).Creeaza(config, dispozitive);

        for (int pas = 0; pas < 300; pas++)
        {
            mediu.Pas();
            foreach (var s in senzori)
            {
                var domeniu = InfoSenzori.DomeniuFizic(s.Tip);
                Assert.InRange(s.Citeste().Valoare, domeniu.Min, domeniu.Max);
            }
        }
    }

    [Fact]
    public void StatiileDeClimat_AratValoriApropiate_PentruCaAerulEComun()
    {
        var (config, dispozitive) = SeraImplicita();
        var mediu = new MediuSeraSimulat(new Random(3));
        var senzori = Fabrica(mediu).Creeaza(config, dispozitive);

        for (int pas = 0; pas < 50; pas++)
        {
            mediu.Pas();
            var temperaturi = senzori.Where(s => s.Tip == TipSenzor.Temperatura).Select(s => s.Citeste().Valoare).ToList();
            // diferențe doar din poziție (pereți) și din eroarea senzorilor: sub 3 °C între oricare două stații
            Assert.True(temperaturi.Max() - temperaturi.Min() < 3.0, $"Diferență prea mare: {temperaturi.Min()}–{temperaturi.Max()}");
        }
    }

    [Fact]
    public void SenzorRecreat_IsiPastreazaEroareaDeCalibrare()
    {
        var (config, dispozitive) = SeraImplicita();
        var mediu = new MediuSeraSimulat(new Random(5));
        var fabrica = Fabrica(mediu);

        var primul = fabrica.Creeaza(config, dispozitive).First(s => s.Id == "CL1-Temperatura").Citeste().Valoare;
        var alDoilea = fabrica.Creeaza(config, dispozitive).First(s => s.Id == "CL1-Temperatura").Citeste().Valoare;

        Assert.InRange(alDoilea, primul - 0.3, primul + 0.3); // diferă doar prin zgomotul de măsurare
    }

    [Fact]
    public void Monitorizarea_CitesteTotiSenzorii()
    {
        var (config, dispozitive) = SeraImplicita();
        using IServiciuMonitorizare monitor = new ServiciuMonitorizare(Fabrica(new MediuSeraSimulat(new Random(1))));

        monitor.Configureaza(config, dispozitive);
        var citiri = monitor.CitesteToti();

        Assert.Equal(dispozitive.Sum(d => d.Tipuri.Count), citiri.Count);
        Assert.Contains(citiri, c => c.ZonaId is null && c.Tip == TipSenzor.Temperatura);
        Assert.DoesNotContain(citiri, c => c.ZonaId is not null && InfoSenzori.EsteDeClimat(c.Tip));
    }

    [Fact]
    public async Task Monitorizarea_TrimiteCitiriPeriodic()
    {
        var (config, dispozitive) = SeraImplicita();
        using var monitor = new ServiciuMonitorizare(Fabrica(new MediuSeraSimulat(new Random(1))));
        monitor.Configureaza(config, dispozitive);
        var primite = new TaskCompletionSource<IReadOnlyList<CitireSenzor>>();
        monitor.CitiriNoi += (_, c) => primite.TrySetResult(c);

        monitor.Porneste(TimeSpan.FromMilliseconds(50));
        var castigator = await Task.WhenAny(primite.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Same(primite.Task, castigator);
        Assert.NotEmpty(await primite.Task);
    }
}
