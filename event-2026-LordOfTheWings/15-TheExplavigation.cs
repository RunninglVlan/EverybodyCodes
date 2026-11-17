using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

Test.Until = "Part I";
// https://everybody.codes/event/2026/quests/15
Test.Run((notes, part, expected) => {
    var grid = new Grid();
    grid.Read(notes);
    int seconds = grid.CollectSulphurAndReachEnd();
    Assert.ActualEqualsExpected(seconds, expected);
});

record PathState(Movement Movement, int[] TargetsReached) : IState<PathState> {
    public int EndTargets { get; set; }
    public int Seconds => TargetsReached.Sum();
    public bool ReachedEnd => EndTargets == 0 && TargetsReached.Last() != 0;

    public bool SulphurCollected(int i) => TargetsReached[i] != 0;

    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public PathState From(Point point, List<Movement> path, int i) {
        var direction = path[^1].Point - path[^2].Point;
        var next = new PathState(new Movement(point, direction), [.. TargetsReached]);
        next.TargetsReached[i] = path.Count - 1;
        next.EndTargets = EndTargets;
        return next;
    }

    public bool IsBetterThan(PathState other) => Seconds < other.Seconds;

    public virtual bool Equals(PathState? other) {
        return other is not null && TargetsReached.SequenceEqual(other.TargetsReached);
    }

