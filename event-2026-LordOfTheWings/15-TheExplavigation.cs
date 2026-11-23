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
    protected Point End => endTargets.Last();
    readonly Dictionary<Point, bool> walkableCells = [];
    readonly HashSet<Point> sulphurDeposits = [];
    protected readonly List<SulphurRect> sulphurRects = [];
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
        FindRects();
        FindEndTargets();
    }

    void FindRects() {
        List<HashSet<Point>> rectPaths = [];
        foreach (var cell in sulphurDeposits.Where(cell => !rectPaths.Any(it => it.Contains(cell)))) {
            foreach (var direction in Point.Directions4) {
                var next = cell + direction;
                if (!walkableCells[next]) {
                    continue;
                }
                var startMove = new Move(next, direction);
                var path = Pathfinding<Move>.Shortest(startMove, new Move(cell), NextMove)!
                        .Select(it => it.Point)
                        .ToHashSet();
                rectPaths.Add(path);
                HashSet<Point> rectDeposits = [.. path.Where(it => sulphurDeposits.Contains(it))];
                sulphurRects.Add(new SulphurRect(path, rectDeposits));
                break;
            }
        }
    }

    IEnumerable<Move> NextMove(Move current) {
        var directions = new[] {
            current.Direction, Screen.TurnLeft(current.Direction), Screen.TurnRight(current.Direction)
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
            if (endTargets.Count > 0 && End.X == pass.X - 1) {
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

readonly record struct SulphurRect {
    public int Deposits { get; }
    public int Min { get; }
    public int Max { get; }
    public int Perimeter { get; }
    public int Width { get; }
    public int Height { get; }

    public SulphurRect(HashSet<Point> path, HashSet<Point> deposits) {
        Deposits = deposits.Count;
        Min = path.Min(it => it.X);
        Max = path.Max(it => it.X);
        Width = Max - Min;
        Height = path.Max(it => it.Y) - path.Min(it => it.Y);
        Perimeter = 2 * (Height + Width);
    }

    public long HalfSeconds(long cycles = 1) => cycles == 0 ? 0 : cycles * Perimeter - Width;
    public long WholeSeconds(long cycles = 1) => cycles == 0 ? 0 : cycles * Perimeter - cycles * Width;
}

class FiniteDepositGrid : Grid, IGame<FiniteDepositGrid.State> {
    readonly StateSearch<State> stateSearch;

    public FiniteDepositGrid() {
        stateSearch = new StateSearch<State>(this);
    }

    public override long CollectSulphurAndReachEnd() {
        var startState = new State(start.X, new bool[sulphurRects.Count]);
        return stateSearch.TryFind(startState, out var endState)
                ? endState.Seconds
                : throw new UnreachableException();
    }

    [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
    IEnumerable<State> IGame<State>.Next(State current) {
        var nextRects = NextRects(current);
        if (nextRects.Count > 0) {
            foreach ((var rect, int i) in nextRects) {
                yield return NextState(rect, i, rect.Min);
                yield return NextState(rect, i, rect.Max);
            }
        } else {
            int nextX = End.X;
            int distance = nextX - current.X;
            yield return current with { X = nextX, Seconds = current.Seconds + distance };
        }
        yield break;

        State NextState(SulphurRect rect, int i, int x) {
            int nextX = rect.Min == x ? rect.Max : rect.Min;
            int distance = Math.Abs(current.X - x) + rect.Perimeter - rect.Width;
            var next = new State(nextX, [.. current.RectsVisited], current.Seconds + distance);
            next.RectsVisited[i] = true;
            return next;
        }
    }

    List<(SulphurRect, int)> NextRects(State state) {
        List<(SulphurRect, int)> rects = [];
        foreach (var endTarget in endTargets) {
            for (var i = 0; i < sulphurRects.Count; i++) {
                var rect = sulphurRects[i];
                if (state.RectsVisited[i] || rect.Max > endTarget.X) {
                    continue;
                }
                rects.Add((rect, i));
            }
            if (rects.Count > 0) {
                return rects;
            }
        }
        return rects;
    }

    bool IGoal<State>.HasReachedEnd(State state) => state.X == End.X;

    readonly record struct State(int X, bool[] RectsVisited, int Seconds = 0) : IState<State> {
        public bool IsBetterThan(State other) => Seconds < other.Seconds;

        public bool Equals(State other) {
            return X == other.X && RectsVisited.SequenceEqual(other.RectsVisited) && Seconds == other.Seconds;
        }

        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(X);
            foreach (bool item in RectsVisited) {
                hash.Add(item);
            }
            hash.Add(Seconds);
            return hash.ToHashCode();
        }
    }
}

class InfiniteDepositGrid : Grid, IGameWithCost<InfiniteDepositGrid.State> {
    readonly StateSearchWithCost<State> stateSearch;

    readonly long required;
    long currentRequired;

    public InfiniteDepositGrid(long required) {
        stateSearch = new StateSearchWithCost<State>(this);
        this.required = required;
    }

    public override long CollectSulphurAndReachEnd() {
        const long maxCycles = 3;
        long[] mask = FindRectMask(220, 224);
        var visit = sulphurRects.Select((rect, i) => (rect, i))
                .Where(it => mask[it.i] > 0 && mask[it.i] < maxCycles).ToList();
        var best = sulphurRects.Select((rect, i) => (rect, i)).Where(it => mask[it.i] > maxCycles).ToList();
        var bestCount = (long)Math.Ceiling(required / (double)best.Sum(it => it.rect.Deposits));
        long[] bestCycles = [.. best.Select(_ => bestCount)];
        var seconds = long.MaxValue;
        List<(SulphurRect value, long cycles)> visited = [];
        long max = visit.Sum(it => it.rect.Deposits) / best.Sum(it => it.rect.Deposits) * maxCycles;
        for (var _ = 0; _ <= max; _++) {
            for (var j = 0; j < best.Count; j++) {
                visited.Clear();
                visited.AddRange(best.Select((tuple, i) => (tuple.rect, bestCycles[i])));
                Visit(0);
                bestCycles[j]--;
            }
        }
        return seconds;

        // Backtracking
        void Visit(int i) {
            if (i >= visit.Count) {
                long collected = visited.Sum(it => it.value.Deposits * it.cycles);
                if (collected == required) {
                    seconds = Math.Min(VisitRects(visited), seconds);
                }
                return;
            }
            var tuple = visit[i];
            for (var cycles = 0; cycles <= maxCycles; cycles++) {
                visited.Add((tuple.rect, cycles));
                Visit(i + 1);
                visited.RemoveAt(visited.Count - 1);
            }
        }
    }

    /// Runs correct search several times to find potential rects to visit in optimized search.
    /// Can't use it when required sulphur is too big, as it becomes too slow
    long[] FindRectMask(int rangeMin, int rangeMax) {
        var mask = new long[sulphurRects.Count];
        for (int i = rangeMin; i < rangeMax; i++) {
            currentRequired = i;
            long[] rectsVisited;
            var startState = new State(start.X, -1, new long[sulphurRects.Count]);
            if (stateSearch.TryFind(startState, out var endState)) {
                rectsVisited = endState.value.RectsVisited;
            } else {
                throw new UnreachableException();
            }
            for (var j = 0; j < rectsVisited.Length; j++) {
                mask[j] = Math.Max(rectsVisited[j], mask[j]);
            }
        }
        return mask;
    }

    long VisitRects(List<(SulphurRect value, long cycles)> rects) {
        long seconds = 0;
        var x = 0;
        (long cycles, bool whole) previous = (-1, false);
        var visited = rects.Where(it => it.cycles > 0).OrderBy(it => it.value.Min).ThenBy(it => it.cycles)
                .Select(it => (it.value, it.cycles))
                .ToArray();
        for (var i = 0; i < visited.Length; i++) {
            var rect = visited[i].value;
            (int start, int end) way = IsInner(rect) ? (rect.Max, rect.Min) : (rect.Min, rect.Max);
            if (IsWhole(i)) {
                VisitWhole(i, way);
                i++;
            } else {
                VisitHalf(i, way);
            }
        }
        return seconds + End.X - x;

        bool IsInner(SulphurRect rect) => rect.Min < x;

        bool IsWhole(int i) {
            if (i + 1 >= visited.Length) {
                return false;
            }
            (var rect, long cycles) = visited[i];
            (var rect2, long cycles2) = visited[i + 1];
            return rect.Min == rect2.Min && cycles2 - cycles <= 2;
        }

        void VisitWhole(int i, (int start, int end) way) {
            (var rect, long cycles) = visited[i];
            (var rect2, long cycles2) = visited[i + 1];
            if (cycles2 - cycles == 2) {
                cycles2--;
                seconds += rect2.Perimeter;
            }
            seconds += Math.Abs(x - way.start) + rect.WholeSeconds(cycles) + rect2.WholeSeconds(cycles2);
            if (IsInner(rect) && previous is { cycles: > 1, whole: false }) {
                seconds -= cycles == cycles2 ? 2 * Math.Abs(rect.Max - x) : 2 * Math.Abs(rect.Min - x);
                seconds += Math.Abs(way.end - x);
            } else {
                x = cycles == cycles2 ? way.start : way.end;
            }
            previous = (visited[i + 1].cycles, true);
        }

        void VisitHalf(int i, (int start, int end) way) {
            (var rect, long cycles) = visited[i];
            if (IsInner(rect) && cycles == 1 && previous.whole && visited[i - 1].cycles - visited[i - 2].cycles == 2) {
                seconds += 2 * rect.Height;
            } else if (IsInner(rect) && previous is { cycles: > 1, whole: false }) {
                seconds += Math.Abs(x - rect.Min) + rect.HalfSeconds(cycles);
                seconds -= 2 * Math.Abs(rect.Min - x);
                x = rect.Max;
            } else {
                seconds += Math.Abs(x - way.start) + rect.HalfSeconds(cycles);
                x = way.end;
            }
            previous = (cycles, false);
        }
    }

    IEnumerable<(State, long)> IGameWithCost<State>.NextWithCost(State current) {
        if (current.SulphurCollected != currentRequired) {
            for (var i = 0; i < sulphurRects.Count; i++) {
                var rect = sulphurRects[i];
                (int xStart, int xEnd) = (rect.Min, rect.Max);
                if (xStart != current.X || i != current.I) {
                    yield return Next(xStart, xEnd, i);
                }
                (xStart, xEnd) = (rect.Max, rect.Min);
                if (xStart != current.X || i != current.I) {
                    yield return Next(xStart, xEnd, i);
                }
            }
        } else {
            int end = End.X;
            yield return (current with { X = end }, end - current.X);
        }
        yield break;

        (State, long) Next(int xStart, int xEnd, int i) {
            var rect = sulphurRects[i];
            var next = new State(xEnd, i, [.. current.RectsVisited], current.SulphurCollected + rect.Deposits);
            next.RectsVisited[i]++;
            long distance = Math.Abs(current.X - xStart) + rect.HalfSeconds();
            return (next, distance);
        }
    }

    bool IGoal<State>.HasReachedEnd(State state) => state.X == End.X;

    readonly record struct State(int X, int I, long[] RectsVisited, int SulphurCollected = 0) {
        public bool Equals(State other) => X == other.X && I == other.I && SulphurCollected == other.SulphurCollected;
        public override int GetHashCode() => HashCode.Combine(X, I, SulphurCollected);
    }
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    static readonly Point RIGHT = new(1, 0);

    public static Point[] Directions4 { get; } = [RIGHT, LEFT, UP, DOWN];

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}

static class Screen {
    public static Point TurnLeft(Point direction) => new(direction.Y, -direction.X);
    public static Point TurnRight(Point direction) => new(-direction.Y, direction.X);
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
    readonly HashSet<T> visited = [];

    /// DFS/Backtracking
    public bool TryFind(T start, out T end) {
        visited.Clear();
        visited.Add(start);
        return TryFind(start, 0, out end);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    bool TryFind(T current, int depth, out T end) {
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
            if (!TryFind(next, depth + 1, out var nextBest)) {
                continue;
            }
            if (!found || nextBest.IsBetterThan(end)) {
                end = nextBest;
                found = true;
            }
        }
        return found;
    }
}

class StateSearchWithCost<T>(IGameWithCost<T> game) where T : IEquatable<T> {
    readonly PriorityQueue<T, long> queue = new();
    readonly Dictionary<T, long> visited = [];

    /// Dijkstra
    public bool TryFind(T start, out (T value, long cost) end) {
        end = default!;
        queue.Clear();
        queue.Enqueue(start, 0);
        visited.Clear();
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

interface IGoal<in TState> {
    bool HasReachedEnd(TState state);
}

interface IGame<TState> : IGoal<TState> {
    IEnumerable<TState> Next(TState current);
}

interface IGameWithCost<TState> : IGoal<TState> {
    IEnumerable<(TState, long cost)> NextWithCost(TState current);
}

interface IState<in T> where T : IState<T> {
    bool IsBetterThan(T other);
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
