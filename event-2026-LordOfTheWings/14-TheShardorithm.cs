using System.Diagnostics;

// https://everybody.codes/event/2026/quests/14
Test.Run((notes, part, expected) => {
    var portal = new PortalGrid();
    portal.Read(notes);
    int bindings = part switch {
        1 => portal.Repair(),
        2 => portal.RepairOptimally(),
        _ => portal.AddChargesAndRepairOptimally()
    };
    Assert.ActualEqualsExpected(bindings, expected);
});

class PortalGrid {
    const char FRAME = ' ';

    readonly Dictionary<char, Piece> pieces = [];

    public void Read(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        int width = rows[0].Length, height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var point = new Point(x, y);
                char value = rows[y][x];
                foreach (var direction in Point.Directions4) {
                    var neighborPoint = point + direction;
                    char neighborValue = neighborPoint.InBounds(width, height)
                            ? rows[neighborPoint.Y][neighborPoint.X]
                            : FRAME;
                    if (neighborValue == value) {
                        continue;
                    }
                    var binding = new Binding(point, neighborPoint);
                    Bindings(value, neighborValue).Add(binding);
                    Bindings(neighborValue, value).Add(binding);
                }
            }
        }
    }

    HashSet<Binding> Bindings(char pieceValue, char neighborValue) {
        if (!pieces.TryGetValue(pieceValue, out var piece)) {
            pieces.Add(pieceValue, piece = new Piece([]));
        }
        if (!piece.Bindings.TryGetValue(neighborValue, out var bindings)) {
            piece.Bindings.Add(neighborValue, bindings = []);
        }
        return bindings;
    }

    public int Repair() {
        return pieces.Values.SelectMany(piece => piece.Bindings.Values.SelectMany(it => it)).ToHashSet().Count;
    }

    public int RepairOptimally() {
        // todo: keep only char to
        PriorityQueue<(char from, char to), int> queue = new();
        Dictionary<char, int> connected = [];
        queue.Enqueue((FRAME, FRAME), 0);
        while (queue.TryDequeue(out var current, out int currentBindings)) {
            if (!connected.TryAdd(current.to, currentBindings)) {
                continue;
            }
            // Test.Log($"connected {current.from}-{current.to}: {currentBindings}");
            foreach ((char next, var bindings) in pieces[current.to].Bindings) {
                queue.Enqueue((current.to, next), bindings.Count);
            }
        }
        return connected.Values.Sum();
    }

    public int AddChargesAndRepairOptimally() {
        var minConnected = int.MaxValue;
        PriorityQueue<(char from, char to), int> queue = new();
        Dictionary<char, int> connected = [];
        char[] pieceValues = [.. pieces.Keys.Where(it => it != FRAME)];
        List<(char first, char second, int count)> pairs = [];
        for (var i = 0; i < pieceValues.Length; i++) {
            char piece1Value = pieceValues[i];
            var piece1 = pieces[piece1Value];
            for (int j = i + 1; j < pieceValues.Length; j++) {
                char piece2Value = pieceValues[j];
                var piece2 = pieces[piece2Value];
                int count = piece1.Bindings.Keys.Union(piece2.Bindings.Keys).Count();
                pairs.Add((piece1Value, piece2Value, count));
            }
        }
        foreach (var pair in pairs) {
            queue.Clear();
            connected.Clear();
            // Test.Log(pair);
            queue.Enqueue((FRAME, pair.first), 0);
            foreach (char value in pieces[pair.first].Bindings.Keys) {
                queue.Enqueue((FRAME, value), 0);
            }
            queue.Enqueue((FRAME, pair.second), 0);
            foreach (char value in pieces[pair.second].Bindings.Keys) {
                queue.Enqueue((FRAME, value), 0);
            }
            while (queue.TryDequeue(out var current, out int currentBindings)) {
                if (!connected.TryAdd(current.to, currentBindings)) {
                    continue;
                }
                // Test.Log($"connected {current.from}-{current.to}: {currentBindings}");
                foreach ((char next, var bindings) in pieces[current.to].Bindings) {
                    queue.Enqueue((current.to, next), bindings.Count);
                }
            }
            minConnected = Math.Min(connected.Values.Sum(), minConnected);
        }
        return minConnected;
    }
}

record Piece(Dictionary<char, HashSet<Binding>> Bindings);

readonly record struct Binding(Point A, Point B) {
    public bool Equals(Binding other) => A == other.A && B == other.B || A == other.B && B == other.A;
    public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
    public override string ToString() => $"{A}-{B}";
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    static readonly Point RIGHT = new(1, 0);

    public static Point[] Directions4 { get; } = [RIGHT, LEFT, UP, DOWN];

    public bool InBounds(int width, int? height = null) {
        return X >= 0 && X < width && Y >= 0 && Y < (height ?? width);
    }

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);

    public override string ToString() => $"({X}, {Y})";
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }
    public static bool LogEnabled { get; set; } = true;
    public static string? ExpectedSeparator { get; set; }

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
            Predicate<string> noteEnd = ExpectedSeparator != null
                    ? it => it == ExpectedSeparator
                    : string.IsNullOrEmpty;
            string notes = ReadLinesUntil(noteEnd);
            string expected = ReadLinesUntil(string.IsNullOrEmpty);
            try {
                action(notes, part, expected);
            } catch (Exception e) {
                Console.WriteLine(e);
                break;
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

    static string ReadLinesUntil(Predicate<string> endPredicate) {
        List<string> lines = [];
        for (string line; !endPredicate(line = Console.ReadLine()!);) {
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

    public static void Log(object? line = null, bool console = true, ConsoleColor? color = null) {
        if (!LogEnabled) {
            return;
        }
        LogInternal(line, console, color ?? ConsoleColor.Red);
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
