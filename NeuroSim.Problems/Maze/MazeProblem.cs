// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Maze/MazeProblem.cs
namespace NeuroSim.Problems.Maze;

/// <summary>
/// Rectangular grid maze.
/// Walls[x, y] == true  →  impassable cell.
/// Origin (0,0) is top-left; X grows right, Y grows down.
/// </summary>
public sealed class MazeProblem
{
    public string   Name   { get; init; } = "Maze";
    public int      Width  { get; init; }
    public int      Height { get; init; }
    public bool[,]  Walls  { get; init; } = null!;
    public (int X, int Y) Start { get; init; }
    public (int X, int Y) Goal  { get; init; }

    public bool IsWall(int x, int y) =>
        x < 0 || y < 0 || x >= Width || y >= Height || Walls[x, y];
}

/// <summary>Built-in maze presets generated with a seeded recursive backtracker.</summary>
public static class MazeDatasets
{
    public static IReadOnlyList<string> Names { get; } =
        ["Classic 15×15", "Winding 21×21", "Labyrinth 25×25"];

    public static MazeProblem Get(string name) => name switch
    {
        "Classic 15×15"  => Generate("Classic 15×15",  15, 15, seed: 1),
        "Winding 21×21"  => Generate("Winding 21×21",  21, 21, seed: 7),
        "Labyrinth 25×25"=> Generate("Labyrinth 25×25",25, 25, seed: 3),
        _                => Generate(name,              15, 15, seed: 1)
    };

    // ── Recursive-backtracker maze generator ─────────────────────────────────
    // Guarantees a perfect maze: one unique path between every pair of cells.
    // Width and Height must be odd numbers (boundary walls + cell/passage layout).

    public static MazeProblem Generate(string name, int w, int h, int seed = 42)
    {
        // Enforce odd dimensions so the grid divides cleanly into cells and walls
        if (w % 2 == 0) w++;
        if (h % 2 == 0) h++;

        var rng   = new Random(seed);
        var walls = new bool[w, h];

        // Start with all walls
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                walls[x, y] = true;

        // Carve passages using iterative DFS
        var stack = new Stack<(int, int)>();
        walls[1, 1] = false;
        stack.Push((1, 1));

        // Directions: step 2 cells (over the wall between cells)
        int[] dx = { 0,  2,  0, -2 };
        int[] dy = { -2, 0,  2,  0 };

        while (stack.Count > 0)
        {
            var (cx, cy) = stack.Peek();

            // Collect unvisited neighbours
            var dirs = new List<int>();
            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dx[d], ny = cy + dy[d];
                if (nx > 0 && nx < w - 1 && ny > 0 && ny < h - 1 && walls[nx, ny])
                    dirs.Add(d);
            }

            if (dirs.Count == 0)
            {
                stack.Pop();
            }
            else
            {
                int chosen = dirs[rng.Next(dirs.Count)];
                int nx = cx + dx[chosen], ny = cy + dy[chosen];
                // Remove the wall between current cell and chosen neighbour
                walls[cx + dx[chosen] / 2, cy + dy[chosen] / 2] = false;
                walls[nx, ny] = false;
                stack.Push((nx, ny));
            }
        }

        return new MazeProblem
        {
            Name   = name,
            Width  = w,
            Height = h,
            Walls  = walls,
            Start  = (1, 1),
            Goal   = (w - 2, h - 2)
        };
    }
}
