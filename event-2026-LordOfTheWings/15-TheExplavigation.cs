using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// https://everybody.codes/event/2026/quests/15
Test.Run((notes, part, expected) => {
    Grid grid = part switch {
        1 => new FiniteDepositGrid(),
        2 => new InfiniteDepositGrid(2026),
        _ => new InfiniteDepositGrid(202620262026)
    };
    grid.Read(notes);
    long seconds = grid.CollectSulphurAndReachEnd();
    Assert.ActualEqualsExpected(seconds, expected);
});

abstract class Grid {
    int width, height;
    protected Point start;
    readonly Dictionary<Point, bool> walkableCells = [];
    readonly HashSet<Point> sulphurDeposits = [];
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

    public abstract long CollectSulphurAndReachEnd();
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

    public long Seconds(long count = 1) => count == 0 ? 0 : count * Perimeter - Width;
}

class FiniteDepositGrid : Grid, IGame<FiniteDepositGrid.State> {
    readonly StateSearch<State> stateSearch;

    public FiniteDepositGrid() {
        stateSearch = new StateSearch<State>(this);
    }

    public override long CollectSulphurAndReachEnd() {
        var startState = new State(start.X, new bool[sulphurCycles.Count]);
        return stateSearch.TryFindBest(startState, out var endState)
                ? endState.Seconds
                : throw new UnreachableException();
    }

