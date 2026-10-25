namespace SeraInteligenta.Core.Models;

using System;
using System.Collections.Generic;

public class ZonaCultura
{
    public int Id { get; set; }
    public string NumeZona { get; set; } = string.Empty;

    public double Lungime { get; set; } = 10.0;
    public double Latime { get; set; } = 5.0;
    public double SuprafataMp
    {
        get => _suprafataMp > 0 ? _suprafataMp : Math.Round(Lungime * Latime, 2);
        set => _suprafataMp = value;
    }
    private double _suprafataMp;

    /// <summary>
    /// ID-ul plantei selectate din catalogul JSON (ex: "rosii", "castraveti").
    /// </summary>
    public string PlantaSelectataId { get; set; } = string.Empty;

    public double UmiditateTinta { get; set; }
    public double LuminaTinta { get; set; }
    public List<string> PlanteCompatibile { get; set; } = new List<string>();

    public ZonaCultura() { }

    public ZonaCultura(int id, string nume, double lungime, double latime, string plantaId, double umid, double lux, List<string>? compatibile = null)
    {
        Id = id;
        NumeZona = nume;
        Lungime = lungime;
        Latime = latime;
        _suprafataMp = Math.Round(lungime * latime, 2);
        PlantaSelectataId = plantaId;
        UmiditateTinta = umid;
        LuminaTinta = lux;
        PlanteCompatibile = compatibile ?? new List<string>();
    }

    /// <summary>
    /// Aplică setările agronomice ale unei plante selectate din catalog.
    /// </summary>
    public void AplicaProfilPlanta(ProfilPlanta planta)
    {
        if (planta == null) return;
        PlantaSelectataId = planta.IdPlanta;
        UmiditateTinta = planta.UmiditateOptima;
        LuminaTinta = planta.LuminaOptima;
    }
}