using System.Diagnostics;

Assert.Throw = true;
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 1;
Test.Run((notes, part, expected) => {
    var map = new Map();
    map.Read(notes);
    map.Reconstruct();
    int cells = part switch {
        1 => map.Land,
        2 => map.Sand,
        _ => map.Trees
    };
    map.Log();
    Assert.ActualEqualsExpected(cells, expected);
});

class Map {
    const int VALID_LAND_NEIGHBORS = 5;
    const int VALID_TREE_NEIGHBORS = 4;
    const int TREE_RADIUS = 2;
    const int TREE_GROWTH_TIMES = 2;

    readonly Dictionary<Point, char> grid = [];
    int width, height;
    public int Land => grid.Values.Count(it => it is Cells.LAND or Cells.SAND or Cells.TREE);
    public int Sand => grid.Values.Count(it => it is Cells.SAND);
    public int Trees => grid.Values.Count(it => it is Cells.TREE);

    public void Read(string notes) {
        string[] rows = [.. notes.Split(Environment.NewLine)];
        width = rows[0].Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                grid[new Point(x, y)] = rows[y][x];
            }
        }
    }

    public void Reconstruct() {
        MarkLand();
        foreach ((var point, char _) in grid.Where(it => it.Value != Cells.LAND)) {
            grid[point] = Cells.WATER;
        }
        MarkSand();
        MarkTrees();
    }

    void MarkLand() {
        var queue = new Queue<Point>();
        var startLand = new Point(width / 2, height / 2);
        grid[startLand] = Cells.LAND;
        queue.Enqueue(startLand);
        while (queue.TryDequeue(out var current)) {
            foreach (var direction in Point.Directions8) {
                var next = current + direction;
                int validCellsAround = Point.Directions8
                        .Select(it => next + it)
                        .Count(it => grid.TryGetValue(it, out char cell) && cell is Cells.DARK or Cells.LAND);
                bool isLand = validCellsAround >= VALID_LAND_NEIGHBORS;
                if (!isLand || grid[next] == Cells.LAND) {
                    continue;
                }
                queue.Enqueue(next);
                grid[next] = Cells.LAND;
            }
        }
    }

    void MarkSand() {
        MarkBeaches();
        var queue = new Queue<Point>();
        foreach ((var point, char _) in grid.Where(it => it.Value == Cells.SAND)) {
            queue.Enqueue(point);
        }
        while (queue.TryDequeue(out var current)) {
            foreach (var direction in Point.Directions8) {
                var next = current + direction;
                int validCellsAround = Point.Directions8
                        .Select(it => next + it)
                        .Count(it => grid.TryGetValue(it, out char cell) && cell == Cells.SAND);
                bool isSand = validCellsAround >= VALID_LAND_NEIGHBORS;
                if (!isSand || grid[next] == Cells.SAND) {
                    continue;
                }
                queue.Enqueue(next);
                grid[next] = Cells.SAND;
            }
        }
    }

    void MarkBeaches() {
        foreach (var point in grid.Keys.Where(it => grid[it] is Cells.LAND)) {
            foreach (var direction in Point.Directions8) {
                var neighbor = point + direction;
                bool isEdge = neighbor.X == 0 || neighbor.X == width - 1 || neighbor.Y == 0 || neighbor.Y == height - 1;
                if (!isEdge && (!grid.TryGetValue(neighbor, out char cell) || cell is not Cells.WATER)) {
                    continue;
                }
                grid[point] = Cells.SAND;
                break;
            }
        }
    }

    void MarkTrees() {
        for (int y = TREE_RADIUS; y < height - TREE_RADIUS; y++) {
            for (int x = TREE_RADIUS; x < width - TREE_RADIUS; x++) {
                var point = new Point(x, y);
                if (IsFirstTree(point)) {
                    grid[point] = Cells.TREE;
                }
            }
        }
        for (var _ = 0; _ < TREE_GROWTH_TIMES; _++) {
            GrowTrees();
        }
    }

    bool IsFirstTree(Point point) {
        var min = point - TREE_RADIUS;
        var max = point + TREE_RADIUS;
        for (int y = min.Y; y <= max.Y; y++) {
            for (int x = min.X; x <= max.X; x++) {
                var neighbor = new Point(x, y);
                if (neighbor == point) {
                    continue;
                }
                bool isCorner = neighbor.Y == min.Y && (neighbor.X == min.X || neighbor.X == max.X)
                        || neighbor.Y == max.Y && (neighbor.X == min.X || neighbor.X == max.X);
                if (isCorner) {
                    continue;
                }
                if (grid[neighbor] is not (Cells.LAND or Cells.TREE)) {
                    return false;
                }
            }
        }
        return true;
    }

    void GrowTrees() {
        var queue = new Queue<Point>();
        foreach ((var point, char _) in grid.Where(it => it.Value == Cells.TREE)) {
            queue.Enqueue(point);
        }
        HashSet<Point> nextTrees = [];
        while (queue.TryDequeue(out var current)) {
            foreach (var direction in Point.Directions8) {
                var next = current + direction;
                if (!IsNextTree(next)) {
                    continue;
                }
                nextTrees.Add(next);
            }
        }
        foreach (var tree in nextTrees) {
            grid[tree] = Cells.TREE;
        }
    }

    bool IsNextTree(Point point) {
        var validCellsAround = 0;
        foreach (var direction in Point.Directions8) {
            var neighbor = point + direction;
            switch (grid[neighbor]) {
                case Cells.TREE:
                    validCellsAround++;
                    break;
                case Cells.SAND:
                    return false;
            }
        }
        return validCellsAround >= VALID_TREE_NEIGHBORS;
    }

    public void Log() {
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = grid[new Point(x, y)];
                Console.ForegroundColor = cell switch {
                    Cells.WATER => ConsoleColor.Blue,
                    Cells.LAND => ConsoleColor.Green,
                    Cells.SAND => ConsoleColor.Yellow,
                    Cells.TREE => ConsoleColor.DarkCyan,
                    _ => Console.ForegroundColor
                };
                Console.Write(cell);
                Console.ResetColor();
            }
            Console.WriteLine();
        }
        // Test.Step();
    }
}

