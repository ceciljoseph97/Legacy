<!-- AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/CHANGELOG.md -->
# Changelog

All notable changes to NeuroSim.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Added
- TSP playground: GA, ES, EP, GP paradigms
- Maze playground: GA, ES, EP paradigms
- Genome display (permutation, direction sequence, binary, real, graph)
- Snapshot save/load for TSP and Maze
- Editable maze grid (start, goal, walls)
- Unit tests (NeuroSim.Problems.Tests)
- System tests (NeuroSim.SystemTests)
- docs/: ARCHITECTURE, ADDING_A_PLAYGROUND, OPERATORS

### Changed
- Modular PlaygroundAnalysisView, GenomeDisplayView

---

## [1.0.0] - 2025-03-16

### Added
- Initial release
- NeuroSim.Engine: Binary, Real, Tree, Graph genomes
- NeuroSim.Problems: TSP, Maze, CSP
- NeuroSim.UI: WPF app, playgrounds, export
