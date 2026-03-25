<!-- AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/docs/OPERATORS.md -->
# Operators reference

Quick lookup. TSP and Maze use problem-specific operators; Engine has generic ones.

---

## TSP

| Operator | Type | What it does |
|----------|------|--------------|
| Tournament | Selection | k-way, pick best |
| Roulette | Selection | Fitness-proportionate |
| Rank | Selection | Rank-based |
| OX | Crossover | Order crossover — copy segment, fill from other parent |
| PMX | Crossover | Partially mapped |
| Edge Recombination | Crossover | Build from adjacency |
| 2-opt | Mutation | Reverse segment (removes crossings) |
| Swap | Mutation | Swap two cities |
| Relocate | Mutation | Move one city |
| Or-opt | Mutation | Move chain of 2–3 cities |

GP: weight vector, greedy tour construction. BLX-α crossover, Gaussian mutation.

---

## Maze

| Operator | Type | What it does |
|----------|------|--------------|
| Tournament | Selection | k-way |
| Roulette | Selection | Fitness-proportionate |
| Rank | Selection | Rank-based |
| Uniform | Crossover | Each gene from either parent |
| TwoPoint | Crossover | Cut-and-splice |
| SinglePoint | Crossover | Single cut |
| Inversion | Mutation | Reverse sub-sequence (best default) |
| SegmentShuffle | Mutation | Shuffle sub-segment |
| BlockReset | Mutation | Overwrite block with random |
| PointMutation | Mutation | Replace single gene |

---

## Engine (Binary, Real, Tree, Graph)

**Selection:** Tournament, Roulette, Rank (same idea).

**Binary:** SinglePoint, TwoPoint, Uniform crossover. FlipBit mutation.

**Real:** Blend, SBX, Arithmetic crossover. Gaussian, Polynomial mutation.

**Tree:** Subtree crossover, Subtree mutation.

**Graph:** Edge-uniform crossover, Edge-toggle mutation.
