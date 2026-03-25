#include "../../include/genome/GraphGenome.h"
#include <sstream>
#include <queue>
#include <unordered_set>

namespace NeuroSim {

std::mt19937& GraphGenome::Rng() {
    thread_local static std::mt19937 rng{ std::random_device{}() };
    return rng;
}

GraphGenome::GraphGenome(int nodeCount, bool directed)
    : nodeCount_(nodeCount), directed_(directed),
      adj_(nodeCount * nodeCount, false),
      weights_(nodeCount * nodeCount, 0.0) {}

GraphGenome::GraphGenome(const GraphGenome& o)
    : nodeCount_(o.nodeCount_), directed_(o.directed_),
      adj_(o.adj_), weights_(o.weights_) {}

std::unique_ptr<IGenome> GraphGenome::Clone() const {
    return std::make_unique<GraphGenome>(*this);
}

bool GraphGenome::HasEdge(int i, int j) const { return adj_[Idx(i,j)]; }
double GraphGenome::EdgeWeight(int i, int j) const { return weights_[Idx(i,j)]; }

void GraphGenome::SetEdge(int i, int j, bool present, double weight) {
    adj_[Idx(i,j)] = present;
    weights_[Idx(i,j)] = weight;
    if (!directed_) {
        adj_[Idx(j,i)] = present;
        weights_[Idx(j,i)] = weight;
    }
}

int GraphGenome::EdgeCount() const {
    int count = 0;
    for (int i = 0; i < nodeCount_; ++i)
        for (int j = (directed_ ? 0 : i+1); j < nodeCount_; ++j)
            if (i != j && adj_[Idx(i,j)]) ++count;
    return count;
}

void GraphGenome::Randomize() {
    std::uniform_real_distribution<> wDist(0, 1);
    std::bernoulli_distribution eDist(0.3);
    for (int i = 0; i < nodeCount_; ++i)
        for (int j = (directed_ ? 0 : i+1); j < nodeCount_; ++j)
            if (i != j) SetEdge(i, j, eDist(Rng()), wDist(Rng()));
}

std::string GraphGenome::Serialize() const {
    std::ostringstream oss;
    oss << "Graph(N=" << nodeCount_ << ", E=" << EdgeCount() << "): ";
    for (int i = 0; i < nodeCount_; ++i)
        for (int j = (directed_ ? 0 : i+1); j < nodeCount_; ++j)
            if (i != j && adj_[Idx(i,j)])
                oss << i << "->" << j << "(" << weights_[Idx(i,j)] << ") ";
    return oss.str();
}

std::vector<bool> GraphGenome::FlattenAdjacency() const {
    std::vector<bool> flat;
    for (int i = 0; i < nodeCount_; ++i)
        for (int j = (directed_ ? 0 : i+1); j < nodeCount_; ++j)
            if (i != j) flat.push_back(adj_[Idx(i,j)]);
    return flat;
}

void GraphGenome::UnflattenAdjacency(const std::vector<bool>& flat) {
    size_t k = 0;
    for (int i = 0; i < nodeCount_ && k < flat.size(); ++i)
        for (int j = (directed_ ? 0 : i+1); j < nodeCount_ && k < flat.size(); ++j)
            if (i != j) SetEdge(i, j, flat[k++], weights_[Idx(i,j)]);
}

std::vector<int> GraphGenome::Neighbors(int node) const {
    std::vector<int> n;
    for (int j = 0; j < nodeCount_; ++j)
        if (j != node && adj_[Idx(node,j)]) n.push_back(j);
    return n;
}

bool GraphGenome::IsConnected() const {
    if (nodeCount_ == 0) return true;
    std::unordered_set<int> visited;
    std::queue<int> q;
    q.push(0); visited.insert(0);
    while (!q.empty()) {
        int cur = q.front(); q.pop();
        for (int nb : Neighbors(cur))
            if (visited.insert(nb).second) q.push(nb);
    }
    return (int)visited.size() == nodeCount_;
}

} // namespace NeuroSim
