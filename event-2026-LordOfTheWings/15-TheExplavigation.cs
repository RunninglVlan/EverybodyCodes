using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// Test.From = "Part II Example";
Test.From = "Part II";
// Test.Until = "Part II";
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
    protected readonly List<Point> sulphurCells = [];
    protected readonly List<(HashSet<Point> path, int sulphur)> cyclesWithSulphur = [];
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
                        sulphurCells.Add(point);
                        break;
                }
            }
        }
        FindCycles();
        FindEndTargets();
    }

    void FindCycles() {
        foreach (var cell in sulphurCells.Where(cell => !cyclesWithSulphur.Any(it => it.path.Contains(cell)))) {
            foreach (var direction in Point.Directions4) {
                var next = cell + direction;
                if (!walkableCells[next]) {
                    continue;
                }
                var startMove = new Move(next, direction);
                var path = Pathfinding<Move>.Shortest(startMove, new Move(cell), NextMove).First();
                cyclesWithSulphur.Add((
                        [.. path.Select(it => it.Point)], path.Count(it => sulphurCells.Contains(it.Point))
                ));
                break;
            }
        }
    }

    protected IEnumerable<Move> NextMove(Move current) => NextMove(current, width - 1);

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

    protected IEnumerable<Move> NextMove(Move current, int xMax) {
        var directions = new[] {
            current.Direction, Point.TurnLeft(current.Direction), Point.TurnRight(current.Direction)
        };
        foreach (var direction in directions) {
            var nextPoint = current.Point + direction;
            if (nextPoint.X < 0 || nextPoint.X > xMax) {
                continue;
            }
            var next = new Move(nextPoint, direction);
            if (walkableCells[nextPoint]) {
                yield return next;
            }
        }
    }
}

record PathState1(Move Move, int[] TargetsReached) : IState<PathState1> {
    public int EndTargets { get; set; }
    public int Seconds => TargetsReached.Sum();
    public bool ReachedEnd => EndTargets == 0 && TargetsReached.Last() != 0;

    public bool SulphurCollected(int i) => TargetsReached[i] != 0;

    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public PathState1 From(Point point, List<Move> path, int i) {
        var direction = path[^1].Point - path[^2].Point;
        var next = new PathState1(new Move(point, direction), [.. TargetsReached]);
        next.TargetsReached[i] = path.Count - 1;
        next.EndTargets = EndTargets;
        return next;
    }

    public bool IsBetterThan(PathState1 other) => Seconds < other.Seconds;

