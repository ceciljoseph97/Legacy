// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Selection/RouletteWheelSelection.cs
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Population;

namespace NeuroSim.Engine.Operators.Selection;

/// <summary>Fitness-proportionate (roulette wheel) selection.</summary>
public sealed class RouletteWheelSelection<G> : ISelectionOperator<G> where G : Genome
{
    public List<Individual<G>> Select(PopulationBase<G> population, int count, Random rng)
    {
        var inds = population.Individuals;
        double minFit = inds.Min(i => i.Fitness);
        double[] adjusted = inds.Select(i => i.Fitness - minFit + 1e-9).ToArray();
        double total = adjusted.Sum();

        var selected = new List<Individual<G>>(count);
        for (int s = 0; s < count; s++)
        {
            double r = rng.NextDouble() * total;
            double cumulative = 0;
            for (int j = 0; j < inds.Count; j++)
            {
                cumulative += adjusted[j];
                if (cumulative >= r) { selected.Add(inds[j]); break; }
            }
            // fallback if fp rounding leaves us at end
            if (selected.Count < s + 1) selected.Add(inds[^1]);
        }
        return selected;
    }
}
