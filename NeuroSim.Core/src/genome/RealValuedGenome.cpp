#include "../../include/genome/RealValuedGenome.h"
#include <algorithm>
#include <sstream>
#include <iomanip>

namespace NeuroSim {

std::mt19937& RealValuedGenome::Rng() {
    thread_local static std::mt19937 rng{ std::random_device{}() };
    return rng;
}

RealValuedGenome::RealValuedGenome(int length, double minVal, double maxVal)
    : genes_(length, 0.0), minVal_(minVal), maxVal_(maxVal) {}

RealValuedGenome::RealValuedGenome(std::vector<double> genes, double minVal, double maxVal)
    : genes_(std::move(genes)), minVal_(minVal), maxVal_(maxVal) {}

std::unique_ptr<IGenome> RealValuedGenome::Clone() const {
    return std::make_unique<RealValuedGenome>(genes_, minVal_, maxVal_);
}

void RealValuedGenome::Set(int i, double v) {
    genes_[i] = std::clamp(v, minVal_, maxVal_);
}

void RealValuedGenome::Randomize() {
    std::uniform_real_distribution<double> dist(minVal_, maxVal_);
    for (auto& g : genes_) g = dist(Rng());
}

std::string RealValuedGenome::Serialize() const {
    std::ostringstream oss;
    oss << std::fixed << std::setprecision(4);
    oss << "[";
    for (size_t i = 0; i < genes_.size(); ++i) {
        if (i) oss << ", ";
        oss << genes_[i];
    }
    oss << "]";
    return oss.str();
}

} // namespace NeuroSim
