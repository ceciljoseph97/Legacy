# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Python/neurosim/__init__.py
"""
NeuroSim Python API — wraps the C++ core for scripting & experimentation.

Usage:
    from neurosim import run_experiment, EvolutionConfig, GenomeType

    result = run_experiment(
        genome_type=GenomeType.RealValued,
        population_size=100,
        max_generations=200,
        fitness_name="Sphere",
        on_generation=lambda s: print(f"Gen {s.generation}: {s.best_fitness:.4f}")
    )
    print("Best genome:", result.best_genome)
"""

from __future__ import annotations
from dataclasses import dataclass, field
from typing import Callable, Optional
import importlib, sys

# Try to import the compiled C++ module; fall back to pure-Python stub
try:
    from neurosim_core import (  # type: ignore
        EvolutionConfig, GenomeType, GenerationStats, Engine,
        default_config, create_engine
    )
    _HAVE_NATIVE = True
except ImportError:
    _HAVE_NATIVE = False
    from neurosim._stub import (
        EvolutionConfig, GenomeType, GenerationStats, Engine,
        default_config, create_engine
    )


@dataclass
class ExperimentResult:
    best_fitness: float
    best_genome: str
    generations_run: int
    stats: list = field(default_factory=list)


def run_experiment(
    genome_type: "GenomeType" = None,
    population_size: int = 100,
    max_generations: int = 200,
    genome_length: int = 30,
    crossover_rate: float = 0.8,
    mutation_rate: float = 0.01,
    tournament_size: int = 3,
    elite_ratio: float = 0.05,
    real_min: float = -5.0,
    real_max: float = 5.0,
    fitness_name: str = "OneMax",
    random_seed: int = 42,
    on_generation: Optional[Callable] = None,
) -> ExperimentResult:
    """High-level experiment runner."""
    if genome_type is None:
        genome_type = GenomeType.Binary

    cfg = default_config(genome_type)
    cfg.population_size = population_size
    cfg.max_generations = max_generations
    cfg.genome_length = genome_length
    cfg.crossover_rate = crossover_rate
    cfg.mutation_rate = mutation_rate
    cfg.tournament_size = tournament_size
    cfg.elite_ratio = elite_ratio
    cfg.real_min = real_min
    cfg.real_max = real_max
    cfg.random_seed = random_seed
    cfg.set_fitness_name(fitness_name)

    engine = create_engine(cfg)
    collected_stats: list[GenerationStats] = []

    def _cb(s: "GenerationStats"):
        collected_stats.append(s)
        if on_generation:
            on_generation(s)

    engine.set_callback(_cb)
    engine.initialize()
    engine.run()

    return ExperimentResult(
        best_fitness=engine.best_fitness(),
        best_genome=engine.best_genome(),
        generations_run=engine.generation_count(),
        stats=collected_stats,
    )


__all__ = [
    "EvolutionConfig", "GenomeType", "GenerationStats", "Engine",
    "default_config", "create_engine", "run_experiment", "ExperimentResult",
]
