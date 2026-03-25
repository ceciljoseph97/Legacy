// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Selection/TournamentSelection.cs
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Population;

namespace NeuroSim.Engine.Operators.Selection;

/// <summary>k-tournament selection — run k random trials, pick the fittest.</summary>
public sealed class TournamentSelection<G> : ISelectionOperator<G> where G : Genome
{
    private readonly int _k;

    public TournamentSelection(int k = 3)
    {
        if (k < 1) throw new ArgumentOutOfRangeException(nameof(k));
        _k = k;
    }

    public List<Individual<G>> Select(PopulationBase<G> population, int count, Random rng)
    {
        var inds = population.Individuals;
        var selected = new List<Individual<G>>(count);

        for (int s = 0; s < count; s++)
        {
            var best = inds[rng.Next(inds.Count)];
            for (int j = 1; j < _k; j++)
            {
                var challenger = inds[rng.Next(inds.Count)];
                if (challenger.Fitness > best.Fitness) best = challenger;
            }
            selected.Add(best);
        }
        return selected;
    }
}
