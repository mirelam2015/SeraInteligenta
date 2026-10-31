using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;
using SeraInteligenta.Infrastructure.Date;

namespace SeraInteligenta.Tests;

/// <summary>
/// Catalog fals, ținut în memorie. Aici se vede avantajul IoC: regulile primesc ICatalogPlante,
/// deci în teste le dăm o implementare simplă, fără fișiere.
/// </summary>
internal sealed class CatalogInMemorie : ICatalogPlante
{
    public CatalogInMemorie(params ProfilPlanta[] plante) => Toate = plante;

    public IReadOnlyList<ProfilPlanta> Toate { get; }

    public ProfilPlanta? Gaseste(string? idPlanta) =>
        Toate.FirstOrDefault(p => string.Equals(p.IdPlanta, idPlanta, StringComparison.OrdinalIgnoreCase));
}

internal static class Fixturi
{
    /// <summary>Catalogul real din Date/plante_catalog.json (copiat lângă teste la build).</summary>
    public static ICatalogPlante CatalogReal() =>
        new CatalogPlanteJson(new OptiuniDate(Path.Combine(AppContext.BaseDirectory, "Date")));

    public static ConfiguratieSera Sera(double lungime, double latime, double alee = 1.0, params ZonaCultura[] zone) => new()
    {
        Sera = new DimensiuniSera { Lungime = lungime, Latime = latime, Inaltime = 3.8, LatimeAlee = alee, ProcentMinimCuloare = 15 },
        Zone = zone.ToList()
    };

    public static ZonaCultura Zona(int id, double lungime, double latime, string planta = "rosii", string secundara = "") => new()
    {
        Id = id,
        Nume = $"Zona {id}",
        Lungime = lungime,
        Latime = latime,
        PlantaPrincipalaId = planta,
        PlantaSecundaraId = secundara
    };

    public static ZonaCultura[] PatruZone(double lungime, double latime) =>
        Enumerable.Range(1, 4).Select(i => Zona(i, lungime, latime)).ToArray();
}
