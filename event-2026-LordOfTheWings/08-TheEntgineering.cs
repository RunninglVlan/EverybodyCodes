using System.Diagnostics;

Test.StepEnabled = false;
// https://everybody.codes/event/2026/quests/8
Test.Run((notes, part, expected) => {
    var grid = new HexGrid();
    grid.Init(notes);
    Assert.ActualEqualsExpected(grid.CalculateEnergy(), expected);
});

class HexGrid {
    static readonly Point SOURCE_DIRECTION = new(1, 1);
    static readonly Point[] NEXT_DIRECTIONS = [new(1, -1), new(2, 0)];

    int height;
    readonly Dictionary<Point, Ent> ents = [];

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        int width = rows.First().Length;
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

    public int CalculateEnergy() {
        var start = new Point(0, height / 2);
        var energy = 0;
        while (true) {
            var current = start;
            while (true) {
                if (!ents.TryGetValue(current, out var ent)) {
                    break;
                }
                if (ent.Height > ent.Shadow) {
                    Test.Log($"ent {ent.Height} with shadow {ent.Shadow} got energy");
                    energy++;
                    ent.GotEnergy = true;
                    CastShadow(ent.Height, current);
                    Print();
                    Test.Step();
                }
                current += SOURCE_DIRECTION;
            }
            var startNext = start + NEXT_DIRECTIONS.First();
            if (!ents.ContainsKey(startNext)) {
                startNext = start + NEXT_DIRECTIONS.Last();
                if (!ents.ContainsKey(startNext)) {
                    return energy;
                }
            }
            start = startNext;
        }
    }

    void CastShadow(int entHeight, Point current) {
        for (int shadow = entHeight - 1; shadow > 0; shadow--) {
            current += SOURCE_DIRECTION;
            if (!ents.TryGetValue(current, out var ent)) {
                break;
            }
            ent.Shadow = Math.Max(shadow, ent.Shadow);
            Test.Log($"  ent {ent.Height} got shadow {shadow}, new shadow: {ent.Shadow}");
        }
    }

    void Print() {
        return;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < 17; x++) {
                if (!ents.TryGetValue(new Point(x, y), out var ent)) {
                    Console.Write('.');
                    continue;
                }
                Console.Write(ent.GotEnergy ? ent.Height : ".");
            }
            Console.WriteLine();
        }
    }
}

record Ent(int Height) {
    public int Shadow { get; set; }
    public bool GotEnergy { get; set; }
}

readonly record struct Point(int X, int Y) {
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
