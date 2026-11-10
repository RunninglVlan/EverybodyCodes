using System.Diagnostics;

// https://everybody.codes/event/2026/quests/11
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
Test.Run((notes, part, expected) => {
    (int sideNeighbors, bool minimizeStrain) = part switch {
        1 => (1, false),
        _ => (3, true)
    };
    var portals = new Portals(sideNeighbors);
    portals.Build(notes);
    Assert.ActualEqualsExpected(portals.Resonance(minimizeStrain), expected);
});

class Portals(int sideNeighbors) {
    Portal[] graph = null!;
    readonly List<(int a, int b)> shortcuts = [];

    public void Build(string notes) {
        string[] data = notes.Split(Environment.NewLine);
        string[] portalsData = data[0].Split(' ');
        int portalCount = int.Parse(portalsData.Last());
        graph = new Portal[portalCount];
        for (var id = 0; id < portalCount; id++) {
            var portal = new Portal([]);
            graph[id] = portal;
            for (var i = 1; i <= sideNeighbors; i++) {
                portal.Connections.Add(Wrap(id - i));
                portal.Connections.Add(Wrap(id + i));
            }
        }
        for (var i = 2; i < data.Length; i++) {
            AddShortcut(data[i]);
        }
        return;

        int Wrap(int id) => (id + portalCount) % portalCount;

        void AddShortcut(string shortcutData) {
            int[] shortcut = [.. shortcutData.Split(' ').Select(int.Parse).Select(it => it - 1)];
            shortcuts.Add((shortcut.First(), shortcut.Last()));
        }
    }

    void Connect(int a, int b) {
        graph[a].Connections.Add(b);
        graph[b].Connections.Add(a);
    }

    void Disconnect(int a, int b) {
        graph[a].Connections.Remove(b);
        graph[b].Connections.Remove(a);
    }

    public int Resonance(bool minimizeStrain) {
        int strain = Strain();
        foreach ((int from, int to) in shortcuts) {
            Connect(from, to);
        }
        return strain - (minimizeStrain ? MinimizeStrain() : Strain());
    }

    int Strain() {
        var result = 0;
        for (var i = 0; i < graph.Length; i++) {
            for (var j = 0; j < graph.Length; j++) {
                if (i == j) {
                    continue;
                }
                result += Pathfinding<int>.Shortest(i, j, current => graph[current].Connections)!.Count - 1;
            }
        }
        return result;
    }

    int MinimizeStrain() {
        var result = int.MaxValue;
        for (var i = 0; i < graph.Length; i++) {
            var portal = graph[i];
            for (int j = i + 1; j < graph.Length; j++) {
                if (portal.Connections.Contains(j)) {
                    continue;
                }
                Connect(i, j);
                result = Math.Min(Strain(), result);
                Disconnect(i, j);
            }
        }
        return result;
    }
}

readonly record struct Portal(List<int> Connections);

static class Pathfinding<T> where T : IEquatable<T> {
    /// BFS to end
    public static List<T>? Shortest(T start, T end, Func<T, IEnumerable<T>> getNext) {
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

    static List<T> Reconstruct(Dictionary<T, T> parents, T start, T end) {
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
            string notes = ReadUntilLine(it => it == EXPECTED_SEPARATOR);
            string expected = ReadUntilLine(string.IsNullOrEmpty);
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

    static string ReadUntilLine(Predicate<string> endPredicate) {
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
