namespace SeraInteligenta.Core.Sensors;

using System;
using SeraInteligenta.Core.Interfaces;

public class SenzorUmiditateSimulat : ISenzor
{
    private readonly Random _random;
    private const double ValoareMinima = 20.0;
    private const double ValoareMaxima = 80.0;

    public double Valoare { get; private set; }

    public SenzorUmiditateSimulat(double valoareInitiala = 50.0, int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        Valoare = Math.Clamp(valoareInitiala, ValoareMinima, ValoareMaxima);
    }

    public double CitesteValoare()
    {
        double delta = (_random.NextDouble() * 7.0) - 3.5;
        double nouaValoare = Valoare + delta;

        if (nouaValoare < ValoareMinima || nouaValoare > ValoareMaxima)
        {
            nouaValoare = ValoareMinima + (_random.NextDouble() * (ValoareMaxima - ValoareMinima));
        }

        Valoare = Math.Round(nouaValoare, 2);
        return Valoare;
    }
}