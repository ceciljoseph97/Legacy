// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Selection/RankSelection.cs
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Population;

namespace NeuroSim.Engine.Operators.Selection;

/// <summary>Rank-based selection — assigns linear selection probability based on rank.</summary>
public sealed class RankSelection<G> : ISelectionOperator<G> where G : Genome
{
    public List<Individual<G>> Select(PopulationBase<G> population, int count, Random rng)
    {
        var sorted = population.Individuals.OrderBy(i => i.Fitness).ToList();
        int n = sorted.Count;
        double total = n * (n + 1) / 2.0;

        var selected = new List<Individual<G>>(count);
        for (int s = 0; s < count; s++)
        {
            double r = rng.NextDouble() * total;
            double cumulative = 0;
            for (int j = 0; j < n; j++)
            {
                cumulative += j + 1;
                if (cumulative >= r) { selected.Add(sorted[j]); break; }
            }
            if (selected.Count < s + 1) selected.Add(sorted[^1]);
        }
        return selected;
    }
}
