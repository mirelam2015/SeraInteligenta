namespace SeraInteligenta.Core.Sensors;

using System;
using SeraInteligenta.Core.Interfaces;

public class SenzorLuminaSimulata : ISenzor
{
    private readonly Random _random;
    private const double ValoareMinima = 0.0;
    private const double ValoareMaxima = 1000.0;

    public double Valoare { get; private set; }

    public SenzorLuminaSimulata(double valoareInitiala = 450.0, int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        Valoare = Math.Clamp(valoareInitiala, ValoareMinima, ValoareMaxima);
    }

    public double CitesteValoare()
    {
        double delta = (_random.NextDouble() * 80.0) - 40.0;
        double nouaValoare = Valoare + delta;

        if (nouaValoare < ValoareMinima || nouaValoare > ValoareMaxima)
        {
            nouaValoare = ValoareMinima + (_random.NextDouble() * (ValoareMaxima - ValoareMinima));
        }

        Valoare = Math.Round(nouaValoare, 2);
        return Valoare;
    }
}