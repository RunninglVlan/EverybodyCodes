using System.Diagnostics;

// https://everybody.codes/event/2026/quests/10
Test.Run((notes, part, expected) => {
    var grid = new Grid();
    grid.Init(notes);
    int collisions = grid.CollisionsAfter(10);
    Assert.ActualEqualsExpected(collisions, expected);
});

class Grid {
    bool[,] walkableCells = null!;
    readonly List<NullGhost> ghosts = [];

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        int width = rows.First().Length;
        int height = rows.Length;
        walkableCells = new bool[width, height];
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = rows[y][x];
                walkableCells[x, y] = cell != '#';
                Point? ghostDirection = cell switch {
                    '>' => Screen.RIGHT,
                    '<' => Screen.LEFT,
                    '^' => Screen.UP,
                    'v' => Screen.DOWN,
                    _ => null
                };
                if (ghostDirection.HasValue) {
                    ghosts.Add(new NullGhost(new Point(x, y), ghostDirection.Value));
                }
            }
        }
    }

    public int CollisionsAfter(int rounds) {
        return 0;
    }
}

record NullGhost(Point Point, Point Direction);

readonly record struct Point(int X, int Y);

static class Screen {
    public static readonly Point UP = new(0, -1);
    public static readonly Point DOWN = new(0, 1);
    public static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }
    public static bool LogEnabled { get; set; } = true;

    public static void Run(Action<string, int, string> action) {
        File.WriteAllText(LOG_FILE, null);
        string cases = ReadCases();
        Console.SetIn(new StringReader(cases));
        var totalTime = 0L;
        while (Console.In.Peek() != -1) {
            var stopwatch = Stopwatch.StartNew();
            string name = Console.ReadLine()!;
            LogInternal(name, console: true, color: ConsoleColor.Cyan);
            int part = int.Parse(Console.ReadLine()!);
            string notes = ReadUntilNewLine();
            string expected = ReadUntilNewLine();
            try {
                action(notes, part, expected);
            }
            finally {
                long time = stopwatch.ElapsedMilliseconds;
                totalTime += time;
                LogInternal($"Test took {time}ms", console: true);
            }
            LogInternal(console: true);
        }
        LogInternal($"All tests took {totalTime}ms", console: true);
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
        Console.ReadKey();
    }

    public static void Log(object? line = null, bool console = false, ConsoleColor? color = null) {
        if (!LogEnabled) {
            return;
        }
        LogInternal(line, console, color);
    }

    static void LogInternal(object? line = null, bool console = false, ConsoleColor? color = null) {
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
    public static bool Throw { get; set; } = true;

    public static void ActualEqualsExpected(object actual, object expected) {
        bool equals = Equals(actual.ToString(), expected);
        Console.WriteLine(equals ? $"✅ {actual}" : $"❌ found {actual}, but expected {expected}");
        if (!equals && Throw) {
            throw new Exception();
        }
    }
}
