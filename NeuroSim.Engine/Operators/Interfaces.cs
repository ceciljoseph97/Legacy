// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Operators/Interfaces.cs
using NeuroSim.Engine.Genomes;
using NeuroSim.Engine.Population;

namespace NeuroSim.Engine.Operators;

public interface ISelectionOperator<G> where G : Genome
{
    /// <summary>Returns <paramref name="count"/> individuals chosen from population.</summary>
    List<Individual<G>> Select(PopulationBase<G> population, int count, Random rng);
}

public interface ICrossoverOperator<G> where G : Genome
{
    /// <summary>Produce two children from two parents.</summary>
    (G child1, G child2) Crossover(G parent1, G parent2, Random rng);
}

public interface IMutationOperator<G> where G : Genome
{
    /// <summary>Return a mutated copy of <paramref name="genome"/>.</summary>
    G Mutate(G genome, double rate, Random rng);
}

public interface IFitnessEvaluator<G> where G : Genome
{
    double Evaluate(G genome);
    string Name { get; }
    string Description { get; }
}
