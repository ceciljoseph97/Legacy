<!-- AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/docs/ARCHITECTURE.md -->
# Architecture

Casual reference. Not a spec.

---

## Layering

```
NeuroSim.UI          → WPF, ViewModels, Views
NeuroSim.Problems    → TspEvolver, MazeEvolver, problem definitions
NeuroSim.Engine       → GAEngine<G>, genomes, operators, fitness
```

UI depends on Problems. Problems depends on Engine. Engine is standalone.

---

## Evolvers

**TspEvolver** and **MazeEvolver** are self-contained. They don't use the generic `GAEngine<G>` — they use raw `int[]` and their own selection/crossover/mutation. Reason: problem-specific operators (OX, 2-opt, Inversion) don't fit the Engine's generic interfaces cleanly. TspGASetup and CspGASetup *do* use the Engine for permutation problems.

**Engine** (`GAEngineFactory`): Binary, Real, Tree, Graph. No Permutation — use TspGASetup/CspGASetup for that.

---

## Playground pattern

1. **ViewModel** extends `PlaygroundViewModelBase`
2. Override `RunEvolutionAsync` — create evolver, subscribe `OnGeneration`, run
3. Override `ResetForNewRun` — clear plots, route, stats
4. Override `TotalGenerations` — for progress bar
5. Expose `StatBadges`, `Charts` for `PlaygroundAnalysisView`
6. Expose `GenomeDisplayInfo` for genome display
7. Snapshot: define DTO in `NeuroSim.UI.Models`, save/load JSON

`PlaygroundAnalysisView` is reusable. Pass `StatBadges`, `Charts`, `ExtraContent`. Genome display goes in ExtraContent via `GenomeDisplayView`.

---

## Genome display

`GenomeDisplayInfo` + `GenomeDisplayView`. Types: Permutation, DirectionSequence, Binary, RealValued, Graph. Each playground provides one; view renders based on `DisplayType`. Graph shows edge list when present.

---

## Snapshot format

TSP: `ExperimentSnapshot` — cities, config, BestTour, History. JSON.

Maze: `MazeExperimentSnapshot` — maze serialised (0/1 grid + Sx,y Gx,y), config, History. JSON.

EditableMaze: `Serialise()` / `Deserialise()` — compact string for grid + start/goal.

---

## Tests

**Unit** (`NeuroSim.Problems.Tests`): Short runs (2–5 gens), single components. TspProblem, MazeProblem, EditableMaze, evolver sanity.

**System** (`NeuroSim.SystemTests`): Full runs (20–100 gens), snapshot file I/O, integration. No UI ref (avoids WPF/net9.0-windows mismatch).
