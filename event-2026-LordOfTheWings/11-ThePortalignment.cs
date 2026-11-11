using System.Diagnostics;

Test.StepEnabled = false;
Test.From = "Part III Example 1";
Test.Until = "Part III";
Test.LogEnabled = false;
Test.ExpectedSeparator = "---";
// Assert.Throw = false;
// https://everybody.codes/event/2026/quests/11
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
Test.Run((notes, part, expected) => {
    (int sideNeighbors, int crystalConnections) = part switch {
        1 => (1, 0),
        2 => (3, 1),
        _ => (3, 4)
    };
    var portals = new Portals(sideNeighbors, crystalConnections);
    portals.Build(notes);
    int resonance = portals.Resonance();
    Assert.ActualEqualsExpected(resonance, expected);
    // Console.WriteLine(resonance);
});

class Portals(int sideNeighbors, int crystalConnections) {
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

    public int Resonance() {
        int strain = Strain();
        foreach ((int from, int to) in shortcuts) {
            Connect(from, to);
        }
        int strainWithShortcuts = crystalConnections switch {
            0 => Strain(),
            1 => MinimizeStrainWith1Extra(),
            _ => MinimizeStrainWith4Extras()
        };
        return strain - strainWithShortcuts;
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

    int progress = -1;

    int MinimizeStrainWith1Extra() {
        var result = int.MaxValue;
        for (var i = 0; i < graph.Length; i++) {
            var portal = graph[i];
            for (int j = i + 1; j < graph.Length; j++) {
                if (portal.Connections.Contains(j)) {
                    continue;
                }
                Connect(i, j);
                AddProgress(i, graph.Length);
                result = Math.Min(Strain(), result);
                Disconnect(i, j);
            }
        }
        return result;
    }

    void AddProgress(int i, float length) {
        if (i == progress) {
            return;
        }
        Console.Error.WriteLine($"progress: {i / length:F}");
        progress = i;
    }

    int MinimizeStrainWith4Extras() {
        HashSet<int> usedCrystals = [];
        HashSet<CrystalPairs> usedPairs = [];
        var result = int.MaxValue;
        int crystalDistance = graph.Length / (4 * 2);
        for (var i0 = 0; i0 < crystalDistance; i0++) {
            var i0Portal = graph[i0];
            for (int i1 = i0 + crystalDistance; i1 < graph.Length; i1 += crystalDistance) {
                if (i0Portal.Connections.Contains(i1)) {
                    continue;
                }
                usedCrystals.Add(i1);
                Connect(i0, i1);
                var iPair = CrystalPair.From(i0, i1);
                for (int j0 = i0 + crystalDistance; j0 < graph.Length; j0 += crystalDistance) {
                    if (!usedCrystals.Add(j0)) {
                        continue;
                    }
                    var j0Portal = graph[j0];
                    for (int j1 = i0 + crystalDistance; j1 < graph.Length; j1 += crystalDistance) {
                        if (!usedCrystals.Add(j1) || j0Portal.Connections.Contains(j1)) {
                            continue;
                        }
                        Connect(j0, j1);
                        var jPair = CrystalPair.From(j0, j1);
                        for (int k0 = i0 + crystalDistance; k0 < graph.Length; k0 += crystalDistance) {
                            if (!usedCrystals.Add(k0)) {
                                continue;
                            }
                            var k0Portal = graph[k0];
                            for (int k1 = i0 + crystalDistance; k1 < graph.Length; k1 += crystalDistance) {
                                if (!usedCrystals.Add(k1) || k0Portal.Connections.Contains(k1)) {
                                    continue;
                                }
                                Connect(k0, k1);
                                var kPair = CrystalPair.From(k0, k1);
                                for (int l0 = i0 + crystalDistance; l0 < graph.Length; l0 += crystalDistance) {
                                    if (!usedCrystals.Add(l0)) {
                                        continue;
                                    }
                                    var l0Portal = graph[l0];
                                    for (int l1 = i0 + crystalDistance; l1 < graph.Length; l1 += crystalDistance) {
                                        if (usedCrystals.Contains(l1) || l0Portal.Connections.Contains(l1)) {
                                            continue;
                                        }
                                        var lPair = CrystalPair.From(l0, l1);
                                        if (!usedPairs.Add(CrystalPairs.From(iPair, jPair, kPair, lPair))) {
                                            continue;
                                        }
                                        Connect(l0, l1);
                                        AddProgress(i0, crystalDistance);
                                        result = Math.Min(Strain(), result);
                                        Disconnect(l0, l1);
                                    }
                                    usedCrystals.Remove(l0);
                                }
                                usedCrystals.Remove(k1);
                                Disconnect(k0, k1);
                            }
                            usedCrystals.Remove(k0);
                        }
                        usedCrystals.Remove(j1);
                        Disconnect(j0, j1);
                    }
                    usedCrystals.Remove(j0);
                }
                usedCrystals.Remove(i1);
                Disconnect(i0, i1);
            }
        }
        return result;
    }
}

readonly record struct Portal(List<int> Connections);

readonly record struct CrystalPair(int A, int B) {
    public static CrystalPair From(int a, int b) {
        return a < b ? new CrystalPair(a, b) : new CrystalPair(b, a);
    }
}

readonly record struct CrystalPairs(CrystalPair A, CrystalPair B, CrystalPair C, CrystalPair D) {
    public static CrystalPairs From(CrystalPair a, CrystalPair b, CrystalPair c, CrystalPair d) {
        Order(ref a, ref b);
        Order(ref c, ref d);
        Order(ref a, ref c);
        Order(ref b, ref d);
        Order(ref b, ref c);
        return new CrystalPairs(a, b, c, d);

        void Order(ref CrystalPair x, ref CrystalPair y) {
            if (x.A > y.A) {
                (x, y) = (y, x);
            }
        }
    }
}

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
