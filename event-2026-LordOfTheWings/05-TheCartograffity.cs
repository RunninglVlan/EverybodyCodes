using System.Diagnostics;

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 1;
Test.Run((notes, part, expected) => {
    var map = new Map();
    map.Read(notes);
    map.Reconstruct();
    Assert.ActualEqualsExpected(map.Land, expected);
});

class Map {
    const char DARK = '1', LAND = '#';
    const int VALID_NEIGHBORS = 5;

    readonly Dictionary<Point, char> grid = [];
    int width, height;
    public int Land => grid.Values.Count(it => it == LAND);

    public void Read(string notes) {
        string[] rows = [.. notes.Split(Environment.NewLine)];
        width = rows[0].Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                grid[new Point(x, y)] = rows[y][x];
            }
        }
        var land = new Point(width / 2, height / 2);
        grid[land] = LAND;
    }

    public void Reconstruct() {
        MarkLand();
        // Log();
    }

    void MarkLand() {
        var queue = new Queue<Point>();
        var start = grid.First(it => it.Value == LAND).Key;
        queue.Enqueue(start);
        grid[start] = LAND;
        while (queue.TryDequeue(out var current)) {
            foreach (var direction in Point.Directions8) {
                var next = current + direction;
                int validCellsAround = Point.Directions8
                        .Select(it => next + it)
                        .Count(it => grid.TryGetValue(it, out char cell) && cell is DARK or LAND);
                bool isLand = validCellsAround >= VALID_NEIGHBORS;
                if (!isLand || grid[next] == Cells.LAND) {
                    continue;
                }
                queue.Enqueue(next);
                grid[next] = LAND;
            }
        }
    }

    void Log() {
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = grid[new Point(x, y)];
                if (cell == LAND) {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.Write(cell);
                Console.ResetColor();
            }
            Console.WriteLine();
        }
        Test.Step();
    }
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
