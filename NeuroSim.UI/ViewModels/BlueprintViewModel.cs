// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/ViewModels/BlueprintViewModel.cs
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using NeuroSim.UI.Models.Blueprint;

namespace NeuroSim.UI.ViewModels;

public sealed class BlueprintViewModel : ViewModelBase
{
    private readonly NavigationService _nav;
    private BlueprintNode? _selectedNode;

    public ObservableCollection<BlockCategory> BlockCategories { get; } = new();
    public ObservableCollection<BlueprintNode> Nodes { get; } = new();
    public ObservableCollection<BlueprintConnection> Connections { get; } = new();

    public BlueprintNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            Set(ref _selectedNode, value);
            RefreshConfigEntries();
        }
    }

    public ObservableCollection<BlockConfigEntry> ConfigEntries { get; } = new();

    public RelayCommand GoBackCommand { get; }
    public RelayCommand ClearCanvasCommand { get; }

    public BlueprintViewModel(NavigationService nav)
    {
        _nav = nav;
        GoBackCommand = new RelayCommand(() => _nav.GoBack());
        ClearCanvasCommand = new RelayCommand(ClearCanvas);

        BuildBlockCatalog();
    }

    private void BuildBlockCatalog()
    {
        BlockCategories.Clear();

        BlockCategories.Add(new BlockCategory("Source", new[]
        {
            new BlockDefinition
            {
                Id = "GenomeSource",
                DisplayName = "Genome Source",
                Category = "Source",
                Description = "Create initial population (Permutation, Binary, DirectionSeq)",
                Outputs = new[] { new PortDefinition { Id = "population", DisplayName = "Population", DataType = "Population", IsInput = false } },
                Inputs = new[] { new PortDefinition { Id = "config", DisplayName = "Config", DataType = "Config", IsInput = true } }
            }
        }));

        BlockCategories.Add(new BlockCategory("Operators", new[]
        {
            new BlockDefinition
            {
                Id = "Crossover",
                DisplayName = "Crossover",
                Category = "Operators",
                Description = "SinglePoint, TwoPoint, Uniform, OX, PMX",
                Inputs = new[]
                {
                    new PortDefinition { Id = "parents", DisplayName = "Parents", DataType = "Individual[]", IsInput = true },
                    new PortDefinition { Id = "config", DisplayName = "Config", DataType = "Config", IsInput = true }
                },
                Outputs = new[] { new PortDefinition { Id = "offspring", DisplayName = "Offspring", DataType = "Individual[]", IsInput = false } }
            },
            new BlockDefinition
            {
                Id = "Mutation",
                DisplayName = "Mutation",
                Category = "Operators",
                Description = "FlipBit, Swap, Insert, 2-Opt",
                Inputs = new[]
                {
                    new PortDefinition { Id = "individual", DisplayName = "Individual", DataType = "Individual", IsInput = true },
                    new PortDefinition { Id = "config", DisplayName = "Config", DataType = "Config", IsInput = true }
                },
                Outputs = new[] { new PortDefinition { Id = "mutated", DisplayName = "Mutated", DataType = "Individual", IsInput = false } }
            }
        }));

        BlockCategories.Add(new BlockCategory("Selection", new[]
        {
            new BlockDefinition
            {
                Id = "Selection",
                DisplayName = "Selection",
                Category = "Selection",
                Description = "Tournament, Roulette, Rank",
                Inputs = new[]
                {
                    new PortDefinition { Id = "population", DisplayName = "Population", DataType = "Population", IsInput = true },
                    new PortDefinition { Id = "config", DisplayName = "Config", DataType = "Config", IsInput = true }
                },
                Outputs = new[] { new PortDefinition { Id = "selected", DisplayName = "Selected", DataType = "Individual[]", IsInput = false } }
            }
        }));

        BlockCategories.Add(new BlockCategory("Fitness", new[]
        {
            new BlockDefinition
            {
                Id = "Fitness",
                DisplayName = "Fitness",
                Category = "Fitness",
                Description = "Problem-specific evaluator",
                Inputs = new[]
                {
                    new PortDefinition { Id = "individual", DisplayName = "Individual", DataType = "Individual", IsInput = true },
                    new PortDefinition { Id = "problem", DisplayName = "Problem", DataType = "Problem", IsInput = true }
                },
                Outputs = new[] { new PortDefinition { Id = "score", DisplayName = "Score", DataType = "float", IsInput = false } }
            }
        }));

        BlockCategories.Add(new BlockCategory("Control", new[]
        {
            new BlockDefinition
            {
                Id = "Terminator",
                DisplayName = "Terminator",
                Category = "Control",
                Description = "MaxGens, stagnation, threshold",
                Inputs = new[]
                {
                    new PortDefinition { Id = "gen", DisplayName = "Generation", DataType = "int", IsInput = true },
                    new PortDefinition { Id = "config", DisplayName = "Config", DataType = "Config", IsInput = true }
                },
                Outputs = new[] { new PortDefinition { Id = "done", DisplayName = "Done", DataType = "bool", IsInput = false } }
            }
        }));
    }

    public void AddNodeFromPalette(BlockDefinition def, Point canvasPosition)
    {
        var node = new BlueprintNode
        {
            Definition = def,
            X = canvasPosition.X,
            Y = canvasPosition.Y
        };
        InitDefaultConfig(node);
        Nodes.Add(node);
        SelectedNode = node;
    }

    private static void InitDefaultConfig(BlueprintNode node)
    {
        foreach (var (key, value) in GetDefaultConfig(node.Definition.Id))
            node.Config[key] = value;
    }

    private static IEnumerable<(string, object)> GetDefaultConfig(string blockId) => blockId switch
    {
        "GenomeSource" => [("GenomeType", "Permutation"), ("Size", 16), ("PopulationSize", 80)],
        "Crossover" => [("Method", "OrderCrossover"), ("Rate", 0.85)],
        "Mutation" => [("Method", "Swap"), ("Rate", 0.15)],
        "Selection" => [("Method", "Tournament"), ("TournamentSize", 3)],
        "Fitness" => [("Problem", "TSP")],
        "Terminator" => [("MaxGenerations", 100), ("StagnationThreshold", 20)],
        _ => []
    };

    private void RefreshConfigEntries()
    {
        ConfigEntries.Clear();
        if (_selectedNode is null) return;
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GenomeType"] = "Genome type", ["Size"] = "Size", ["PopulationSize"] = "Population size",
            ["Method"] = "Method", ["Rate"] = "Rate", ["TournamentSize"] = "Tournament size",
            ["Problem"] = "Problem", ["MaxGenerations"] = "Max generations", ["StagnationThreshold"] = "Stagnation threshold"
        };
        foreach (var kv in _selectedNode.Config)
        {
            var entry = new BlockConfigEntry
            {
                Key = kv.Key,
                DisplayName = names.GetValueOrDefault(kv.Key, kv.Key),
                Value = kv.Value
            };
            entry.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BlockConfigEntry.Value) && _selectedNode is not null)
                    _selectedNode.Config[entry.Key] = entry.Value;
            };
            ConfigEntries.Add(entry);
        }
    }

    public void AddConnection(BlueprintNode source, string sourcePortId, BlueprintNode target, string targetPortId)
    {
        if (source == target) return;
        if (Connections.Any(c => c.SourceNode == source && c.SourcePortId == sourcePortId && c.TargetNode == target && c.TargetPortId == targetPortId))
            return;

        Connections.Add(new BlueprintConnection
        {
            SourceNode = source,
            SourcePortId = sourcePortId,
            TargetNode = target,
            TargetPortId = targetPortId
        });
    }

    public void RemoveNode(BlueprintNode node)
    {
        if (SelectedNode == node) SelectedNode = null;
        foreach (var c in Connections.Where(c => c.SourceNode == node || c.TargetNode == node).ToList())
            Connections.Remove(c);
        Nodes.Remove(node);
    }

    public void RemoveConnection(BlueprintConnection conn) => Connections.Remove(conn);

    private void ClearCanvas()
    {
        Nodes.Clear();
        Connections.Clear();
    }
}

public sealed class BlockCategory
{
    public string Name { get; }
    public IReadOnlyList<BlockDefinition> Blocks { get; }

    public BlockCategory(string name, BlockDefinition[] blocks)
    {
        Name = name;
        Blocks = blocks;
    }
}
