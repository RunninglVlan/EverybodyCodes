using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

Test.StepEnabled = false;
// https://everybody.codes/event/2026/quests/8
Test.Run((notes, part, expected) => {
    var grid = new HexGrid();
    grid.Init(notes);
    int rounds = part switch {
        1 => 1,
        _ => 6
    };
    Assert.ActualEqualsExpected(grid.CalculateEnergy(rounds), expected);
});

class HexGrid {
    int width, height;
    readonly Dictionary<Point, Ent> ents = [];
    Point start;
    Point sourceDirection;
    Point[] nextDirections = null!;

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        width = rows.First().Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char plot = rows[y][x];
                if (plot == '.') {
                    continue;
                }
                ents[new Point(x, y)] = new Ent((int)char.GetNumericValue(plot));
            }
        }
    }

    public int CalculateEnergy(int rounds) {
        var energy = 0;
        for (var i = 0; i < rounds; i++) {
            InitRound(i);
            foreach (var ent in ents.Values) {
                ent.Shadow = 0;
                ent.GotEnergy = false;
            }
            do {
                for (var current = start; ents.TryGetValue(current, out var ent); current += sourceDirection) {
                    if (ent.Height <= ent.Shadow) {
                        continue;
                    }
                    // Test.Log($"ent {ent.Height} with shadow {ent.Shadow} got energy");
                    energy++;
                    ent.GotEnergy = true;
                    CastShadow(ent.Height, current);
                }
            } while (TryNextStart());
            Print();
            Test.Step();
        }
        return energy;
    }

    void InitRound(int i) {
        switch (i) {
            case 0:
                start = new Point(0, height / 2);
                sourceDirection = Screen.BOTTOM_RIGHT;
                nextDirections = [Screen.TOP_RIGHT, Screen.HEX_RIGHT];
                break;
            case 1:
                start = new Point(width / 4, 0);
                sourceDirection = Screen.BOTTOM_LEFT;
                nextDirections = [Screen.HEX_RIGHT, Screen.BOTTOM_RIGHT];
                break;
            case 2:
                start = new Point(width * 3 / 4, 0);
                sourceDirection = Screen.HEX_LEFT;
                nextDirections = [Screen.BOTTOM_RIGHT, Screen.BOTTOM_LEFT];
                break;
            case 3:
                start = new Point(width - 1, height / 2);
                sourceDirection = Screen.TOP_LEFT;
                nextDirections = [Screen.BOTTOM_LEFT, Screen.HEX_LEFT];
                break;
            case 4:
                start = new Point(width * 3 / 4, height - 1);
                sourceDirection = Screen.TOP_RIGHT;
                nextDirections = [Screen.HEX_LEFT, Screen.TOP_LEFT];
                break;
            default:
                start = new Point(width / 4, height - 1);
                sourceDirection = Screen.HEX_RIGHT;
                nextDirections = [Screen.TOP_LEFT, Screen.TOP_RIGHT];
                break;
        }
    }

    void CastShadow(int entHeight, Point current) {
        for (int shadow = entHeight - 1; shadow > 0; shadow--) {
            current += sourceDirection;
            if (!ents.TryGetValue(current, out var ent)) {
                break;
            }
            ent.Shadow = Math.Max(shadow, ent.Shadow);
            // Test.Log($"  ent {ent.Height} got shadow {shadow}, new shadow: {ent.Shadow}");
        }
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    bool TryNextStart() {
        foreach (var direction in nextDirections) {
            var startNext = start + direction;
            if (ents.ContainsKey(startNext)) {
                start = startNext;
                return true;
            }
        }
        return false;
    }

    void Print() {
        var rows = new StringBuilder();
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (!ents.TryGetValue(new Point(x, y), out var ent)) {
                    rows.Append('.');
                    continue;
                }
                rows.Append(ent.GotEnergy ? ent.Height : ".");
            }
            rows.AppendLine();
        }
        Test.Log(rows);
    }
}

record Ent(int Height) {
    public int Shadow { get; set; }
    public bool GotEnergy { get; set; }
}

readonly record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}

static class Screen {
    public static readonly Point TOP_LEFT = new(-1, -1);
    public static readonly Point TOP_RIGHT = new(1, -1);
    public static readonly Point HEX_LEFT = new(-2, 0);
    public static readonly Point HEX_RIGHT = new(2, 0);
    public static readonly Point BOTTOM_LEFT = new(-1, 1);
    public static readonly Point BOTTOM_RIGHT = new(1, 1);
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
        var totalTime = 0L;
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
