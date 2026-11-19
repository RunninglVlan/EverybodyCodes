using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

Test.From = "Part II";
Test.Until = "Part III Example";
// https://everybody.codes/event/2026/quests/15
Test.Run((notes, part, expected) => {
    Grid grid = part switch {
        1 => new Grid1(),
        _ => new Grid2()
    };
    grid.Read(notes);
    int seconds = grid.CollectSulphurAndReachEnd();
    Assert.ActualEqualsExpected(seconds, expected);
});

abstract class Grid {
    int width, height;
    protected Point start;
    readonly Dictionary<Point, bool> walkableCells = [];
    readonly List<Point> sulphurDeposits = [];
    protected readonly List<SulphurCycle> sulphurCycles = [];
    protected readonly List<Point> endTargets = [];

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
                        sulphurDeposits.Add(point);
                        break;
                }
            }
        }
        FindCycles();
        FindEndTargets();
    }

    void FindCycles() {
        List<HashSet<Point>> cyclePaths = [];
        foreach (var cell in sulphurDeposits.Where(cell => !cyclePaths.Any(it => it.Contains(cell)))) {
            foreach (var direction in Point.Directions4) {
                var next = cell + direction;
                if (!walkableCells[next]) {
                    continue;
                }
                var startMove = new Move(next, direction);
                var path = Pathfinding<Move>.Shortest(startMove, new Move(cell), NextMove)!
                        .Select(it => it.Point)
                        .ToHashSet();
                cyclePaths.Add(path);
                int deposits = path.Count(it => sulphurDeposits.Contains(it));
                sulphurCycles.Add(new SulphurCycle(path, deposits));
                break;
            }
        }
    }

    IEnumerable<Move> NextMove(Move current) {
        var directions = new[] {
            current.Direction, Point.TurnLeft(current.Direction), Point.TurnRight(current.Direction)
        };
        foreach (var direction in directions) {
            var nextPoint = current.Point + direction;
            if (nextPoint.X < 0 || nextPoint.X > width - 1) {
                continue;
            }
            var next = new Move(nextPoint, direction);
            if (walkableCells[nextPoint]) {
                yield return next;
            }
        }
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

    public abstract int CollectSulphurAndReachEnd();
}

static class Cells {
    public const char START = 'S';
    public const char WALL = '#';
    public const char SULPHUR = '@';
}

readonly record struct Move(Point Point, Point Direction = default) {
    public bool Equals(Move other) => Point == other.Point;
    public override int GetHashCode() => Point.GetHashCode();
}

readonly record struct SulphurCycle {
    public int Deposits { get; }
    public (int min, int max) X { get; }
    public int Perimeter { get; }
    public int Width => X.max - X.min;

    public SulphurCycle(HashSet<Point> path, int deposits) {
        Deposits = deposits;
        int height = path.Max(it => it.Y) - path.Min(it => it.Y);
        X = (path.Min(it => it.X), path.Max(it => it.X));
        Perimeter = 2 * (height + Width);
    }
}

readonly record struct PathState1(int X, bool[] CyclesVisited, int Seconds = 0) : IState<PathState1> {
    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public PathState1 From(SulphurCycle cycle, int i, int xStart) {
        int nextX = cycle.X.min == xStart ? cycle.X.max : cycle.X.min;
        int distance = Math.Abs(X - xStart) + cycle.Perimeter - cycle.Width;
        var next = new PathState1(nextX, [.. CyclesVisited], Seconds + distance);
        next.CyclesVisited[i] = true;
        return next;
    }

    public bool IsBetterThan(PathState1 other) => Seconds < other.Seconds;

    public bool Equals(PathState1 other) {
        return X == other.X && CyclesVisited.SequenceEqual(other.CyclesVisited) && Seconds == other.Seconds;
    }

    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(X);
        foreach (bool item in CyclesVisited) {
            hash.Add(item);
        }
        hash.Add(Seconds);
        return hash.ToHashCode();
    }
}

class Grid1 : Grid, IGame<PathState1> {
    readonly StateSearch<PathState1> stateSearch;

    public Grid1() {
        stateSearch = new StateSearch<PathState1>(this);
    }

    public override int CollectSulphurAndReachEnd() {
        var startState = new PathState1(start.X, new bool[sulphurCycles.Count]);
        return stateSearch.TryFindBest(startState, out var endState)
                ? endState.Seconds
                : throw new UnreachableException();
    }

    IEnumerable<PathState1> IGame<PathState1>.Next(PathState1 current) {
        var nextCycles = NextCycles(current);
        if (nextCycles.Count > 0) {
            foreach ((var cycle, int i) in nextCycles) {
                yield return current.From(cycle, i, cycle.X.min);
                yield return current.From(cycle, i, cycle.X.max);
            }
        } else {
            int nextX = endTargets.Last().X;
            int distance = nextX - current.X;
            yield return current with { X = nextX, Seconds = current.Seconds + distance };
        }
    }

