// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/TSP/TspDatasets.cs
namespace NeuroSim.Problems.TSP;

/// <summary>Embedded standard TSP benchmark instances.</summary>
public static class TspDatasets
{
    public static TspProblem Ulysses16 { get; } = new()
    {
        Name = "Ulysses16 (16 cities)",
        Cities =
        [
            new("1",  38.24, 20.42), new("2",  39.57, 26.15), new("3",  40.56, 25.32),
            new("4",  36.26, 23.12), new("5",  33.48, 10.54), new("6",  37.56, 12.19),
            new("7",  38.42, 13.11), new("8",  37.52, 20.44), new("9",  41.23,  9.10),
            new("10", 41.17, 13.05), new("11", 36.08, -5.21), new("12", 38.47, 15.13),
            new("13", 38.15, 15.35), new("14", 37.51, 15.17), new("15", 35.49, 14.32),
            new("16", 39.36, 19.56)
        ]
    };

    public static TspProblem Berlin52 { get; } = new()
    {
        Name = "Berlin52 (52 cities)",
        Cities =
        [
            new("1",  565,  575), new("2",  25,   185), new("3",  345,  750), new("4",  945,  685),
            new("5",  845,  655), new("6",  880,  660), new("7",  25,   230), new("8",  525,  1000),
            new("9",  580,  1175), new("10", 650,  1130), new("11", 1605, 620), new("12", 1220, 580),
            new("13", 1465, 200), new("14", 1530, 5),   new("15", 845,  680), new("16", 725,  370),
            new("17", 145,  665), new("18", 415,  635), new("19", 510,  875), new("20", 560,  365),
            new("21", 300,  465), new("22", 520,  585), new("23", 480,  415), new("24", 835,  625),
            new("25", 975,  580), new("26", 1215, 245), new("27", 1320, 315), new("28", 1250, 400),
            new("29", 660,  180), new("30", 410,  250), new("31", 420,  555), new("32", 575,  665),
            new("33", 1150, 1160), new("34", 700,  580), new("35", 685,  595), new("36", 685,  610),
            new("37", 770,  610), new("38", 795,  645), new("39", 720,  635), new("40", 760,  650),
            new("41", 475,  960), new("42", 95,   260), new("43", 875,  920), new("44", 700,  500),
            new("45", 555,  815), new("46", 830,  485), new("47", 1170, 65),  new("48", 830,  610),
            new("49", 605,  625), new("50", 595,  360), new("51", 1340, 725), new("52", 1740, 245)
        ]
    };

    public static TspProblem Random(int n, int seed = 42)
    {
        var rng = new Random(seed);
        return new TspProblem
        {
            Name = $"Random{n} (seed={seed})",
            Cities = Enumerable.Range(1, n)
                .Select(i => new TspCity(i.ToString(),
                    rng.NextDouble() * 1000,
                    rng.NextDouble() * 1000))
                .ToArray()
        };
    }

    public static TspProblem[] All =>
        [Ulysses16, Berlin52, Random(30), Random(50, 99)];
}
