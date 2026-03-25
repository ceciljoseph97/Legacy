// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/TSP/TspProblem.cs
namespace NeuroSim.Problems.TSP;

public sealed record TspCity(string Name, double X, double Y);

public sealed class TspProblem
{
    public string Name { get; init; } = "TSP";
    public TspCity[] Cities { get; init; } = Array.Empty<TspCity>();

    private double[,]? _dist;

    public double[,] DistanceMatrix
    {
        get
        {
            if (_dist is not null) return _dist;
            int n = Cities.Length;
            _dist = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    _dist[i, j] = Euclidean(Cities[i], Cities[j]);
            return _dist;
        }
    }

    public static double Euclidean(TspCity a, TspCity b)
        => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    /// <summary>Route distance (returns raw positive km/units for display).</summary>
    public double RouteLength(int[] route)
    {
        var d = DistanceMatrix;
        double total = 0;
        for (int i = 0; i < route.Length; i++)
            total += d[route[i], route[(i + 1) % route.Length]];
        return total;
    }
}
