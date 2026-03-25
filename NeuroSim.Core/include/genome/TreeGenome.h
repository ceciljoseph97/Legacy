#pragma once
#include "IGenome.h"
#include "../../export.h"
#include <vector>
#include <string>
#include <memory>
#include <random>

namespace NeuroSim {

enum class NodeKind { Function, Variable, Constant };

struct NEUROSIM_API TreeNode {
    NodeKind kind;
    std::string label;
    double constValue = 0.0;
    int varIndex = 0;
    int arity = 0;
    std::vector<std::unique_ptr<TreeNode>> children;

    TreeNode() = default;
    TreeNode(const TreeNode&) = delete;
    TreeNode& operator=(const TreeNode&) = delete;

    std::unique_ptr<TreeNode> Clone() const;
    double Evaluate(const std::vector<double>& vars) const;
    std::string ToString() const;
    int Size() const;
    int Depth() const;
    std::vector<TreeNode*> AllNodes();
};

class NEUROSIM_API TreeGenome : public IGenome {
public:
    explicit TreeGenome(int numVars = 1, int maxDepth = 5);

    std::unique_ptr<IGenome> Clone() const override;
    void Randomize() override;
    std::string Serialize() const override;
    GenomeType Type() const override { return GenomeType::Tree; }
    int Size() const override { return root_ ? root_->Size() : 0; }

    TreeNode* Root() { return root_.get(); }
    const TreeNode* Root() const { return root_.get(); }
    double Evaluate(const std::vector<double>& vars) const;
    void SetRoot(std::unique_ptr<TreeNode> node) { root_ = std::move(node); }

    static void SwapSubtree(TreeGenome& genome, std::unique_ptr<TreeNode> replacement,
                             std::mt19937& rng);

private:
    std::unique_ptr<TreeNode> root_;
    int numVars_;
    int maxDepth_;

    std::unique_ptr<TreeNode> Build(std::mt19937& rng, int depth, bool full);
    std::unique_ptr<TreeNode> MakeLeaf(std::mt19937& rng);

    static std::mt19937& Rng();
};

} // namespace NeuroSim
