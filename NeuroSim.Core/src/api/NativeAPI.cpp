#define NEUROSIM_CORE_EXPORTS
#include "../../include/api/NativeAPI.h"
#include "../../include/genome/BinaryGenome.h"
#include "../../include/genome/RealValuedGenome.h"
#include "../../include/genome/TreeGenome.h"
#include "../../include/genome/GraphGenome.h"

#include <vector>
#include <memory>
#include <random>
#include <functional>
#include <chrono>
#include <algorithm>
#include <numeric>
#include <cstring>
#include <cmath>

// ── Minimal self-contained GA engine used by the C API ───────────────────────
// (The C++ engine is template-heavy; the C API exposes a type-erased layer)

struct NSEngineImpl {
    NSEvolutionConfig cfg;
    NSGenerationCallback callback = nullptr;
    void* callbackData = nullptr;

    // Results
    std::vector<NSGenerationStats> log;
    std::string bestGenome;
    double bestFitness = -1e300;
    bool running = false;

    // Population: (genome_str, fitness)
    std::vector<std::pair<std::string, double>> population;
    std::mt19937 rng;

    explicit NSEngineImpl(const NSEvolutionConfig& c) : cfg(c), rng(c.randomSeed) {}

    // ── Genome creation ────────────────────────────────────────────────────

    std::string MakeGenome() {
        if (cfg.genomeType == NS_BINARY) {
            NeuroSim::BinaryGenome g(cfg.genomeLength);
            g.Randomize();
            return g.Serialize();
        }
        if (cfg.genomeType == NS_REAL_VALUED) {
            NeuroSim::RealValuedGenome g(cfg.genomeLength, cfg.realMin, cfg.realMax);
            g.Randomize();
            return g.Serialize();
        }
        if (cfg.genomeType == NS_TREE) {
            NeuroSim::TreeGenome g(cfg.treeNumVars, cfg.treeMaxDepth);
            g.Randomize();
            return g.Serialize();
        }
        if (cfg.genomeType == NS_GRAPH) {
            NeuroSim::GraphGenome g(cfg.graphNodes, cfg.graphDirected != 0);
            g.Randomize();
            return g.Serialize();
        }
        return "";
    }

    // ── Built-in fitness evaluation from genome string ─────────────────────
    double EvalBuiltin(const std::string& gStr) {
        std::string name(cfg.fitnessName);
        if (cfg.genomeType == NS_BINARY) {
            if (name == "Trap") {
                int ones = 0;
                for (char c : gStr) if (c == '1') ++ones;
                int n = (int)gStr.size();
                int k = 5;
                int blocks = n / k;
                double total = 0;
                for (int b = 0; b < blocks; ++b) {
                    int bo = 0;
                    for (int i = b*k; i < (b+1)*k; ++i) if (gStr[i] == '1') ++bo;
                    total += (bo == k) ? k : k-1-bo;
                }
                return total / (blocks * k);
            }
            // OneMax
            int ones = 0;
            for (char c : gStr) if (c == '1') ++ones;
            return (double)ones / gStr.size();
        }
        if (cfg.genomeType == NS_REAL_VALUED) {
            // Parse "[v0, v1, ...]"
            std::vector<double> vals;
            auto pos = gStr.find('[');
            if (pos != std::string::npos) {
                std::string inner = gStr.substr(pos+1);
                for (char& c : inner) if (c == ',' || c == ']') c = ' ';
                std::istringstream iss(inner);
                double v;
                while (iss >> v) vals.push_back(v);
            }
            if (name == "Rastrigin") {
                double s = 10.0 * vals.size();
                for (double x : vals) s += x*x - 10*cos(2*M_PI*x);
                return -s;
            }
            if (name == "Ackley") {
                double n = vals.size();
                double sq = 0, cs = 0;
                for (double x : vals) { sq += x*x; cs += cos(2*M_PI*x); }
                return -(20*exp(-0.2*sqrt(sq/n)) + exp(cs/n) - 20 - M_E);
            }
            // Sphere
            double s = 0; for (double x : vals) s += x*x;
            return -s;
        }
        // Tree / Graph: just return string length as proxy fitness for now
        return -(double)gStr.size();
    }

    // ── Simple crossover (genome-string level) ─────────────────────────────
    std::string Crossover(const std::string& p1, const std::string& p2) {
        if (p1.size() != p2.size() || p1.empty()) return p1;
        std::uniform_int_distribution<int> pt(1, (int)p1.size()-1);
        int cut = pt(rng);
        return p1.substr(0, cut) + p2.substr(cut);
    }

    std::string Mutate(const std::string& g) {
        std::string result = g;
        std::bernoulli_distribution flip(cfg.mutationRate);
        for (auto& c : result) {
            if (cfg.genomeType == NS_BINARY && flip(rng))
                c = (c == '0') ? '1' : '0';
        }
        return result;
    }
};

// ── C API implementation ──────────────────────────────────────────────────────

#include <sstream>

