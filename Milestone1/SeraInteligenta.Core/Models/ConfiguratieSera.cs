namespace SeraInteligenta.Core.Models;

/// <summary>Tot ce descrie o seră: dimensiunile și zonele. Se salvează în Date/configuratie_sera.json.</summary>
public sealed class ConfiguratieSera
{
    public DimensiuniSera Sera { get; set; } = new();
    public List<ZonaCultura> Zone { get; set; } = new();

    /// <summary>Configurația folosită când fișierul lipsește. E validă și arată câteva tipuri de mesaje.</summary>
    public static ConfiguratieSera Implicita() => new()
    {
        Sera = new DimensiuniSera(),
        Zone =
        {
            new ZonaCultura { Id = 1, Nume = "Zona 1 – Nord-Vest", Lungime = 10, Latime = 4, PlantaPrincipalaId = "rosii", PlantaSecundaraId = "ierburi" },
            new ZonaCultura { Id = 2, Nume = "Zona 2 – Nord-Est", Lungime = 10, Latime = 4, PlantaPrincipalaId = "ardei", PlantaSecundaraId = "vinete" },
            new ZonaCultura { Id = 3, Nume = "Zona 3 – Sud-Vest", Lungime = 10, Latime = 4, PlantaPrincipalaId = "salata", PlantaSecundaraId = "spanac" },
            new ZonaCultura { Id = 4, Nume = "Zona 4 – Sud-Est", Lungime = 10, Latime = 4, PlantaPrincipalaId = "castraveti" }
        }
    };
}
