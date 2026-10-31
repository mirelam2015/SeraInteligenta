namespace SeraInteligenta.Core.Models;

/// <summary>O parcelă de cultură din seră, cu o plantă principală și, opțional, una secundară.</summary>
public sealed class ZonaCultura
{
    public int Id { get; set; }
    public string Nume { get; set; } = string.Empty;

    /// <summary>Dimensiunea pe lungimea serei, în metri.</summary>
    public double Lungime { get; set; } = 10.0;

    /// <summary>Dimensiunea pe lățimea serei, în metri.</summary>
    public double Latime { get; set; } = 4.0;

    public string PlantaPrincipalaId { get; set; } = string.Empty;

    /// <summary>Gol dacă zona are o singură cultură.</summary>
    public string PlantaSecundaraId { get; set; } = string.Empty;

    public double SuprafataMp => Lungime * Latime;
    public bool AreDouaPlante => !string.IsNullOrWhiteSpace(PlantaSecundaraId);
}
