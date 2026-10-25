namespace SeraInteligenta.Core.Models;

/// <summary>
/// Reprezintă o specie sau un soi de plantă din catalogul de resurse JSON.
/// </summary>
public class ProfilPlanta
{
    public string IdPlanta { get; set; } = string.Empty;
    public string Nume { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public double UmiditateOptima { get; set; }
    public double LuminaOptima { get; set; }
    public double TemperaturaOptima { get; set; }
    public string Descriere { get; set; } = string.Empty;

    public override string ToString() => $"{Nume} ({Categorie})";
}