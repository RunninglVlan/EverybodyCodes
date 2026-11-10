using System.Diagnostics;

// https://everybody.codes/event/2026/quests/11
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
Test.Run((notes, part, expected) => {
    var portals = new Portals();
    portals.Build(notes);
    Assert.ActualEqualsExpected(portals.Resonance(), expected);
});

class Portals {
    Portal[] graph = null!;

    public void Build(string notes) {
        string[] data = notes.Split(Environment.NewLine);
        string[] portalsData = data[0].Split(' ');
        int portalCount = int.Parse(portalsData.Last());
        graph = new Portal[portalCount];
        for (var i = 0; i < portalCount; i++) {
            var portal = new Portal([]);
            graph[i] = portal;
            portal.Connections.Add(i > 0 ? i - 1 : portalCount - 1);
            portal.Connections.Add(i < portalCount - 1 ? i + 1 : 0);
        }
    }

    public int Resonance() {
        var strain = 0;
        for (var i = 0; i < graph.Length; i++) {
            for (var j = 0; j < graph.Length; j++) {
                if (i == j) {
                    continue;
                }
                strain += Pathfinding.Shortest(i, j, current => graph[current].Connections)!.Count - 1;
            }
        }
        return strain;
    }

    int Strain() {
        int result = graph.Length / 2;
        for (var i = 1; i < graph.Length / 2; i++) {
            result += i * 2;
        }
        return result * graph.Length;
    }
}

readonly record struct Portal(List<int> Connections);

static class Pathfinding {
    /// BFS to end
    public static List<T>? Shortest<T>(T start, T end, Func<T, IEnumerable<T>> getNext) where T : IEquatable<T> {
        Dictionary<T, T> parents = [];
        Queue<T> queue = [];
        HashSet<T> visited = [];
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.TryDequeue(out var current)) {
            if (current.Equals(end)) {
                return Reconstruct(parents, start, current);
            }
            foreach (var next in getNext(current)) {
                if (!visited.Add(next)) {
                    continue;
                }
                queue.Enqueue(next);
                parents[next] = current;
            }
        }
        return null;
    }

    static List<T> Reconstruct<T>(Dictionary<T, T> parents, T start, T end) where T : IEquatable<T> {
        var path = new List<T>();
        var current = end;
        while (!current.Equals(start)) {
            path.Add(current);
            current = parents[current];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";
    const string EXPECTED_SEPARATOR = "---";

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
            string notes = ReadUntilLine(EXPECTED_SEPARATOR);
            string expected = ReadUntilLine(null);
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

    static string ReadUntilLine(string? separator) {
        List<string> lines = [];
        for (string line; (line = Console.ReadLine()!) != separator;) {
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