    public virtual bool Equals(PathState1? other) {
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

class Grid1 : Grid, IGame<PathState1> {
    readonly StateSearch<PathState1> stateSearch;

    public Grid1() {
        stateSearch = new StateSearch<PathState1>(this, 100);
    }

    public override int CollectSulphurAndReachEnd() {
        var startMove = new Move(start, Point.RIGHT);
        var startState = new PathState1(startMove, new int[sulphurCells.Count + endTargets.Count]) {
            EndTargets = endTargets.Count
        };
        var endState = stateSearch.FindBest(startState)!;
        return endState.Seconds;
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    IEnumerable<PathState1> IGame<PathState1>.Next(PathState1 current) {
        Func<Move, IEnumerable<Move>> nextMove = it => NextMove(current, it);
        var sulphurCollected = true;
        int endTargetIndex = endTargets.Count - current.EndTargets;
        var endTarget = endTargets[endTargetIndex];
        for (var i = 0; i < sulphurCells.Count; i++) {
            var sulphur = sulphurCells[i];
            if (sulphur.X > endTarget.X || current.SulphurCollected(i)) {
                continue;
            }
            var paths = Pathfinding<Move>.Shortest(current.Move, new Move(sulphur), nextMove, 2);
            if (paths.Count == 0) {
                continue;
            }
            foreach (var path in paths) {
                yield return current.From(sulphur, path, i);
            }
            sulphurCollected = false;
        }
        if (sulphurCollected) {
            var path = Pathfinding<Move>.Shortest(current.Move, new Move(endTarget), nextMove).First();
            var next = current.From(endTarget, path, sulphurCells.Count + endTargetIndex);
            next.EndTargets--;
            yield return next;
        }
    }

    IEnumerable<Move> NextMove(PathState1 state, Move current) {
        var remainingSulphurCells = sulphurCells.Where((_, i) => state.TargetsReached[i] == 0);
        if (remainingSulphurCells.Contains(current.Point)) {
            yield break;
        }
        var endTarget = endTargets[^state.EndTargets];
        foreach (var move in NextMove(current, endTarget.X)) {
            yield return move;
        }
    }

    bool IGame<PathState1>.HasReachedEnd(PathState1 state) => state.ReachedEnd;
}

record PathState2(Move Move, int[] TargetsReached) : IState<PathState2> {
    public int EndTargets { get; set; }
    public int Seconds => TargetsReached.Sum();
    public bool ReachedEnd => EndTargets == 0 && TargetsReached.Last() != 0;

    public bool SulphurCollected(int i) => TargetsReached[i] != 0;

    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    public PathState2 From(Point point, List<Move> path, int i) {
        var direction = path[^1].Point - path[^2].Point;
        var next = new PathState2(new Move(point, direction), [.. TargetsReached]);
        next.TargetsReached[i] = path.Count - 1;
        next.EndTargets = EndTargets;
        return next;
    }

    public bool IsBetterThan(PathState2 other) => Seconds < other.Seconds;

    public virtual bool Equals(PathState2? other) {
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

class Grid2 : Grid, IGame<PathState2> {
    readonly StateSearch<PathState2> stateSearch;

    public Grid2() {
        stateSearch = new StateSearch<PathState2>(this, 100);
    }

    public override int CollectSulphurAndReachEnd() {
        const int required = 2026;
        foreach ((var path, int sulphur) in cyclesWithSulphur) {
            Test.Log((path.Count, sulphur, (float)path.Count / sulphur, sulphur / (float)path.Count));
        }
        var cycle = cyclesWithSulphur.MaxBy(it => it.sulphur / (float)it.path.Count);
        var cyclesToCollect = (int)Math.Ceiling(required / (float)cycle.sulphur);
        int seconds = cyclesToCollect * cycle.path.Count;
        Test.Log((cycle.path.Count, cycle.sulphur, cyclesToCollect, seconds));
        // reach cycle
        var startMove = new Move(start, Point.RIGHT);
        var end = new Move(cycle.path.First());
        var pathToCycle = Pathfinding<Move>.Shortest(startMove, end, NextMove).First();
        // remove overlapping cells
        for (int i = pathToCycle.Count - 1; i >= 0; i--) {
            var move = pathToCycle[i];
            if (cycle.path.Contains(move.Point)) {
                pathToCycle.RemoveAt(i);
            }
        }
        // todo: reach end from the cycle using state search
        startMove = end with { Direction = Point.RIGHT };
        end = new Move(endTargets.Last());
        var pathToEnd = Pathfinding<Move>.Shortest(startMove, end, NextMove).First();
        // remove overlapping cells
        for (int i = pathToEnd.Count - 1; i >= 0; i--) {
            var move = pathToEnd[i];
            if (cycle.path.Contains(move.Point)) {
                pathToEnd.RemoveAt(i);
            }
        }
        return pathToCycle.Count + seconds + pathToEnd.Count;
    }

    public IEnumerable<PathState2> Next(PathState2 current) {
        throw new NotImplementedException();
    }

    public bool HasReachedEnd(PathState2 state) {
        throw new NotImplementedException();
    }
}

readonly record struct Move(Point Point, Point Direction = default) {
    public bool Equals(Move other) => Point == other.Point;
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
    public static List<List<T>> Shortest(T start, T end, Func<T, IEnumerable<T>> getNext, int max = 1) {
        List<List<T>> paths = [];
        Dictionary<T, T> parents = [];
        Queue<T> queue = [];
        HashSet<T> visited = [];
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.TryDequeue(out var current)) {
            if (current.Equals(end)) {
                paths.Add(Reconstruct(parents, start, current));
                if (paths.Count == max) {
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
