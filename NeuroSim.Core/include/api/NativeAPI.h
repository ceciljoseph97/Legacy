#pragma once
#include "../export.h"
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

// ── Handle types ─────────────────────────────────────────────────────────────
typedef void* NSEngineHandle;

// ── Enums (must match NeuroSim::* enums exactly) ─────────────────────────────
typedef enum { NS_BINARY = 0, NS_REAL_VALUED = 1, NS_TREE = 2, NS_GRAPH = 3 } NSGenomeType;
typedef enum { NS_SEL_TOURNAMENT = 0, NS_SEL_ROULETTE = 1, NS_SEL_RANK = 2 } NSSelectionType;
typedef enum { NS_CX_SINGLE = 0, NS_CX_TWO_POINT = 1, NS_CX_UNIFORM = 2 } NSBinaryCrossoverType;
typedef enum { NS_CX_BLEND = 0, NS_CX_SBX = 1, NS_CX_ARITHMETIC = 2 } NSRealCrossoverType;
typedef enum { NS_MUT_FLIP = 0 } NSBinaryMutationType;
typedef enum { NS_MUT_GAUSSIAN = 0, NS_MUT_POLYNOMIAL = 1 } NSRealMutationType;

// ── Config POD ────────────────────────────────────────────────────────────────
typedef struct {
    // population
    int populationSize;
    int maxGenerations;
    double eliteRatio;
    // genome
    NSGenomeType genomeType;
    int genomeLength;
    double realMin;
    double realMax;
    int treeNumVars;
    int treeMaxDepth;
    int graphNodes;
    int graphDirected;
    // operators
    NSSelectionType selectionType;
    int tournamentSize;
    NSBinaryCrossoverType binaryCrossover;
    NSRealCrossoverType realCrossover;
    NSBinaryMutationType binaryMutation;
    NSRealMutationType realMutation;
    // rates
    double crossoverRate;
    double mutationRate;
    double gaussianSigma;
    // fitness (built-in name, null-terminated, up to 64 chars)
    char fitnessName[64];
    // termination
    int useTargetFitness;
    double targetFitness;
    unsigned int randomSeed;
    int logInterval;
} NSEvolutionConfig;

// ── Stats POD ─────────────────────────────────────────────────────────────────
typedef struct {
    int generation;
    double bestFitness;
    double meanFitness;
    double stdDev;
    double worstFitness;
    int evaluations;
    double elapsedMs;
    char bestGenome[512];
} NSGenerationStats;

// ── Callbacks ─────────────────────────────────────────────────────────────────
typedef void (*NSGenerationCallback)(NSGenerationStats stats, void* userData);

// ── Engine lifecycle ──────────────────────────────────────────────────────────
NEUROSIM_API NSEvolutionConfig NS_DefaultConfig(NSGenomeType genomeType);
NEUROSIM_API NSEngineHandle    NS_CreateEngine(NSEvolutionConfig config);
NEUROSIM_API void              NS_DestroyEngine(NSEngineHandle handle);

// ── Run control ───────────────────────────────────────────────────────────────
NEUROSIM_API void NS_SetGenerationCallback(NSEngineHandle handle,
                                            NSGenerationCallback fn,
                                            void* userData);
NEUROSIM_API void NS_Initialize(NSEngineHandle handle);
NEUROSIM_API void NS_Run(NSEngineHandle handle);   // blocking
NEUROSIM_API void NS_Stop(NSEngineHandle handle);

// ── Results ───────────────────────────────────────────────────────────────────
NEUROSIM_API int               NS_GetGenerationCount(NSEngineHandle handle);
NEUROSIM_API double            NS_GetBestFitness(NSEngineHandle handle);
NEUROSIM_API const char*       NS_GetBestGenome(NSEngineHandle handle);
NEUROSIM_API NSGenerationStats NS_GetStats(NSEngineHandle handle, int generation);

#ifdef __cplusplus
}
#endif
