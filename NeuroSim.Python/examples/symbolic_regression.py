# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Python/examples/symbolic_regression.py
"""
Symbolic Regression — find an expression that approximates y = x² + x + 1.
Uses the Tree genome type (Genetic Programming).
"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from neurosim import run_experiment, GenomeType


def on_gen(s):
    if s.generation % 25 == 0:
        print(f"  Gen {s.generation:4d} | best={s.best_fitness:.4f} | expr: {s.best_genome()[:60]}")


print("=== NeuroSim: Symbolic Regression (Tree GP) ===")
result = run_experiment(
    genome_type=GenomeType.Tree,
    population_size=200,
    max_generations=100,
    mutation_rate=0.1,
    crossover_rate=0.9,
    fitness_name="SymbolicRegression",
    random_seed=7,
    on_generation=on_gen,
)

print(f"\nDone in {result.generations_run} generations.")
print(f"Best fitness : {result.best_fitness:.6f}  (0 = perfect)")
print(f"Best expr    : {result.best_genome}")
