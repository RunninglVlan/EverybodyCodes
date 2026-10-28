using System.Diagnostics;

// Test.StepEnabled = false;
// Test.From = "Part III";
// Test.Until = "Part II";
// https://everybody.codes/story/3/quests/2
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
var part = 2;
var grid = new Grid();
Test.Run((notes, part, expected) => {
    grid.Read(notes);
    int steps = grid.CountSteps(part switch {
        1 => ReachVocalBone(),
        2 => SurroundVocalBone(),
        _ => SurroundVocalBones()
    });
    Assert.ActualEqualsExpected(steps, expected);
});
return;

Part ReachVocalBone() => new(cell => IsEmpty(cell) || IsVocalBone(cell), IsVocalBone);
Part SurroundVocalBone() => new(IsEmpty, grid.SurroundedVocalBone);
Part SurroundVocalBones() => new(IsEmpty, grid.SurroundedVocalBones, 3);
bool IsEmpty(char cell) => cell == Cells.EMPTY;
bool IsVocalBone(char cell) => cell == Cells.VOCAL_BONE;

readonly record struct Part(Predicate<char> IsValid, Predicate<char> IsEnd, int Times = 1);

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
        foreach (var bone in cells.Where(it => it.Value == Cells.VOCAL_BONE).Select(it => it.Key).ToArray()) {
            FillSurrounded(bone);
        }
    }

    public int CountSteps(Part part) {
        var soundSource = cells.First(it => it.Value == Cells.SOUND_SOURCE).Key;
        cells[soundSource] = Cells.SOUND_WAVE;
        var current = soundSource;
        var steps = 0;
        while (true) {
            foreach (var direction in DIRECTIONS) {
                for (var _ = 0; _ < part.Times; _++) {
                    var next = current + direction;
                    char nextCell = cells.GetValueOrDefault(next, Cells.EMPTY);
                    if (!part.IsValid(nextCell)) {
                        break;
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
    }

    void FillSurrounded(Point current) {
        foreach (var direction in DIRECTIONS) {
            var neighbor = current + direction;
            char neighborCell = cells.GetValueOrDefault(neighbor, Cells.EMPTY);
            if (neighborCell != Cells.EMPTY || !Surrounded(neighbor, out var toFill)) {
                continue;
            }
            foreach (var point in toFill) {
                cells.Add(point, Cells.SOUND_WAVE);
            }
        }
    }

    public bool SurroundedVocalBone(char cell) {
        var vocalBone = cells.First(it => it.Value == Cells.VOCAL_BONE).Key;
        return Surrounded(vocalBone, out _);
    }

    public bool SurroundedVocalBones(char cell) {
        var vocalBones = cells.Where(it => it.Value == Cells.VOCAL_BONE);
        return vocalBones.All(it => Surrounded(it.Key, out _));
    }

    bool Surrounded(Point point, out HashSet<Point> toFill) {
        toFill = [point];
        int obstacles = DIRECTIONS
                .Select(it => cells.GetValueOrDefault(point + it, Cells.EMPTY))
                .Count(neighbor => neighbor != Cells.EMPTY);
        return obstacles == 4 || ConfirmSurrounded(point, out toFill);
    }

    bool ConfirmSurrounded(Point point, out HashSet<Point> toFill) {
        const int max = 100;
        Queue<Point> queue = new();
        var visited = toFill = [point];
        queue.Enqueue(point);
        while (queue.TryDequeue(out var current)) {
            foreach (var direction in DIRECTIONS) {
                var next = current + direction;
                char nextCell = cells.GetValueOrDefault(next, Cells.EMPTY);
                if (nextCell != Cells.EMPTY) {
                    continue;
                }
                if (visited.Add(next)) {
                    queue.Enqueue(next);
                }
                if (visited.Count == max) {
                    return false;
                }
            }
        }
        return visited.Count < max;
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
    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }

    public static void Run(Action<string, int, int> action) {
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
        bool equals = Equals(actual, expected);
        Console.WriteLine(equals ? $"✅ {actual}" : $"❌ found {actual}, but expected {expected}");
        if (!equals && Throw) {
            throw new Exception();
        }
    }
}
