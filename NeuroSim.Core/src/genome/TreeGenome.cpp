#include "../../include/genome/TreeGenome.h"
#include <algorithm>
#include <cmath>
#include <sstream>

namespace NeuroSim {

// ── Statics ───────────────────────────────────────────────────────────────────
static const char* kFuncs[] = {"+","-","*","/","sin","cos","sqrt","neg"};
static const int kArity[]   = { 2,  2,  2,  2,   1,   1,    1,   1 };
static constexpr int kNumFuncs = 8;

std::mt19937& TreeGenome::Rng() {
    thread_local static std::mt19937 rng{ std::random_device{}() };
    return rng;
}

// ── TreeNode ──────────────────────────────────────────────────────────────────
std::unique_ptr<TreeNode> TreeNode::Clone() const {
    auto n = std::make_unique<TreeNode>();
    n->kind       = kind;
    n->label      = label;
    n->constValue = constValue;
    n->varIndex   = varIndex;
    n->arity      = arity;
    for (const auto& c : children) n->children.push_back(c->Clone());
    return n;
}

double TreeNode::Evaluate(const std::vector<double>& vars) const {
    if (kind == NodeKind::Constant) return constValue;
    if (kind == NodeKind::Variable) return vars.empty() ? 0.0 : vars[varIndex % vars.size()];

    auto c = [&](int i){ return children[i]->Evaluate(vars); };
    const std::string& lbl = label;
    if (lbl == "+")    return c(0) + c(1);
    if (lbl == "-")    return c(0) - c(1);
    if (lbl == "*")    return c(0) * c(1);
    if (lbl == "/")    { double d = c(1); return std::abs(d) < 1e-9 ? 1.0 : c(0)/d; }
    if (lbl == "sin")  return std::sin(c(0));
    if (lbl == "cos")  return std::cos(c(0));
    if (lbl == "sqrt") return std::sqrt(std::abs(c(0)));
    if (lbl == "neg")  return -c(0);
    if (lbl == "exp")  return std::exp(std::clamp(c(0), -10.0, 10.0));
    if (lbl == "log")  { double v = c(0); return v <= 0 ? 0.0 : std::log(v); }
    return 0.0;
}

std::string TreeNode::ToString() const {
    if (kind == NodeKind::Constant) {
        std::ostringstream oss; oss << constValue; return oss.str();
    }
    if (kind == NodeKind::Variable) return "x" + std::to_string(varIndex);
    if (arity == 1) return label + "(" + children[0]->ToString() + ")";
    return "(" + children[0]->ToString() + " " + label + " " + children[1]->ToString() + ")";
}

int TreeNode::Size() const {
    int s = 1;
    for (const auto& c : children) s += c->Size();
    return s;
}

int TreeNode::Depth() const {
    if (children.empty()) return 0;
    int d = 0;
    for (const auto& c : children) d = std::max(d, c->Depth());
    return d + 1;
}

std::vector<TreeNode*> TreeNode::AllNodes() {
    std::vector<TreeNode*> out{ this };
    for (auto& c : children) {
        auto sub = c->AllNodes();
        out.insert(out.end(), sub.begin(), sub.end());
    }
    return out;
}

// ── TreeGenome ────────────────────────────────────────────────────────────────
TreeGenome::TreeGenome(int numVars, int maxDepth)
    : numVars_(numVars), maxDepth_(maxDepth) {
    root_ = Build(Rng(), 0, false);
}

std::unique_ptr<IGenome> TreeGenome::Clone() const {
    auto copy = std::make_unique<TreeGenome>(numVars_, maxDepth_);
    copy->root_ = root_->Clone();
    return copy;
}

void TreeGenome::Randomize() { root_ = Build(Rng(), 0, false); }

std::string TreeGenome::Serialize() const { return root_ ? root_->ToString() : ""; }

double TreeGenome::Evaluate(const std::vector<double>& vars) const {
    return root_ ? root_->Evaluate(vars) : 0.0;
}

std::unique_ptr<TreeNode> TreeGenome::Build(std::mt19937& rng, int depth, bool full) {
    std::uniform_real_distribution<> prob(0,1);
    bool leaf = depth >= maxDepth_ || (!full && prob(rng) < 0.3);

    if (leaf) return MakeLeaf(rng);

    int idx = std::uniform_int_distribution<int>(0, kNumFuncs-1)(rng);
    auto node = std::make_unique<TreeNode>();
    node->kind  = NodeKind::Function;
    node->label = kFuncs[idx];
    node->arity = kArity[idx];
    for (int i = 0; i < kArity[idx]; ++i)
        node->children.push_back(Build(rng, depth+1, full));
    return node;
}

std::unique_ptr<TreeNode> TreeGenome::MakeLeaf(std::mt19937& rng) {
    auto node = std::make_unique<TreeNode>();
    if (numVars_ > 0 && std::bernoulli_distribution(0.7)(rng)) {
        node->kind     = NodeKind::Variable;
        node->varIndex = std::uniform_int_distribution<int>(0, numVars_-1)(rng);
        node->label    = "x" + std::to_string(node->varIndex);
    } else {
        node->kind       = NodeKind::Constant;
        node->constValue = std::uniform_real_distribution<>(-5, 5)(rng);
    }
    return node;
}

void TreeGenome::SwapSubtree(TreeGenome& genome, std::unique_ptr<TreeNode> replacement,
                              std::mt19937& rng) {
    auto nodes = genome.root_->AllNodes();
    if (nodes.size() <= 1) { genome.root_ = std::move(replacement); return; }

    // Pick a random non-root node; replace in parent
    int target_idx = std::uniform_int_distribution<int>(1, (int)nodes.size()-1)(rng);
    TreeNode* target = nodes[target_idx];

    // Walk tree looking for parent
    std::function<bool(TreeNode*)> replace = [&](TreeNode* cur) -> bool {
        for (auto& child : cur->children) {
            if (child.get() == target) { child = std::move(replacement); return true; }
            if (replace(child.get())) return true;
        }
        return false;
    };
    replace(genome.root_.get());
}

} // namespace NeuroSim