static class Cells {
    public const char DARK = '1';
    public const char LAND = '#';
    public const char WATER = '.';
    public const char SAND = '=';
    public const char TREE = '@';
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    static readonly Point RIGHT = new(1, 0);

    public static Point[] Directions8 { get; } = [
        RIGHT, LEFT, UP, DOWN,
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
    ];

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator +(Point a, int value) => new(a.X + value, a.Y + value);
    public static Point operator -(Point a, int value) => new(a.X - value, a.Y - value);
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    static readonly TextReader consoleIn = Console.In;
    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }

    public static void Run(Action<string, int, string> action) {
        File.WriteAllText(LOG_FILE, null);
        string cases = ReadCases();
        Console.SetIn(new StringReader(cases));
        long totalTime = 0L;
        while (Console.In.Peek() != -1) {
            var stopwatch = Stopwatch.StartNew();
            string name = Console.ReadLine()!;
            Log(name, console: true, color: ConsoleColor.Cyan);
            int part = int.Parse(Console.ReadLine()!);
            string notes = ReadUntilNewLine();
            string expected = ReadUntilNewLine();
            try {
                action(notes, part, expected);
            }
            finally {
                long time = stopwatch.ElapsedMilliseconds;
                totalTime += time;
                Log($"Test took {time}ms", console: true);
            }
            Log(console: true);
        }
        Log($"All tests took {totalTime}ms", console: true);
    }

    static string ReadCases() {
        string cases = File.ReadAllText(CASE_FILE);
        string[] lines = cases.ReplaceLineEndings(Environment.NewLine).Split(Environment.NewLine);
        if (From != null) {
            int start = Array.IndexOf(lines, From);
            if (start < 0) {
                throw new InvalidOperationException($"Line '{From}' not found");
            }
            lines = lines[start..];
        }
        if (Until != null) {
            int end = Array.IndexOf(lines, Until);
            if (end < 0) {
                throw new InvalidOperationException($"Line '{Until}' not found");
            }
            lines = lines[..end];
        }
        cases = string.Join(Environment.NewLine, lines);
        return cases;
    }

    static string ReadUntilNewLine() {
        List<string> lines = [];
        for (string line; !string.IsNullOrEmpty(line = Console.ReadLine()!);) {
            lines.Add(line);
        }
        return string.Join(Environment.NewLine, lines);
    }

    public static void Step() {
        if (!StepEnabled) {
            return;
        }
        var current = Console.In;
        Console.SetIn(consoleIn);
        Console.ReadLine();
        Console.SetIn(current);
    }

    public static void Log(object? line = null, bool console = false, ConsoleColor? color = null) {
        File.AppendAllText(LOG_FILE, $"{line}{Environment.NewLine}");
        if (!console) {
            return;
        }
        if (color != null) {
            Console.ForegroundColor = color.Value;
        }
        Console.Error.WriteLine(line);
        Console.ResetColor();
    }
}

static class Assert {
    public static bool Throw { get; set; }

    public static void ActualEqualsExpected(object actual, object expected) {
        bool equals = Equals(actual.ToString(), expected);
        Console.WriteLine(equals ? $"✅ {actual}" : $"❌ found {actual}, but expected {expected}");
        if (!equals && Throw) {
            throw new Exception();
        }
    }
}
