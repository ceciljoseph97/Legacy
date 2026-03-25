<!-- AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/README.md -->
# NeuroSim (Experimental)

**Source signature:** `AUTH:DEVNEUROSIM:7A3F9E2B` — see [SIGNATURE.md](../docs/SIGNATURE.md) (repo root).

Evolutionary computation simulation framework. Run GA, ES, EP, GP on benchmark problems, watch populations evolve in real time.

**TL;DR:** WPF app. TSP + Maze playgrounds. Modular evolvers, snapshot save/load, genome display.

![Watch demo video](Snapshots/NsimSample.mp4)

---

## Run it

```bash
cd devNeuroSim
dotnet run --project NeuroSim.UI
```

Or open `devNeuroSim.sln` in Visual Studio and hit F5.

---

## What's in here

| Project | What it does |
|---------|--------------|
| **NeuroSim.Engine** | Generic GA engine. Binary, Real, Tree, Graph, Permutation genomes. Selection, crossover, mutation interfaces. |
| **NeuroSim.Problems** | TSP (TspEvolver), Maze (MazeEvolver), CSP. Self-contained evolvers with GA/ES/EP/GP. |
| **NeuroSim.UI** | WPF app. Playgrounds, analysis panels, genome display, export. |
| **NeuroSim.Problems.Tests** | Unit tests for TSP + Maze. |
| **NeuroSim.SystemTests** | Integration tests. Full runs, snapshot round-trip. |

---

## Playgrounds

**TSP** — Traveling Salesperson. Permutation genome. GA/ES/EP/GP. OX, PMX, 2-opt, Or-opt. Ulysses16, Berlin52, random instances.

**Maze** — Path-finding. Direction sequence (N/E/S/W). GA/ES/EP. Inversion, SegmentShuffle, BlockReset. Editable grid, presets.

Both: paradigm + selection + crossover + mutation configurable. Snapshot save/load. Genome shown in Analysis tab.

---

## Tests

```bash
dotnet test NeuroSim.Problems.Tests   # unit
dotnet test NeuroSim.SystemTests     # system
dotnet test                          # all
```

## CI/CD

GitHub Actions: `.github/workflows/` — build, unit tests, system tests, compliance. Compliance runs `scripts/compliance.sh` (Linux). Pass = state Good (10) or Perfect (11). Set `COMPLIANCE_REQUIRE_PERFECT=true` to require Perfect only.

---

## Adding a playground

1. **NeuroSim.Problems**: add `XxxEvolver`, `XxxRunConfig`, `XxxProblem`.
2. **NeuroSim.UI**: extend `PlaygroundViewModelBase`, add view, wire `GenomeDisplayInfo`.
3. Follow TSP/Maze pattern — `PlaygroundAnalysisView`, `StatBadges`, `Charts`, `ExtraContent`.

See `docs/ARCHITECTURE.md` for details.

---

## Genome types

| Type | Used by | Changeable? |
|------|---------|-------------|
| Permutation | TSP, CSP | No — problem dictates it |
| DirectionSequence | Maze | No |
| Binary | Engine (OneMax, Trap) | — |
| Graph | Engine (MaxSpanning) | — |
| RealValued | Engine (Sphere, GP weight vector) | — |

---

## Requirements

- .NET 8+ (Engine, Problems, Tests)
- .NET 9 (UI, SystemTests)
- Windows (WPF)
