namespace SeraInteligenta.Core.Models;

/// <summary>Geometria serei și parametrii pentru căile de acces.</summary>
public sealed class DimensiuniSera
{
    /// <summary>Lungimea serei (axa X a hărții), în metri.</summary>
    public double Lungime { get; set; } = 24.0;

    /// <summary>Lățimea serei (axa Y a hărții), în metri.</summary>
    public double Latime { get; set; } = 12.0;

    /// <summary>Înălțimea la coamă, în metri.</summary>
    public double Inaltime { get; set; } = 3.8;

    /// <summary>
    /// Lățimea aleilor: pe margini, între coloanele și între rândurile de parcele.
    /// Minim recomandat 0,8 m, ca să treacă o roabă sau un cărucior.
    /// </summary>
    public double LatimeAlee { get; set; } = 1.0;

    /// <summary>Procentul minim din suprafață care trebuie să rămână liber pentru acces.</summary>
    public double ProcentMinimCuloare { get; set; } = 15.0;

    public double SuprafataTotala => Lungime * Latime;
    public double VolumTotal => SuprafataTotala * Inaltime;
    public double SuprafataMinimaCuloare => SuprafataTotala * ProcentMinimCuloare / 100.0;
    public double SuprafataMaximaCultivabila => SuprafataTotala - SuprafataMinimaCuloare;
}
