#pragma once
#include "IGenome.h"
#include "../../export.h"
#include <vector>

namespace NeuroSim {

class NEUROSIM_API GraphGenome : public IGenome {
public:
    GraphGenome(int nodeCount, bool directed = false);
    GraphGenome(const GraphGenome& other);

    std::unique_ptr<IGenome> Clone() const override;
    void Randomize() override;
    std::string Serialize() const override;
    GenomeType Type() const override { return GenomeType::Graph; }
    int Size() const override { return EdgeCount(); }

    int NodeCount() const { return nodeCount_; }
    bool Directed() const { return directed_; }

    bool HasEdge(int i, int j) const;
    double EdgeWeight(int i, int j) const;
    void SetEdge(int i, int j, bool present, double weight = 1.0);
    int EdgeCount() const;

    std::vector<bool> FlattenAdjacency() const;
    void UnflattenAdjacency(const std::vector<bool>& flat);

    std::vector<int> Neighbors(int node) const;
    bool IsConnected() const;

private:
    int nodeCount_;
    bool directed_;
    // Stored as flat row-major arrays
    std::vector<bool> adj_;
    std::vector<double> weights_;

    int Idx(int i, int j) const { return i * nodeCount_ + j; }
    static std::mt19937& Rng();
};

} // namespace NeuroSim
