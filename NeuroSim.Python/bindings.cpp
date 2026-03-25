/**
 * NeuroSim Python pybind11 bindings.
 * Wraps the C API (NativeAPI.h) — no C++ internals exposed.
 *
 * Build:
 *   cmake -S NeuroSim.Python -B build_py && cmake --build build_py
 * Or via pip:
 *   pip install scikit-build-core pybind11
 *   cd NeuroSim.Python && pip install -e .
 */
#include <pybind11/pybind11.h>
#include <pybind11/functional.h>
#include <pybind11/stl.h>
#include <string>
#include <cstring>

// Include the C API only
#include "../NeuroSim.Core/include/api/NativeAPI.h"

namespace py = pybind11;

// ── Thin RAII wrapper so Python GC can own the handle ─────────────────────────
struct PyEngine {
    NSEngineHandle handle;
    py::function callback;

    explicit PyEngine(NSEvolutionConfig cfg) : handle(NS_CreateEngine(cfg)) {}
    ~PyEngine() { if (handle) { NS_Stop(handle); NS_DestroyEngine(handle); handle = nullptr; } }

    PyEngine(const PyEngine&) = delete;
    PyEngine& operator=(const PyEngine&) = delete;
};

PYBIND11_MODULE(neurosim_core, m) {
    m.doc() = "NeuroSim C++ genetic algorithm engine — Python bindings";

    // ── GenomeType ─────────────────────────────────────────────────────────
    py::enum_<NSGenomeType>(m, "GenomeType")
        .value("Binary",     NS_BINARY)
        .value("RealValued", NS_REAL_VALUED)
        .value("Tree",       NS_TREE)
        .value("Graph",      NS_GRAPH)
        .export_values();

    py::enum_<NSSelectionType>(m, "SelectionType")
        .value("Tournament",   NS_SEL_TOURNAMENT)
        .value("RouletteWheel",NS_SEL_ROULETTE)
        .value("Rank",         NS_SEL_RANK)
        .export_values();

    // ── EvolutionConfig ────────────────────────────────────────────────────
    py::class_<NSEvolutionConfig>(m, "EvolutionConfig")
        .def(py::init([]() { return NS_DefaultConfig(NS_BINARY); }))
        .def_readwrite("population_size",  &NSEvolutionConfig::populationSize)
        .def_readwrite("max_generations",  &NSEvolutionConfig::maxGenerations)
        .def_readwrite("elite_ratio",      &NSEvolutionConfig::eliteRatio)
        .def_readwrite("genome_type",      &NSEvolutionConfig::genomeType)
        .def_readwrite("genome_length",    &NSEvolutionConfig::genomeLength)
        .def_readwrite("real_min",         &NSEvolutionConfig::realMin)
        .def_readwrite("real_max",         &NSEvolutionConfig::realMax)
        .def_readwrite("tree_num_vars",    &NSEvolutionConfig::treeNumVars)
        .def_readwrite("tree_max_depth",   &NSEvolutionConfig::treeMaxDepth)
        .def_readwrite("graph_nodes",      &NSEvolutionConfig::graphNodes)
        .def_readwrite("crossover_rate",   &NSEvolutionConfig::crossoverRate)
        .def_readwrite("mutation_rate",    &NSEvolutionConfig::mutationRate)
        .def_readwrite("tournament_size",  &NSEvolutionConfig::tournamentSize)
        .def_readwrite("random_seed",      &NSEvolutionConfig::randomSeed)
        .def_readwrite("log_interval",     &NSEvolutionConfig::logInterval)
        .def_readwrite("use_target_fitness",&NSEvolutionConfig::useTargetFitness)
        .def_readwrite("target_fitness",   &NSEvolutionConfig::targetFitness)
        .def("set_fitness_name", [](NSEvolutionConfig& c, const std::string& n) {
            std::strncpy(c.fitnessName, n.c_str(), 63);
            c.fitnessName[63] = '\0';
        })
        .def("get_fitness_name", [](const NSEvolutionConfig& c) {
            return std::string(c.fitnessName);
        })
        .def("__repr__", [](const NSEvolutionConfig& c) {
            return std::string("<EvolutionConfig genome=") + std::to_string(c.genomeType)
                 + " pop=" + std::to_string(c.populationSize)
                 + " gens=" + std::to_string(c.maxGenerations) + ">";
        });

    // ── GenerationStats ────────────────────────────────────────────────────
    py::class_<NSGenerationStats>(m, "GenerationStats")
        .def_readonly("generation",    &NSGenerationStats::generation)
        .def_readonly("best_fitness",  &NSGenerationStats::bestFitness)
        .def_readonly("mean_fitness",  &NSGenerationStats::meanFitness)
        .def_readonly("std_dev",       &NSGenerationStats::stdDev)
        .def_readonly("worst_fitness", &NSGenerationStats::worstFitness)
        .def_readonly("evaluations",   &NSGenerationStats::evaluations)
        .def_readonly("elapsed_ms",    &NSGenerationStats::elapsedMs)
        .def("best_genome", [](const NSGenerationStats& s) { return std::string(s.bestGenome); })
        .def("__repr__", [](const NSGenerationStats& s) {
            return "<Stats gen=" + std::to_string(s.generation)
                 + " best=" + std::to_string(s.bestFitness) + ">";
        });

    // ── Engine ─────────────────────────────────────────────────────────────
    py::class_<PyEngine>(m, "Engine")
        .def(py::init<NSEvolutionConfig>(), py::arg("config"))
        .def("initialize", [](PyEngine& e) { NS_Initialize(e.handle); })
        .def("run", [](PyEngine& e) {
            // Release GIL while running so Python callbacks can acquire it
            py::gil_scoped_release release;
            NS_Run(e.handle);
        })
        .def("stop", [](PyEngine& e) { NS_Stop(e.handle); })
        .def("best_fitness", [](PyEngine& e) { return NS_GetBestFitness(e.handle); })
        .def("best_genome",  [](PyEngine& e) { return std::string(NS_GetBestGenome(e.handle)); })
        .def("generation_count", [](PyEngine& e) { return NS_GetGenerationCount(e.handle); })
        .def("get_stats", [](PyEngine& e, int gen) { return NS_GetStats(e.handle, gen); })
        .def("set_callback", [](PyEngine& e, py::function fn) {
            e.callback = fn;
            // Store raw pointer to the PyEngine for the callback
            NS_SetGenerationCallback(e.handle, [](NSGenerationStats s, void* ud) {
                auto* eng = static_cast<PyEngine*>(ud);
                py::gil_scoped_acquire gil;
                try { eng->callback(s); }
                catch (py::error_already_set& ex) { ex.discard_as_unraisable(__func__); }
            }, &e);
        });

    // ── Module-level helpers ───────────────────────────────────────────────
    m.def("default_config", [](NSGenomeType gt) { return NS_DefaultConfig(gt); },
          py::arg("genome_type") = NS_BINARY,
          "Return a default EvolutionConfig for the given genome type.");

    m.def("create_engine", [](NSEvolutionConfig cfg) {
        return std::make_unique<PyEngine>(cfg);
    }, py::arg("config"), "Create a new GA Engine from a config.");
}