extern "C" {

NSEvolutionConfig NS_DefaultConfig(NSGenomeType genomeType) {
    NSEvolutionConfig c{};
    c.populationSize = 100;
    c.maxGenerations = 200;
    c.eliteRatio     = 0.05;
    c.genomeType     = genomeType;
    c.genomeLength   = 30;
    c.realMin        = -5.0;
    c.realMax        =  5.0;
    c.treeNumVars    = 1;
    c.treeMaxDepth   = 5;
    c.graphNodes     = 8;
    c.graphDirected  = 0;
    c.selectionType  = NS_SEL_TOURNAMENT;
    c.tournamentSize = 3;
    c.binaryCrossover = NS_CX_SINGLE;
    c.realCrossover   = NS_CX_BLEND;
    c.binaryMutation  = NS_MUT_FLIP;
    c.realMutation    = NS_MUT_GAUSSIAN;
    c.crossoverRate   = 0.8;
    c.mutationRate    = 0.01;
    c.gaussianSigma   = 0.1;
    std::strncpy(c.fitnessName, genomeType == NS_BINARY ? "OneMax" : "Sphere", 64);
    c.useTargetFitness = 0;
    c.targetFitness    = 1.0;
    c.randomSeed       = 42;
    c.logInterval      = 1;
    return c;
}

NSEngineHandle NS_CreateEngine(NSEvolutionConfig config) {
    return new NSEngineImpl(config);
}

void NS_DestroyEngine(NSEngineHandle handle) {
    delete static_cast<NSEngineImpl*>(handle);
}

void NS_SetGenerationCallback(NSEngineHandle handle, NSGenerationCallback fn, void* userData) {
    auto* e = static_cast<NSEngineImpl*>(handle);
    e->callback     = fn;
    e->callbackData = userData;
}

void NS_Initialize(NSEngineHandle handle) {
    auto* e = static_cast<NSEngineImpl*>(handle);
    e->log.clear();
    e->population.clear();
    e->bestFitness = -1e300;
    e->bestGenome.clear();
    for (int i = 0; i < e->cfg.populationSize; ++i) {
        auto g = e->MakeGenome();
        auto f = e->EvalBuiltin(g);
        e->population.emplace_back(g, f);
        if (f > e->bestFitness) { e->bestFitness = f; e->bestGenome = g; }
    }
}

void NS_Run(NSEngineHandle handle) {
    auto* e = static_cast<NSEngineImpl*>(handle);
    e->running = true;
    int totalEvals = e->cfg.populationSize;
    int eliteN = std::max(1, (int)(e->cfg.populationSize * e->cfg.eliteRatio));

    for (int gen = 1; gen <= e->cfg.maxGenerations && e->running; ++gen) {
        auto t0 = std::chrono::high_resolution_clock::now();

        // Sort by fitness descending
        std::sort(e->population.begin(), e->population.end(),
                  [](auto& a, auto& b){ return a.second > b.second; });

        std::vector<std::pair<std::string,double>> newPop;
        newPop.reserve(e->cfg.populationSize);

        // Keep elite
        for (int i = 0; i < eliteN; ++i)
            newPop.push_back(e->population[i]);

        // Tournament selection + crossover + mutation
        std::uniform_int_distribution<int> pick(0, (int)e->population.size()-1);
        std::bernoulli_distribution doCX(e->cfg.crossoverRate);

        while ((int)newPop.size() < e->cfg.populationSize) {
            // tournament
            auto selectOne = [&]() -> const std::string& {
                int best = pick(e->rng);
                for (int t = 1; t < e->cfg.tournamentSize; ++t) {
                    int c = pick(e->rng);
                    if (e->population[c].second > e->population[best].second) best = c;
                }
                return e->population[best].first;
            };
            std::string child;
            if (doCX(e->rng))
                child = e->Crossover(selectOne(), selectOne());
            else
                child = selectOne();
            child = e->Mutate(child);
            double f = e->EvalBuiltin(child);
            totalEvals++;
            newPop.emplace_back(child, f);
            if (f > e->bestFitness) { e->bestFitness = f; e->bestGenome = child; }
        }

        e->population = std::move(newPop);

        if (gen % e->cfg.logInterval == 0) {
            double sumF = 0, sum2 = 0, worstF = e->population[0].second;
            for (auto& [g,f] : e->population) {
                sumF += f;
                if (f < worstF) worstF = f;
            }
            double mean = sumF / e->population.size();
            for (auto& [g,f] : e->population)
                sum2 += (f - mean) * (f - mean);
            double stdDev = std::sqrt(sum2 / e->population.size());

            auto t1 = std::chrono::high_resolution_clock::now();
            double ms = std::chrono::duration<double,std::milli>(t1-t0).count();

            NSGenerationStats stats{};
            stats.generation  = gen;
            stats.bestFitness = e->bestFitness;
            stats.meanFitness = mean;
            stats.stdDev      = stdDev;
            stats.worstFitness = worstF;
            stats.evaluations = totalEvals;
            stats.elapsedMs   = ms;
            std::strncpy(stats.bestGenome, e->bestGenome.c_str(), 511);
            e->log.push_back(stats);

            if (e->callback) e->callback(stats, e->callbackData);
        }

        if (e->cfg.useTargetFitness && e->bestFitness >= e->cfg.targetFitness)
            break;
    }

    e->running = false;
}

void NS_Stop(NSEngineHandle handle) {
    static_cast<NSEngineImpl*>(handle)->running = false;
}

int NS_GetGenerationCount(NSEngineHandle handle) {
    return (int)static_cast<NSEngineImpl*>(handle)->log.size();
}

double NS_GetBestFitness(NSEngineHandle handle) {
    return static_cast<NSEngineImpl*>(handle)->bestFitness;
}

const char* NS_GetBestGenome(NSEngineHandle handle) {
    return static_cast<NSEngineImpl*>(handle)->bestGenome.c_str();
}

NSGenerationStats NS_GetStats(NSEngineHandle handle, int generation) {
    auto* e = static_cast<NSEngineImpl*>(handle);
    for (const auto& s : e->log)
        if (s.generation == generation) return s;
    NSGenerationStats empty{};
    return empty;
}

} // extern "C"
