using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

Test.From = "Part III";
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

    public override string ToString() {
        return $"Deposits: {Deposits}, Min: {Min}, Max: {Max}, Width: {Width}, Height: {Height}";
    }
}

class FiniteDepositGrid : Grid, IGame<FiniteDepositGrid.State> {
    readonly StateSearch<State> stateSearch;

    public FiniteDepositGrid() {
        stateSearch = new StateSearch<State>(this);
    }

    public override long CollectSulphurAndReachEnd() {
        var startState = new State(start.X, new bool[sulphurRects.Count]);
        return stateSearch.TryFindBest(startState, out var endState)
                ? endState.Seconds
                : throw new UnreachableException();
    }

    IEnumerable<State> IGame<State>.Next(State current) {
        var nextRects = NextRects(current);
        if (nextRects.Count > 0) {
            foreach ((var rect, int i) in nextRects) {
                yield return current.From(rect, i, rect.Min);
                yield return current.From(rect, i, rect.Max);
            }
        } else {
            int nextX = endTargets.Last().X;
            int distance = nextX - current.X;
            yield return current with { X = nextX, Seconds = current.Seconds + distance };
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

    bool IGame<State>.HasReachedEnd(State state) => state.X == endTargets.Last().X;

    readonly record struct State(int X, bool[] RectsVisited, int Seconds = 0) : IState<State> {
        [SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
        public State From(SulphurRect rect, int i, int x) {
            int nextX = rect.Min == x ? rect.Max : rect.Min;
            int distance = Math.Abs(X - x) + rect.Perimeter - rect.Width;
            var next = new State(nextX, [.. RectsVisited], Seconds + distance);
            next.RectsVisited[i] = true;
            return next;
        }

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

class InfiniteDepositGrid : Grid, IGame<InfiniteDepositGrid.State> {
    readonly StateSearch<State> stateSearch;

    // todo: should be readonly
    long required;

    public InfiniteDepositGrid(long required) {
        stateSearch = new StateSearch<State>(this);
        this.required = required;
    }

    // todo: move best version inside, remove other versions
    public override long CollectSulphurAndReachEnd() {
        long[] best = FindBestRects();
        // DoTest();
        if (false) {
            var slow = Slow();
            int slowVisited = slow.rectsVisited.Count(it => it > 0);
            string rectsVisited = string.Join(", ", slow.rectsVisited.Select(it => it.ToString().PadLeft(2)));
            Test.Log(("slow\n" + rectsVisited, $"seconds: {slow.seconds}, visited: {slowVisited}"), 1);
            return slow.seconds;
        }
        if (true) {
            var fast = Fast(best);
            int fastVisited = fast.rectsVisited.Count(it => it > 0);
            string rectsVisited = string.Join(", ", fast.rectsVisited.Select(it => it.ToString().PadLeft(2)));
            Test.Log(("fast\n" + rectsVisited, $"seconds: {fast.seconds}, visited: {fastVisited}"), 1);
            return fast.seconds;
        }
    }

    long[] FindBestRects() {
        var visit = new long[sulphurRects.Count];
        // i don't really know when to start
        // i guess we'll see
        var begin = 220;
        long requiredOriginal = required;
        for (var i = 0; i < 10; i++) {
            required = begin + i;
            var slow = Slow();
            // Test.Log($"required: {required}");
            int slowVisited = slow.rectsVisited.Count(it => it > 0);
            string rectsVisited = string.Join(", ", slow.rectsVisited.Select(it => it.ToString().PadLeft(2)));
            // Test.Log(("slow\n" + rectsVisited, $"seconds: {slow.seconds}, visited: {slowVisited}"), 1);
            long[] previous = [.. visit];
            for (var j = 0; j < slow.rectsVisited.Length; j++) {
                visit[j] = Math.Max(slow.rectsVisited[j], visit[j]);
            }
            if (!previous.SequenceEqual(visit)) {
                string visited = string.Join(", ", visit.Select(it => it == 0 ? " _" : it.ToString().PadLeft(2)));
                // Test.Log(($"{required}\n" + visited, slow.seconds));
            }
        }
        required = requiredOriginal;
        // Test.Step();
        return visit;
    }

    void DoTest() {
        Test.Log("Deposits:\n" + string.Join(", ", sulphurRects.Select(it => it.Deposits.ToString().PadLeft(2))));
        Test.Log("Perimeter:\n" + string.Join(", ", sulphurRects.Select(it => it.Perimeter.ToString().PadLeft(2))));
        Test.Log("gain:\n"
                + string.Join(", ",
                        sulphurRects.Select(it => (it.Deposits / (float)it.Perimeter).ToString("F")[2..])));
        Test.Log("zone:\n"
                + string.Join(", ", sulphurRects
                        .Select(it => it.Max < endTargets[0].X ? " 0" : it.Max < endTargets[1].X ? " 1" : " 2")));
        var visit = new long[sulphurRects.Count];
        var maxVisited = 0;
        const long increment = 1_000;
        const long begin = 2026;
        for (var i = 0; i < 10_000; i++) {
            (long min, long max) = (begin + i * increment, begin + (i + 1) * increment);
            required = Random.Shared.NextInt64(min, max);
            required = 12624;
            var stopwatch = Stopwatch.StartNew();
            var slow = Slow();
            long slowElapsed = stopwatch.ElapsedMilliseconds;
            stopwatch = Stopwatch.StartNew();
            var fast = Fast(new long[sulphurRects.Count]);
            long fastElapsed = stopwatch.ElapsedMilliseconds;
            Test.Log($"required [{min}-{max}): {required}, slow: {slowElapsed}, fast: {fastElapsed}");
            if (slow.seconds == fast.seconds) {
                continue;
            }
            int slowVisited = slow.rectsVisited.Count(it => it > 0);
            Test.Log(("slow\n" + string.Join(", ", slow.rectsVisited.Select(it => it.ToString().PadLeft(2))),
                    $"seconds: {slow.seconds}, visited: {slowVisited}"), 1);
            Test.Log($"difference: {slow.seconds - fast.seconds}");
            int fastVisited = fast.rectsVisited.Count(it => it > 0);
            Test.Log(("fast\n" + string.Join(", ", fast.rectsVisited.Select(it => it.ToString().PadLeft(2))),
                    $"seconds: {fast.seconds}, visited: {fastVisited}"), 1);
            long[] previous = [.. visit];
            for (var j = 0; j < slow.rectsVisited.Length; j++) {
                visit[j] = Math.Max(slow.rectsVisited[j], visit[j]);
            }
            maxVisited = Math.Max(maxVisited, slow.rectsVisited.Count(it => it > 0));
            if (!previous.SequenceEqual(visit)) {
                Test.Log((
                        $"{required}\n"
                        + string.Join(", ", visit.Select(it => it == 0 ? " _" : it.ToString().PadLeft(2))),
                        slow.seconds, maxVisited));
            }
            Test.Step();
        }
        // Test.Step();
    }

    (long[] rectsVisited, long seconds) Slow() {
        return stateSearch.TryFindCheapest(new State(start.X, -1, new long[sulphurRects.Count]), out var endState)
                ? (endState.Item1.RectsVisited, endState.cost)
                : throw new UnreachableException();
    }

    (long[] rectsVisited, long seconds) Fast(long[] best) {
        int end = endTargets.Last().X;
        // bool[] visit = [
        //     .. " _,  _,  1,  _,  1,  _,  1,  1,  _,  1,  _,  _,  1,  _,  _,  _,  1,  1,  1,  1,  1,  _,  _,  _,  _,  _,  _,  _,  _,  1"
        //             .Split(", ").Select(it => it == " 1")
        // ];
        const long maxCycles = 3;
        var bestRects = sulphurRects.Select((rect, i) => (rect, i))
                .Where(it => best[it.i] > 0 && best[it.i] < maxCycles).ToList();
        var mainRects = sulphurRects.Select((rect, i) => (rect, i)).Where(it => best[it.i] > maxCycles).ToList();
        var mainCount = (long)Math.Ceiling(required / (double)mainRects.Select(it => it.rect.Deposits).Sum());
        long[] mainCycles = [mainCount, mainCount];
        var seconds = long.MaxValue;
        List<(SulphurRect value, int i, long cycles)> visited =
                [.. mainRects.Select((tuple, i) => (tuple.rect, tuple.i, mainCycles[i]))];
        var rectsVisited = new long[sulphurRects.Count];
        Collect(end, ref rectsVisited, ref seconds, visited);
        long max = bestRects.Sum(it => it.rect.Deposits) / mainRects.Sum(it => it.rect.Deposits) * maxCycles;
        for (var _ = 0; _ <= max; _++) {
            mainCycles[0]--;
            visited.Clear();
            visited.AddRange(mainRects.Select((tuple, i) => (tuple.rect, tuple.i, mainCycles[i])));
            Visit(0);
            mainCycles[0]++;
            mainCycles[1]--;
            visited.Clear();
            visited.AddRange(mainRects.Select((tuple, i) => (tuple.rect, tuple.i, mainCycles[i])));
            Visit(0);
            mainCycles[0]--;
            visited.Clear();
            visited.AddRange(mainRects.Select((tuple, i) => (tuple.rect, tuple.i, mainCycles[i])));
            Visit(0);
        }
        return (rectsVisited, seconds);

        void Visit(int i) {
            if (i >= bestRects.Count) {
                Collect(end, ref rectsVisited, ref seconds, visited);
                return;
            }
            for (var cycles = 0; cycles <= maxCycles; cycles++) {
                var tuple = bestRects[i];
                visited.Add((tuple.rect, tuple.i, cycles));
                Visit(i + 1);
                visited.RemoveAt(visited.Count - 1);
            }
        }
    }

    void Collect(int end, ref long[] rectsVisited, ref long seconds,
            List<(SulphurRect value, int i, long cycles)> rects) {
        long collected = rects.Sum(it => it.value.Deposits * it.cycles);
        if (collected != required) {
            return;
        }
        long newSeconds = 0;
        var x = 0;
        (SulphurRect rect, long cycles, bool whole) previous = (default, -1, false);
        var log = new StringBuilder();
        log.AppendLine($"new, required: {required}");
        var j = 1;
        var visited = rects.Where(it => it.cycles > 0).OrderBy(it => it.value.Min).ThenBy(it => it.cycles)
                .Select(it => (it.value, it.cycles))
                .ToArray();
        for (var i = 0; i < visited.Length; i++) {
            (var rect, long cycles) = visited[i];
            if (i + 1 < visited.Length && rect.Min == visited[i + 1].value.Min) {
                (var rect2, long cycles2) = visited[i + 1];
                if (cycles2 - cycles == 2) {
                    cycles2--;
                    newSeconds += rect2.Perimeter;
                }
                (int rectStart, int rectEnd) = x < rect.Min ? (rect.Min, rect.Max) : (rect.Max, rect.Min);
                newSeconds += Math.Abs(x - rectStart) + rect.WholeSeconds(cycles) + rect2.WholeSeconds(cycles2);
                if (rect.Min < x && previous is { cycles: > 1, whole: false }) {
                    newSeconds -= cycles == cycles2 ? 2 * Math.Abs(rect.Max - x) : 2 * Math.Abs(rect.Min - x);
                    newSeconds += Math.Abs(rectEnd - x);
                } else {
                    x = cycles == cycles2 ? rectStart : rectEnd;
                }
                previous = (rect2, visited[i + 1].cycles, true);
                log.AppendLine("  whole rect");
                log.AppendLine($"  cycles: {cycles}, {rect}");
                log.AppendLine($"  cycles: {visited[i + 1].cycles}, {rect2}");
                log.AppendLine($"  {j++}: seconds: {newSeconds}, x: {x}, ");
                i++;
            } else {
                (int rectStart, int rectEnd) = x < rect.Min ? (rect.Min, rect.Max) : (rect.Max, rect.Min);
                // todo: refactor
                if (rect.Min < x
                        && cycles == 1
                        && previous.whole
                        && visited[i - 1].cycles - visited[i - 2].cycles == 2) {
                    newSeconds += 2 * rect.Height;
                } else {
                    newSeconds += Math.Abs(x - rectStart) + rect.HalfSeconds(cycles);
                    x = rectEnd;
                }
                previous = (rect, cycles, false);
                log.AppendLine($"  {j++}: seconds: {newSeconds}, x: {x}, cycles: {cycles}, {rect}, ");
            }
        }
        newSeconds += end - x;
        Array.Fill(rectsVisited, 0);
        foreach ((_, int i, long cycles) in rects) {
            rectsVisited[i] = cycles;
        }
        const string slowVisited =
                " 2,  1,  0,  0,  0,  0, 2007,  0,  0,  0,  0,  0,  0,  1,  0";
        string fastVisited = string.Join(", ", rectsVisited.Select(it => it.ToString().PadLeft(2)));
        if (newSeconds >= seconds && fastVisited != slowVisited) {
            return;
        }
        seconds = Math.Min(newSeconds, seconds);
        Test.Log(log);
        Test.Log((fastVisited, newSeconds), 1);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    IEnumerable<(State, long)> IGame<State>.NextWithCost(State current) {
        if (current.SulphurCollected != required) {
            for (var i = 0; i < sulphurRects.Count; i++) {
                var rect = sulphurRects[i];
                (int xStart, int xEnd) = (rect.Min, rect.Max);
                if (xStart != current.X || i != current.I) {
                    long distance = Math.Abs(current.X - xStart) + rect.HalfSeconds();
                    yield return (current.From(rect, i, xEnd), distance);
                }
                (xStart, xEnd) = (rect.Max, rect.Min);
                if (xStart != current.X || i != current.I) {
                    long distance = Math.Abs(current.X - xStart) + rect.HalfSeconds();
                    yield return (current.From(rect, i, xEnd), distance);
                }
            }
        } else {
            int end = endTargets.Last().X;
            yield return (current with { X = end }, end - current.X);
        }
    }

    bool IGame<State>.HasReachedEnd(State state) => state.X == endTargets.Last().X;

    // todo: RectsVisited is debug, remove custom equals too
    readonly record struct State(int X, int I, long[] RectsVisited, int SulphurCollected = 0) : IState<State> {
        public State From(SulphurRect rect, int i, int nextX) {
            var next = new State(nextX, i, [.. RectsVisited], SulphurCollected + rect.Deposits);
            next.RectsVisited[i]++;
            return next;
        }

        public bool Equals(State other) => X == other.X && I == other.I && SulphurCollected == other.SulphurCollected;
        public override int GetHashCode() => HashCode.Combine(X, I, SulphurCollected);
    }
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);

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
