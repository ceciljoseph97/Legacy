#pragma once
#include "IGenome.h"
#include "../../export.h"
#include <vector>
#include <random>

namespace NeuroSim {

class NEUROSIM_API RealValuedGenome : public IGenome {
public:
    RealValuedGenome(int length, double minVal = -5.0, double maxVal = 5.0);
    RealValuedGenome(std::vector<double> genes, double minVal, double maxVal);

    std::unique_ptr<IGenome> Clone() const override;
    void Randomize() override;
    std::string Serialize() const override;
    GenomeType Type() const override { return GenomeType::RealValued; }
    int Size() const override { return static_cast<int>(genes_.size()); }

    double Get(int i) const { return genes_[i]; }
    void Set(int i, double v);
    std::vector<double>& Genes() { return genes_; }
    const std::vector<double>& Genes() const { return genes_; }
    double MinVal() const { return minVal_; }
    double MaxVal() const { return maxVal_; }

private:
    std::vector<double> genes_;
    double minVal_, maxVal_;
    static std::mt19937& Rng();
};

} // namespace NeuroSim
