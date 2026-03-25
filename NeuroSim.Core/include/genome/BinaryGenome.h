#pragma once
#include "IGenome.h"
#include "../../export.h"
#include <vector>
#include <random>

namespace NeuroSim {

class NEUROSIM_API BinaryGenome : public IGenome {
public:
    explicit BinaryGenome(int length);
    BinaryGenome(std::vector<bool> genes);

    std::unique_ptr<IGenome> Clone() const override;
    void Randomize() override;
    std::string Serialize() const override;
    GenomeType Type() const override { return GenomeType::Binary; }
    int Size() const override { return static_cast<int>(genes_.size()); }

    bool Get(int i) const { return genes_[i]; }
    void Set(int i, bool v) { genes_[i] = v; }
    std::vector<bool>& Genes() { return genes_; }
    const std::vector<bool>& Genes() const { return genes_; }
    int CountOnes() const;

private:
    std::vector<bool> genes_;
    static std::mt19937& Rng();
};

} // namespace NeuroSim
