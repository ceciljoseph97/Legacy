#pragma once
#include <memory>
#include <string>

namespace NeuroSim {

enum class GenomeType { Binary, RealValued, Tree, Graph };

class IGenome {
public:
    virtual ~IGenome() = default;
    virtual std::unique_ptr<IGenome> Clone() const = 0;
    virtual void Randomize() = 0;
    virtual std::string Serialize() const = 0;
    virtual GenomeType Type() const = 0;
    virtual int Size() const = 0;
};

} // namespace NeuroSim
