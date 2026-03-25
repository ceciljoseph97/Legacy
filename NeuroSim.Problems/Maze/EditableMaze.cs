// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.Problems/Maze/EditableMaze.cs
namespace NeuroSim.Problems.Maze;

/// <summary>
/// Mutable maze grid for UI editing.
/// Supports: set start, set goal, toggle walls.
/// Call ToMazeProblem() to get an immutable MazeProblem for the evolver.
/// </summary>
public sealed class EditableMaze
{
    public int Width  { get; }
    public int Height { get; }

    private readonly bool[,] _walls;
    private (int X, int Y) _start;
    private (int X, int Y) _goal;

    public (int X, int Y) Start
    {
        get => _start;
        set
        {
            if (IsInBounds(value.X, value.Y))
            {
                _walls[value.X, value.Y] = false; // ensure start is open
                _start = value;
            }
        }
    }

    public (int X, int Y) Goal
    {
        get => _goal;
        set
        {
            if (IsInBounds(value.X, value.Y))
            {
                _walls[value.X, value.Y] = false; // ensure goal is open
                _goal = value;
            }
        }
    }

    public bool IsWall(int x, int y) => !IsInBounds(x, y) || _walls[x, y];
    public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public EditableMaze(int width, int height)
    {
        Width  = width;
        Height = height;
        _walls = new bool[width, height];
        _start = (1, 1);
        _goal  = (width - 2, height - 2);
    }

    /// <summary>Create from a preset MazeProblem (e.g. after loading "Classic 15×15").</summary>
    public static EditableMaze FromMazeProblem(MazeProblem p)
    {
        var m = new EditableMaze(p.Width, p.Height);
        Array.Copy(p.Walls, m._walls, p.Width * p.Height);
        m._start = p.Start;
        m._goal  = p.Goal;
        return m;
    }

    /// <summary>Toggle wall at (x,y). Start and goal cells are always forced open.</summary>
    public void ToggleWall(int x, int y)
    {
        if (!IsInBounds(x, y)) return;
        if ((x, y) == _start || (x, y) == _goal) return; // don't wall over S/G
        _walls[x, y] = !_walls[x, y];
    }

    /// <summary>Set cell as wall (if not start/goal).</summary>
    public void SetWall(int x, int y, bool wall)
    {
        if (!IsInBounds(x, y)) return;
        if ((x, y) == _start || (x, y) == _goal) return;
        _walls[x, y] = wall;
    }

    /// <summary>Produce immutable MazeProblem for the evolver.</summary>
    public MazeProblem ToMazeProblem()
    {
        var walls = new bool[Width, Height];
        Array.Copy(_walls, walls, Width * Height);
        return new MazeProblem
        {
            Name   = "Custom",
            Width  = Width,
            Height = Height,
            Walls  = walls,
            Start  = _start,
            Goal   = _goal
        };
    }

    /// <summary>Serialise for snapshot: compact string of 0/1 for walls, then "Sx,y Gx,y".</summary>
    public string Serialise()
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                sb.Append(_walls[x, y] ? '1' : '0');
        sb.Append($" S{_start.X},{_start.Y} G{_goal.X},{_goal.Y}");
        return sb.ToString();
    }

    /// <summary>Deserialise from snapshot string.</summary>
    public static EditableMaze Deserialise(string s)
    {
        var parts = s.TrimEnd().Split(' ');
        if (parts.Length < 3) return new EditableMaze(15, 15);

        string grid = parts[0];
        int total = grid.Length;
        int w = (int)Math.Sqrt(total);
        if (w * w != total) w = 15;
        int h = total / w;
        var m = new EditableMaze(w, h);

        for (int i = 0; i < total && i < w * h; i++)
        {
            int x = i % w, y = i / w;
            m._walls[x, y] = grid[i] == '1';
        }

        foreach (var p in parts.Skip(1))
        {
            if (p.StartsWith("S") && p.Length > 1)
            {
                var xy = p[1..].Split(',');
                if (xy.Length == 2 && int.TryParse(xy[0], out int sx) && int.TryParse(xy[1], out int sy))
                    m.Start = (sx, sy);
            }
            else if (p.StartsWith("G") && p.Length > 1)
            {
                var xy = p[1..].Split(',');
                if (xy.Length == 2 && int.TryParse(xy[0], out int gx) && int.TryParse(xy[1], out int gy))
                    m.Goal = (gx, gy);
            }
        }
        return m;
    }
}