    List<(SulphurCycle, int)> NextCycles(PathState1 state) {
        List<(SulphurCycle, int)> cycles = [];
        foreach (var endTarget in endTargets) {
            for (var i = 0; i < sulphurCycles.Count; i++) {
                var cycle = sulphurCycles[i];
                if (state.CyclesVisited[i] || cycle.X.max > endTarget.X) {
                    continue;
                }
                cycles.Add((cycle, i));
            }
            if (cycles.Count > 0) {
                return cycles;
            }
        }
        return cycles;
    }

    bool IGame<PathState1>.HasReachedEnd(PathState1 state) => state.X == endTargets.Last().X;
}

readonly record struct PathState2(int X, int SulphurCollected = 0) : IState<PathState2> {
    public PathState2 From(SulphurCycle cycle, int xStart) {
        int nextX = cycle.X.min == xStart ? cycle.X.max : cycle.X.min;
        return new PathState2(nextX, SulphurCollected + cycle.Deposits);
    }

    public int Distance(SulphurCycle cycle, int xStart) {
        return Math.Abs(X - xStart) + cycle.Perimeter - cycle.Width;
    }
}

class Grid2 : Grid, IGame<PathState2> {
    readonly StateSearch<PathState2> stateSearch;

    public Grid2() {
        stateSearch = new StateSearch<PathState2>(this);
    }

    public override int CollectSulphurAndReachEnd() {
        return stateSearch.TryFindCheapest(new PathState2(start.X), out var endState)
                ? endState.cost
                : throw new UnreachableException();
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    IEnumerable<(PathState2, int)> IGame<PathState2>.NextWithCost(PathState2 current) {
        const int required = 2026;
        if (current.SulphurCollected < required) {
            foreach (var cycle in sulphurCycles) {
                if (cycle.X.min != current.X) {
                    int distance = current.Distance(cycle, cycle.X.min);
                    yield return (current.From(cycle, cycle.X.min), distance);
                }
                if (cycle.X.max != current.X) {
                    int distance = current.Distance(cycle, cycle.X.max);
                    yield return (current.From(cycle, cycle.X.max), distance);
                }
            }
        } else {
            int end = endTargets.Last().X;
            yield return (current with { X = end }, end - current.X);
        }
    }

    bool IGame<PathState2>.HasReachedEnd(PathState2 state) => state.X == endTargets.Last().X;
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);

    public static Point[] Directions4 { get; } = [RIGHT, LEFT, UP, DOWN];

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

class StateSearch<T>(IGame<T> game, int depthMax = 100) where T : IState<T>, IEquatable<T> {
    /// DFS/Backtracking
    public bool TryFindBest(T start, out T end) {
        HashSet<T> visited = [start];
        return TryFindBest(start, visited, 0, out end);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    bool TryFindBest(T current, HashSet<T> visited, int depth, out T end) {
        end = default!;
        if (game.HasReachedEnd(current)) {
            end = current;
            return true;
        }
        if (depth > depthMax) {
            return false;
        }
        var found = false;
        foreach (var next in game.Next(current)) {
            if (!visited.Add(next)) {
                continue;
            }
            if (!TryFindBest(next, visited, depth + 1, out var nextBest)) {
                continue;
            }
            if (!found || nextBest.IsBetterThan(end)) {
                end = nextBest;
                found = true;
            }
        }
        return found;
    }

    /// Dijkstra
    public bool TryFindCheapest(T start, out (T, int cost) end) {
        end = default!;
        PriorityQueue<T, int> queue = new();
        Dictionary<T, int> visited = [];
        queue.Enqueue(start, 0);
        visited.Add(start, 0);
        while (queue.TryDequeue(out var current, out int currentCost)) {
            if (game.HasReachedEnd(current)) {
                end = (current, currentCost);
                return true;
            }
            foreach ((var next, int cost) in game.NextWithCost(current)) {
                int newCost = currentCost + cost;
                if (newCost >= visited.GetValueOrDefault(next, int.MaxValue)) {
                    continue;
                }
                queue.Enqueue(next, newCost);
                visited[next] = newCost;
            }
        }
        return false;
    }
}

interface IGame<TState> {
    IEnumerable<TState> Next(TState current) {
        throw new NotSupportedException("Implement Next");
    }

    IEnumerable<(TState, int)> NextWithCost(TState current) {
        throw new NotSupportedException("Implement NextWithCost");
    }

    bool HasReachedEnd(TState state);
}

interface IState<in T> where T : IState<T> {
    bool IsBetterThan(T other) {
        throw new NotSupportedException("Implement IsBetterThan");
    }
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";
    const int TAB_LENGTH = 2;

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

    public static void Log(object? line = null, int tab = 0, bool console = true, ConsoleColor? color = null) {
        if (!LogEnabled) {
            return;
        }
        var padding = new string(' ', tab * TAB_LENGTH);
        LogInternal($"{padding}{line}", console, color ?? ConsoleColor.Red);
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
