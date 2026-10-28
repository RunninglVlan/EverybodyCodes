using System.Diagnostics;

// https://everybody.codes/story/3/quests/2
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
var part = 2;
var grid = new Grid();
Test.Run((notes, part, expected) => {
    grid.Read(notes);
    Assert.ActualEqualsExpected(grid.CountSteps(part == 1 ? ReachVocalBone() : SurroundVocalBone()), expected);
});
return;

Part ReachVocalBone() => new(cell => IsEmpty(cell) || IsVocalBone(cell), IsVocalBone);
Part SurroundVocalBone() => new(IsEmpty, grid.SurroundedVocalBone);
bool IsEmpty(char cell) => cell == Cells.EMPTY;
bool IsVocalBone(char cell) => cell == Cells.VOCAL_BONE;

readonly record struct Part(Predicate<char> IsValid, Predicate<char> IsEnd);

class Grid {
    static readonly Point[] DIRECTIONS = [Screen.UP, Screen.RIGHT, Screen.DOWN, Screen.LEFT];

    readonly Dictionary<Point, char> cells = [];

    public void Read(string input) {
        cells.Clear();
        string[] rows = input.Split(Environment.NewLine);
        int width = rows[0].Length, height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = rows[y][x];
                if (cell is not (Cells.SOUND_SOURCE or Cells.VOCAL_BONE)) {
                    continue;
                }
                cells.Add(new Point(x, y), cell);
            }
        }
    }

    public int CountSteps(Part part) {
        var soundSource = cells.First(it => it.Value == Cells.SOUND_SOURCE).Key;
        cells[soundSource] = Cells.SOUND_WAVE;
        var current = soundSource;
        var steps = 0;
        while (true) {
            foreach (var direction in DIRECTIONS) {
                var next = current + direction;
                char nextCell = cells.GetValueOrDefault(next, Cells.EMPTY);
                if (!part.IsValid(nextCell)) {
                    continue;
                }
                cells[next] = Cells.SOUND_WAVE;
                current = next;
                steps++;
                FillSurrounded(current);
                if (part.IsEnd(nextCell)) {
                    return steps;
                }
            }
        }
    }

    void FillSurrounded(Point current) {
        foreach (var direction in DIRECTIONS) {
            var neighbor = current + direction;
            char neighborCell = cells.GetValueOrDefault(neighbor, Cells.EMPTY);
            if (neighborCell == Cells.EMPTY && Surrounded(neighbor)) {
                cells.Add(neighbor, Cells.SOUND_WAVE);
            }
        }
    }

    public bool SurroundedVocalBone(char _) {
        var vocalBone = cells.First(it => it.Value == Cells.VOCAL_BONE).Key;
        return Surrounded(vocalBone);
    }

    bool Surrounded(Point point) {
        return DIRECTIONS
                .Select(it => cells.GetValueOrDefault(point + it, Cells.EMPTY))
                .All(neighbor => neighbor != Cells.EMPTY);
    }
}

static class Cells {
    public const char EMPTY = '.';
    public const char SOUND_SOURCE = '@';
    public const char VOCAL_BONE = '#';
    public const char SOUND_WAVE = '+';
}

readonly record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}

static class Screen {
    public static readonly Point UP = new(0, -1);
    public static readonly Point DOWN = new(0, 1);
    public static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    static readonly TextReader consoleIn = Console.In;
    public static bool StepEnabled { get; set; }
    public static string? From { get; set; }
    public static string? Until { get; set; }

    public static void Run(Action<string, int, int> action) {
        File.WriteAllText(LOG_FILE, null);
        string cases = File.ReadAllText(CASE_FILE);
        if (From != null) {
            cases = cases[cases.IndexOf(From, StringComparison.Ordinal)..];
        }
        if (Until != null) {
            cases = cases[..cases.IndexOf(Until, StringComparison.Ordinal)];
        }
        Console.SetIn(new StringReader(cases));
        var totalTime = 0L;
        while (Console.In.Peek() != -1) {
            var stopwatch = Stopwatch.StartNew();
            string name = Console.ReadLine()!;
            Log(name, console: true, color: ConsoleColor.Cyan);
            int part = int.Parse(Console.ReadLine()!);
            string notes = ReadUntilNewLine();
            int expected = int.Parse(ReadUntilNewLine());
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
        return;

        static string ReadUntilNewLine() {
            List<string> lines = [];
            for (string line; !string.IsNullOrEmpty(line = Console.ReadLine()!);) {
                lines.Add(line);
            }
            return string.Join(Environment.NewLine, lines);
        }
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
        bool equals = Equals(actual, expected);
        Console.WriteLine(equals ? $"✅ {actual}" : $"❌ found {actual}, but expected {expected}");
        if (!equals && Throw) {
            throw new Exception();
        }
    }
}