    IEnumerable<State> IGame<State>.Next(State current) {
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

    List<(SulphurCycle, int)> NextCycles(State state) {
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

    bool IGame<State>.HasReachedEnd(State state) => state.X == endTargets.Last().X;

    readonly record struct State(int X, bool[] CyclesVisited, int Seconds = 0) : IState<State> {
        [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
        public State From(SulphurCycle cycle, int i, int xStart) {
            int nextX = cycle.X.min == xStart ? cycle.X.max : cycle.X.min;
            int distance = Math.Abs(X - xStart) + cycle.Perimeter - cycle.Width;
            var next = new State(nextX, [.. CyclesVisited], Seconds + distance);
            next.CyclesVisited[i] = true;
            return next;
        }

        public bool IsBetterThan(State other) => Seconds < other.Seconds;

        public bool Equals(State other) {
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
}

class InfiniteDepositGrid : Grid, IGame<InfiniteDepositGrid.State> {
    readonly StateSearch<State> stateSearch;

    // todo: should be readonly
    long required;

    public InfiniteDepositGrid(long required) {
        stateSearch = new StateSearch<State>(this);
        this.required = required;
    }

    // todo: move best version inside
    public override long CollectSulphurAndReachEnd() {
        // DoTest();
        if (false) {
            return Slow().seconds;
        }
        if (false) {
            return Backtracking();
        }
        {
            return Fast();
        }
    }

    void DoTest() {
        required = 2026;
        var slow = Slow();
        long newVersion = Backtracking();
        Test.Log((slow.seconds, newVersion));
        Test.Step();

        Test.Log("Deposits: " + string.Join(", ", sulphurCycles.Select(it => it.Deposits)));
        Test.Log("Perimeter: " + string.Join(", ", sulphurCycles.Select(it => it.Perimeter)));
        Test.Log("gain: "
                + string.Join(", ", sulphurCycles.Select(it => (it.Deposits / (float)it.Perimeter).ToString("F"))));
        Test.Log("zone: "
                + string.Join(", ", sulphurCycles
                        .Select(it => it.X.max < endTargets[0].X ? 0 : it.X.max < endTargets[1].X ? 1 : 2)));
        for (var i = 0; i < 10; i++) {
            required = 2026 + i;
            var result = Slow();
            Test.Log((required, string.Join(", ", result.cyclesVisited), result.seconds));
        }
        Test.Step();
    }

    (long[] cyclesVisited, long seconds) Slow() {
        return stateSearch.TryFindCheapest(new State(start.X, new long[sulphurCycles.Count]), out var endState)
                ? (endState.Item1.CyclesVisited, endState.cost)
                : throw new UnreachableException();
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    long Backtracking() {
        required = 2026;
        return Find(start.X, 0, 0);

        long Find(int x, long sulphurCollected, long seconds) {
            // Test.Log($"{sulphurCollected}, {seconds}");
            if (sulphurCollected >= required) {
                return seconds + endTargets.Last().X - x;
            }
            foreach (var cycle in sulphurCycles) {
                if (cycle.X.min != x) {
                    long newSeconds = seconds + Math.Abs(x - cycle.X.min) + cycle.Seconds();
                    return Find(cycle.X.max, sulphurCollected + cycle.Deposits, newSeconds);
                }
                if (cycle.X.max != x) {
                    long newSeconds = seconds + Math.Abs(x - cycle.X.max) + cycle.Seconds();
                    return Find(cycle.X.min, sulphurCollected + cycle.Deposits, newSeconds);
                }
            }
            return 0;
        }
    }

    long Fast() {
        int end = endTargets.Last().X;
        var bestCycles = sulphurCycles.Select((cycle, i) => (cycle, i))
                .OrderByDescending(it => it.cycle.Deposits / (float)it.cycle.Perimeter)
                .Take(7)
                .ToList();
        /* max and min is xMax and xMin
- so one normal cycle is start at xMin, p-w, finish at xMax, so count*p-w  b1s1  s2b2
- unoptimized 2 rect cycles: 2b+1s, b is (4x2), s is (2x1)                 0 1 2 3 4
- 2*b.p-b.w + b.max-s.min + s.p-s.w + b.max-s.max = 2*12-4 + 4-1 + 6-2 + 4-3 = 28
- optimized 2 rect cycles: 2b+1s
- b.p-b.w + b.max-s.max + s.p-s.w + s.min-b.min + b.p-b.w = 2*(b.p-b.w) + b.max-s.max + s.p-s.w + s.min-b.min
- => 2*(12-4) + 4-3 + 6-2 + 1-0 = 22, diff is 6, so 2*(b.max-s.min)
- now let's check 2b+2s
- unoptimized: 2*b.p-b.w + b.max-s.min + 2*s.p-s.w + b.max-s.max = 2*12-4 + 4-1 + 2*6-2 + 4-3 = 34
- optimized: hm, i could either do 2 s circles right away, or split them
- 1: b.p-b.w + b.max-s.max + s.p-s.w + s.min-b.min + b.p-b.w + b.max-s.min + s.p-s.w + b.max-s.max
- => 2*(b.p-b.w) + 2*(b.max-s.max) + 2*(s.p-s.w) + s.min-b.min + b.max-s.min = 2*(12-4) + 2*(4-3) + 2*(6-2) + 1-0 + 4-1 = 30
- 2: b.p-b.w + b.max-s.max + 2*s.p-s.w + s.min-b.min + b.p-b.w = 2*(b.p-b.w) + b.max-s.max + 2*s.p-s.w + s.min-b.min
- => 2*(12-4) + 4-3 + 2*6-2 + 1-0 = 28, diff is 6 again
- so it seems that diff is always const
         */
        (var main, int mainI) = bestCycles.First();
        bestCycles.RemoveAt(0);
        var mainCount = (long)Math.Ceiling(required / (double)main.Deposits);
        long seconds = main.Seconds(mainCount) + end;
        List<(SulphurCycle value, long count)> visited = [(main, mainCount)];
        Collect(end, ref seconds, visited);
        const long maxCount = 2;
        for (var _ = 0; _ <= bestCycles.Sum(it => it.cycle.Deposits) / main.Deposits * maxCount; _++) {
            mainCount--;
            visited.Clear();
            visited.Add((main, mainCount));
            Visit(0);
        }
        return seconds;

        void Visit(int i) {
            if (i == mainI) {
                i++;
            }
            if (i >= bestCycles.Count) {
                Collect(end, ref seconds, visited);
                return;
            }
            for (var count = 0; count <= maxCount; count++) {
                visited.Add((bestCycles[i].cycle, count));
                Visit(i + 1);
                visited.RemoveAt(visited.Count - 1);
            }
        }
    }

    void Collect(int end, ref long seconds, List<(SulphurCycle value, long count)> cycles) {
        long collected = cycles.Sum(it => it.value.Deposits * it.count);
        if (collected != required) {
            return;
        }
        long newSeconds = 0;
        var x = 0;
        long previousCount = -1;
        foreach ((var cycle, long count) in cycles.Where(it => it.count > 0).OrderBy(it => it.value.X.min)) {
            newSeconds += Math.Abs(x - cycle.X.min) + cycle.Seconds(count);
            if (previousCount == 2 && cycle.X.min < x) {
                newSeconds -= 2 * Math.Abs(cycle.X.min - x);
            }
            x = cycle.X.max;
            previousCount = count;
        }
        newSeconds += end - x;
        seconds = Math.Min(newSeconds, seconds);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    IEnumerable<(State, long)> IGame<State>.NextWithCost(State current) {
        if (current.SulphurCollected < required) {
            for (var i = 0; i < sulphurCycles.Count; i++) {
                var cycle = sulphurCycles[i];
                if (cycle.X.min != current.X) {
                    long distance = current.Distance(cycle, cycle.X.min);
                    yield return (current.From(cycle, i, cycle.X.min), distance);
                }
                if (cycle.X.max != current.X) {
                    long distance = current.Distance(cycle, cycle.X.max);
                    yield return (current.From(cycle, i, cycle.X.max), distance);
                }
            }
        } else {
            int end = endTargets.Last().X;
            yield return (current with { X = end }, end - current.X);
        }
    }

    bool IGame<State>.HasReachedEnd(State state) => state.X == endTargets.Last().X;

    // todo: CyclesVisited is debug, remove custom equals too
    readonly record struct State(int X, long[] CyclesVisited, int SulphurCollected = 0) : IState<State> {
        public State From(SulphurCycle cycle, int i, int xStart) {
            int nextX = cycle.X.min == xStart ? cycle.X.max : cycle.X.min;
            var next = new State(nextX, [.. CyclesVisited], SulphurCollected + cycle.Deposits);
            next.CyclesVisited[i]++;
            return next;
        }

        public long Distance(SulphurCycle cycle, int xStart) {
            return Math.Abs(X - xStart) + cycle.Seconds();
        }

        public bool Equals(State other) => X == other.X && SulphurCollected == other.SulphurCollected;
        public override int GetHashCode() => HashCode.Combine(X, SulphurCollected);
    }
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
    public bool TryFindCheapest(T start, out (T, long cost) end) {
        end = default!;
        PriorityQueue<T, long> queue = new();
        Dictionary<T, long> visited = [];
        queue.Enqueue(start, 0);
        visited.Add(start, 0);
        while (queue.TryDequeue(out var current, out long currentCost)) {
            if (game.HasReachedEnd(current)) {
                end = (current, currentCost);
                return true;
            }
            foreach ((var next, long cost) in game.NextWithCost(current)) {
                long newCost = currentCost + cost;
                if (newCost >= visited.GetValueOrDefault(next, long.MaxValue)) {
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

    IEnumerable<(TState, long)> NextWithCost(TState current) {
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
