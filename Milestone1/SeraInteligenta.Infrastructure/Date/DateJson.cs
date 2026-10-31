using System.Text.Encodings.Web;
using System.Text.Json;
using SeraInteligenta.Core.Interfaces;
using SeraInteligenta.Core.Models;

namespace SeraInteligenta.Infrastructure.Date;

/// <summary>Unde se află fișierele de date. Înregistrat în containerul IoC.</summary>
public sealed class OptiuniDate
{
    public OptiuniDate(string folderDate) => FolderDate = folderDate;

    public string FolderDate { get; }
    public string FisierCatalog => Path.Combine(FolderDate, "plante_catalog.json");
    public string FisierConfiguratie => Path.Combine(FolderDate, "configuratie_sera.json");
}

internal static class OptiuniJson
{
    public static readonly JsonSerializerOptions Implicite = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // Proprietățile calculate (SuprafataTotala, SuprafataMp…) nu se scriu în fișier
        IgnoreReadOnlyProperties = true,
        // Păstrează diacriticele lizibile în fișierul salvat
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}

/// <summary>Catalogul de plante citit o singură dată din plante_catalog.json.</summary>
public sealed class CatalogPlanteJson : ICatalogPlante
{
    private readonly Dictionary<string, ProfilPlanta> _dupaId;

    public CatalogPlanteJson(OptiuniDate optiuni)
    {
        if (!File.Exists(optiuni.FisierCatalog))
            throw new FileNotFoundException($"Lipsește catalogul de plante: {optiuni.FisierCatalog}", optiuni.FisierCatalog);

        var plante = JsonSerializer.Deserialize<List<ProfilPlanta>>(File.ReadAllText(optiuni.FisierCatalog), OptiuniJson.Implicite)
                     ?? new List<ProfilPlanta>();

        Toate = plante.OrderBy(p => p.Nume, StringComparer.CurrentCulture).ToList();
        _dupaId = plante
            .Where(p => !string.IsNullOrWhiteSpace(p.IdPlanta))
            .GroupBy(p => p.IdPlanta, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ProfilPlanta> Toate { get; }

    public ProfilPlanta? Gaseste(string? idPlanta) =>
        !string.IsNullOrWhiteSpace(idPlanta) && _dupaId.TryGetValue(idPlanta, out var p) ? p : null;
}

/// <summary>Încarcă și salvează configurația serei în configuratie_sera.json.</summary>
public sealed class DepozitConfiguratieJson : IDepozitConfiguratie
{
    private readonly OptiuniDate _optiuni;

    public DepozitConfiguratieJson(OptiuniDate optiuni) => _optiuni = optiuni;

    public ConfiguratieSera Incarca()
    {
        if (!File.Exists(_optiuni.FisierConfiguratie))
            return ConfiguratieSera.Implicita();

        var config = JsonSerializer.Deserialize<ConfiguratieSera>(File.ReadAllText(_optiuni.FisierConfiguratie), OptiuniJson.Implicite);
        return config ?? ConfiguratieSera.Implicita();
    }

    public void Salveaza(ConfiguratieSera configuratie)
    {
        Directory.CreateDirectory(_optiuni.FolderDate);
        File.WriteAllText(_optiuni.FisierConfiguratie, JsonSerializer.Serialize(configuratie, OptiuniJson.Implicite));
    }
}
