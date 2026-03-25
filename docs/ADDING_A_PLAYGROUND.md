<!-- AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/docs/ADDING_A_PLAYGROUND.md -->
# Adding a Playground

Step-by-step. Assumes you know the problem and genome encoding.

---

## 1. Problem layer (NeuroSim.Problems)

Create `XxxProblem`, `XxxRunConfig`, `XxxEvolver`.

**Config** — paradigm enum, operators, pop size, generations, rates, seed. See `TspRunConfig` or `MazeRunConfig`.

**Evolver** — `RunAsync(CancellationToken)`, `OnGeneration` event. Genome = `int[]` or whatever fits. Implement GA loop (selection → crossover → mutation → elitism). Add ES/EP if you want. Use `GenerationDelayMs = 0` for tests.

**Stats record** — e.g. `XxxGenStats(Generation, BestFitness, MeanFitness, BestGenome, ...)`. Fire it from `OnGeneration`.

---

## 2. UI layer (NeuroSim.UI)

**ViewModel** — extend `PlaygroundViewModelBase`. Inject `NavigationService` (or mock for tests). Override:

- `RunEvolutionAsync` — build evolver from config, subscribe `OnGeneration`, call `evolver.RunAsync(ct)`
- `ResetForNewRun` — clear charts, best solution display
- `TotalGenerations` — return `MaxGenerations`

Add paradigm/operator combos, params. Expose `StatBadges`, `Charts`, `GenomeDisplay` (from `GenomeDisplayInfo`).

**View** — XAML. Left: config panel. Right: tabs (main viz, Analysis, Reference). Use `PlaygroundAnalysisView` for Analysis tab. Put `GenomeDisplayView` in `ExtraContent`.

**Navigation** — add entry in `HomePage` / `ProblemSelectorView`, wire `Navigate` to your ViewModel.

---

## 3. Snapshot (optional)

Define `XxxExperimentSnapshot` in `NeuroSim.UI.Models`. Fields: config, problem state, BestGenome/BestResult, History. Serialise to JSON. Load restores config + replays history into charts.

---

## 4. Tests

**Unit** — `NeuroSim.Problems.Tests`: problem creation, evolver runs 2–5 gens, OnGeneration fires, EditableXxx round-trip if applicable.

**System** — `NeuroSim.SystemTests`: full run (50+ gens), snapshot JSON round-trip, file write/read.

---

## Checklist

- [ ] XxxProblem, XxxRunConfig, XxxEvolver
- [ ] XxxPlaygroundViewModel extends PlaygroundViewModelBase
- [ ] XxxPlaygroundView with config + tabs
- [ ] PlaygroundAnalysisView with StatBadges, Charts, GenomeDisplayView
- [ ] GenomeDisplayInfo with correct DisplayType
- [ ] Snapshot DTO + save/load
- [ ] Unit tests
- [ ] System tests
