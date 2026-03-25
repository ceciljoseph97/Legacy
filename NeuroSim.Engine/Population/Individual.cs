// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Engine/Population/Individual.cs
using NeuroSim.Engine.Genomes;

namespace NeuroSim.Engine.Population;

public sealed class Individual<G> where G : Genome
{
    public G Genome { get; set; }
    public double Fitness { get; set; }
    public int Age { get; set; }

    public Individual(G genome)
    {
        Genome = genome;
        Fitness = double.NegativeInfinity;
    }

    public Individual<G> Clone() => new((G)Genome.Clone()) { Fitness = Fitness, Age = Age };
}