    public override int GetHashCode() {
        var hash = new HashCode();
        foreach (int item in TargetsReached) {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

class Grid : IGame<PathState> {
    readonly StateSearch<PathState> stateSearch;

    int width, height;
    Point start;
    readonly Dictionary<Point, bool> walkableCells = [];
    readonly List<Point> sulphurCells = [];
    readonly List<Point> endTargets = [];

    public Grid() {
        stateSearch = new StateSearch<PathState>(this, 100);
    }

    public void Read(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        width = rows[0].Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var point = new Point(x, y);
                char cell = rows[y][x];
                walkableCells.Add(point, cell != Cells.WALL);
                switch (cell) {
                    case Cells.START:
                        start = point;
                        break;
                    case Cells.SULPHUR:
                        sulphurCells.Add(point);
                        break;
                }
            }
        }
        FindEndTargets();
    }

    void FindEndTargets() {
        for (var x = 1; x < width; x++) {
            if (!TryFindPass(x, out var pass)) {
                continue;
            }
            if (endTargets.Count > 0 && endTargets.Last().X == pass.X - 1) {
                endTargets.RemoveAt(endTargets.Count - 1);
            }
            endTargets.Add(pass);
        }
        return;

        bool TryFindPass(int x, out Point pass) {
            pass = default;
            for (var y = 0; y < height; y++) {
                var point = new Point(x, y);
                if (!walkableCells[point]) {
                    continue;
                }
                if (pass != default) {
                    return false;
                }
                pass = point;
            }
            return true;
        }
    }

    public int CollectSulphurAndReachEnd() {
        var startMovement = new Movement(start, Point.RIGHT);
        var startState = new PathState(startMovement, new int[sulphurCells.Count + endTargets.Count]) {
            EndTargets = endTargets.Count
        };
        var endState = stateSearch.FindBest(startState)!;
        return endState.Seconds;
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    IEnumerable<PathState> IGame<PathState>.Next(PathState current) {
        Func<Movement, IEnumerable<Movement>> nextMovement = it => NextMovement(current, it);
        var sulphurCollected = true;
        int endTargetIndex = endTargets.Count - current.EndTargets;
        var endTarget = endTargets[endTargetIndex];
        for (var i = 0; i < sulphurCells.Count; i++) {
            var sulphur = sulphurCells[i];
            if (sulphur.X > endTarget.X || current.SulphurCollected(i)) {
                continue;
            }
            var paths = Pathfinding<Movement>.Shortest(current.Movement, new Movement(sulphur), nextMovement);
            if (paths.Count == 0) {
                continue;
            }
            foreach (var path in paths) {
                yield return current.From(sulphur, path, i);
            }
            sulphurCollected = false;
        }
        if (sulphurCollected) {
            var paths = Pathfinding<Movement>.Shortest(current.Movement, new Movement(endTarget), nextMovement);
            var next = current.From(endTarget, paths.First(), sulphurCells.Count + endTargetIndex);
            next.EndTargets--;
            yield return next;
        }
    }

    IEnumerable<Movement> NextMovement(PathState state, Movement current) {
        var remainingSulphurCells = sulphurCells.Where((_, i) => state.TargetsReached[i] == 0);
        if (remainingSulphurCells.Contains(current.Point)) {
            yield break;
        }
        var endTarget = endTargets[^state.EndTargets];
        var directions = new[] {
            current.Direction, Point.TurnLeft(current.Direction), Point.TurnRight(current.Direction)
        };
        foreach (var direction in directions) {
            var nextPoint = current.Point + direction;
            if (nextPoint.X < 0 || nextPoint.X > endTarget.X) {
                continue;
            }
            var next = new Movement(nextPoint, direction);
            if (walkableCells[nextPoint]) {
                yield return next;
            }
        }
    }

    bool IGame<PathState>.HasReachedEnd(PathState state) => state.ReachedEnd;
}

readonly record struct Movement(Point Point, Point Direction = default) {
    public bool Equals(Movement other) => Point == other.Point;
    public override int GetHashCode() => Point.GetHashCode();
}

static class Cells {
    public const char START = 'S';
    public const char END = 'E';
    public const char WALL = '#';
    public const char SULPHUR = '@';
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);

    public static char ToChar(Point direction) => direction switch {
        _ when direction == UP => 'v',
        _ when direction == DOWN => '^',
        _ when direction == LEFT => '<',
        _ when direction == RIGHT => '>',
        _ => throw new ArgumentException($"Unsupported direction: {direction}")
    };

    public static Point TurnLeft(Point direction) => new(-direction.Y, direction.X);
    public static Point TurnRight(Point direction) => new(direction.Y, -direction.X);

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator +(Point a, int value) => new(a.X + value, a.Y + value);
    public static Point operator -(Point a, int value) => new(a.X - value, a.Y - value);
    public static Point operator -(Point value) => new(-value.X, -value.Y);

    public override string ToString() => $"({X}, {Y})";
}

static class Pathfinding<T> where T : IEquatable<T> {
    /// BFS
    public static List<List<T>> Shortest(T start, T end, Func<T, IEnumerable<T>> getNext) {
        List<List<T>> paths = [];
        Dictionary<T, T> parents = [];
        Queue<T> queue = [];
        HashSet<T> visited = [];
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.TryDequeue(out var current)) {
            if (current.Equals(end)) {
                paths.Add(Reconstruct(parents, start, current));
                if (paths.Count == 2) {
                    return paths;
                }
                continue;
            }
            foreach (var next in getNext(current)) {
                if (!visited.Add(next) && !next.Equals(end)) {
                    continue;
                }
                queue.Enqueue(next);
                parents[next] = current;
            }
        }
        return paths;
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

class StateSearch<T>(IGame<T> game, int depthMax) where T : IState<T>, IEquatable<T> {
    readonly HashSet<T> visited = [];

    /// DFS/Backtracking
    public T? FindBest(T start) {
        visited.Clear();
        visited.Add(start);
        return FindBest(start, 0);
    }

    T? FindBest(T current, int depth) {
        if (game.HasReachedEnd(current)) {
            return current;
        }
        if (depth > depthMax) {
            return default;
        }
        T? best = default;
        foreach (var next in game.Next(current)) {
            if (!visited.Add(next)) {
                continue;
            }
            var nextBest = FindBest(next, depth + 1);
            if (nextBest == null) {
                continue;
            }
            if (best == null || nextBest.IsBetterThan(best)) {
                best = nextBest;
            }
        }
        return best;
    }
}

interface IGame<TState> {
    IEnumerable<TState> Next(TState current);
    bool HasReachedEnd(TState state);
}

interface IState<in T> where T : IState<T> {
    bool IsBetterThan(T other);
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
