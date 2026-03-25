# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Python/examples/binary_ga.py
"""
Binary Genome GA — OneMax problem.
Maximise the number of 1-bits in a 50-bit string.
"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from neurosim import run_experiment, GenomeType


def on_gen(s):
    if s.generation % 20 == 0:
        print(f"  Gen {s.generation:4d} | best={s.best_fitness:.4f} "
              f"| mean={s.mean_fitness:.4f} | {s.best_genome()[:20]}…")


print("=== NeuroSim: Binary OneMax (50 bits) ===")
result = run_experiment(
    genome_type=GenomeType.Binary,
    population_size=100,
    max_generations=200,
    genome_length=50,
    mutation_rate=0.02,
    crossover_rate=0.8,
    fitness_name="OneMax",
    on_generation=on_gen,
)

print(f"\nDone in {result.generations_run} generations.")
print(f"Best fitness : {result.best_fitness:.6f}")
print(f"Best genome  : {result.best_genome}")
