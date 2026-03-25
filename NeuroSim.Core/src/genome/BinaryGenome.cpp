#include "../../include/genome/BinaryGenome.h"
#include <sstream>
#include <numeric>

namespace NeuroSim {

std::mt19937& BinaryGenome::Rng() {
    thread_local static std::mt19937 rng{ std::random_device{}() };
    return rng;
}

BinaryGenome::BinaryGenome(int length) : genes_(length, false) {}
BinaryGenome::BinaryGenome(std::vector<bool> genes) : genes_(std::move(genes)) {}

std::unique_ptr<IGenome> BinaryGenome::Clone() const {
    return std::make_unique<BinaryGenome>(genes_);
}

void BinaryGenome::Randomize() {
    std::bernoulli_distribution dist(0.5);
    for (auto&& g : genes_) g = dist(Rng());
}

std::string BinaryGenome::Serialize() const {
    std::string s;
    s.reserve(genes_.size());
    for (bool b : genes_) s += (b ? '1' : '0');
    return s;
}

int BinaryGenome::CountOnes() const {
    int n = 0;
    for (bool b : genes_) if (b) ++n;
    return n;
}

} // namespace NeuroSim
