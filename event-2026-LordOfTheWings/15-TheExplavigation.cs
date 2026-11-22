using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

Test.From = "Part III";
// https://everybody.codes/event/2026/quests/15
Test.Run((notes, part, expected) => {
    // Console.WriteLine(DepositRoute.Solve(notes.Split(Environment.NewLine)));
    Grid grid = part switch {
        1 => new FiniteDepositGrid(),
        2 => new InfiniteDepositGrid(2026),
        _ => new InfiniteDepositGrid(202620262026)
    };
    grid.Read(notes);
    long seconds = grid.CollectSulphurAndReachEnd();
    Assert.ActualEqualsExpected(seconds, expected);
});

public static class DepositRoute {
    // Headings: 0 = up, 1 = right, 2 = down, 3 = left (clockwise order)
    static readonly int[] DR = { -1, 0, 1, 0 };
    static readonly int[] DC = { 0, 1, 0, -1 };
    static readonly char[] ARROW = { '^', '>', 'v', '<' };

    public static int Solve(string[] grid, int required = 12624) {
        int H = grid.Length;
        if (H == 0) return -1;
        int W = grid[0].Length;

        int sr = -1, sc = -1;
        for (int r = 0; r < H; r++)
        for (int c = 0; c < W; c++)
            if (grid[r][c] == 'S') {
                sr = r;
                sc = c;
            }
        if (sr < 0) return -1;

        int C = required + 1; // deposit counts 0..required
        long total = (long)H * W * 4 * C;
        if (total > int.MaxValue) throw new InvalidOperationException("State space too large");
        int[] dist = new int[total];
        Array.Fill(dist, -1);
        int[] parent = new int[total];
        Array.Fill(parent, -1);
        byte[] moveHeading = new byte[total];
        int[] queue = new int[total];
        int head = 0, tail = 0;

        // Start facing every direction with zero deposits.
        for (int h = 0; h < 4; h++) {
            int idx = ((sr * W + sc) * 4 + h) * C;
            dist[idx] = 0;
            queue[tail++] = idx;
        }

        int goal = -1;
        while (head < tail) {
            int cur = queue[head++];
            int cnt = cur % C;
            int rest = cur / C;
            int h = rest % 4;
            int cell = rest / 4;
            int r = cell / W;
            int c = cell % W;

            if (grid[r][c] == 'E' && cnt == required) {
                goal = cur;
                break;
            }

            // Forward, turn left and step, turn right and step.
            int[] nextHeadings = { h, (h + 3) % 4, (h + 1) % 4 };
            foreach (int nh in nextHeadings) {
                int nr = r + DR[nh];
                int nc = c + DC[nh];
                if (nr < 0 || nr >= H || nc < 0 || nc >= W) continue;
                char ch = grid[nr][nc];
                if (ch == '#') continue;
                int ncnt = ch == '@' ? cnt + 1 : cnt;
                if (ncnt > required) continue;
                int ni = ((nr * W + nc) * 4 + nh) * C + ncnt;
                if (dist[ni] != -1) continue;
                dist[ni] = dist[cur] + 1;
                parent[ni] = cur;
                moveHeading[ni] = (byte)nh;
                queue[tail++] = ni;
            }
        }

        if (goal < 0) {
            Console.WriteLine("No path found.");
            return -1;
        }

        // Walk back from the goal to the start to collect the route.
        var chain = new List<int>();
        for (int i = goal; i != -1; i = parent[i])
            chain.Add(i);
        chain.Reverse();

        // Draw the arrow for each step on a copy of the grid.
        char[][] outGrid = grid.Select(l => l.ToCharArray()).ToArray();
        for (int k = 1; k < chain.Count; k++) {
            int idx = chain[k];
            int from = parent[idx];
            int fcell = (from / C) / 4;
            int fr = fcell / W;
            int fc = fcell % W;
            char mark = grid[fr][fc];
            if (mark == 'S' || mark == 'E') continue;
            outGrid[fr][fc] = ARROW[moveHeading[idx]];
        }

        // Print the grid with arrows in red.
        foreach (char[] row in outGrid) {
            foreach (char ch in row) {
                if ("^>v<".IndexOf(ch) >= 0) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(ch);
                    Console.ResetColor();
                } else {
                    Console.Write(ch);
                }
            }
            Console.WriteLine();
        }

        return dist[goal];
    }
}

abstract class Grid {
    protected int width, height;
    protected Point start;
    protected readonly Dictionary<Point, bool> walkableCells = [];
    protected readonly HashSet<Point> sulphurDeposits = [];
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
                HashSet<Point> cycleDeposits = [.. path.Where(it => sulphurDeposits.Contains(it))];
                sulphurCycles.Add(new SulphurCycle(path, cycleDeposits));
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

readonly record struct SulphurCycle {
    // todo: need only count
    public HashSet<Point> Deposits { get; }
    public int Min { get; }
    public int Max { get; }
    public int Perimeter { get; }
    public int Width { get; }
    int Height { get; }

    public SulphurCycle(HashSet<Point> path, HashSet<Point> deposits) {
        Deposits = deposits;
        Min = path.Min(it => it.X);
        Max = path.Max(it => it.X);
        Width = Max - Min;
        Height = path.Max(it => it.Y) - path.Min(it => it.Y);
        Perimeter = 2 * (Height + Width);
    }

    public long HalfSeconds(long count = 1) => count == 0 ? 0 : count * Perimeter - Width;
    public long WholeSeconds(long count = 1) => count == 0 ? 0 : count * Perimeter - count * Width;

    public override string ToString() {
        return $"Deposits: {Deposits.Count}, Min: {Min}, Max: {Max}, Width: {Width}, Height: {Height}";
    }
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
                yield return current.From(cycle, i, cycle.Min);
                yield return current.From(cycle, i, cycle.Max);
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
                if (state.CyclesVisited[i] || cycle.Max > endTarget.X) {
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
        public State From(SulphurCycle cycle, int i, int x) {
            int nextX = cycle.Min == x ? cycle.Max : cycle.Min;
            int distance = Math.Abs(X - x) + cycle.Perimeter - cycle.Width;
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

class InfiniteDepositGrid : Grid, IGame<InfiniteDepositGrid.State>, IGame<InfiniteDepositGrid.MoveState> {
    readonly StateSearch<State> stateSearch;
    readonly StateSearch<MoveState> moveStateSearch;

    // todo: should be readonly
    long required;

    public InfiniteDepositGrid(long required) {
        stateSearch = new StateSearch<State>(this);
        moveStateSearch = new StateSearch<MoveState>(this);
        this.required = required;
    }

    // todo: move best version inside, remove other versions
    public override long CollectSulphurAndReachEnd() {
        DoTest();
        if (false) {
            return Slow().seconds;
        }
        if (false) {
            return Backtracking();
        }
        if (true) {
            return Fast().seconds;
        }
        if (false) {
            return MoveVersion().seconds;
        }
    }

    void DoTest() {
        Test.Log("Deposits:\n"
                + string.Join(", ", sulphurCycles.Select(it => it.Deposits.Count.ToString().PadLeft(2))));
        Test.Log("Perimeter:\n" + string.Join(", ", sulphurCycles.Select(it => it.Perimeter.ToString().PadLeft(2))));
        Test.Log("gain:\n"
                + string.Join(", ",
                        sulphurCycles.Select(it => (it.Deposits.Count / (float)it.Perimeter).ToString("F")[2..])));
        Test.Log("zone:\n"
                + string.Join(", ", sulphurCycles
                        .Select(it => it.Max < endTargets[0].X ? " 0" : it.Max < endTargets[1].X ? " 1" : " 2")));
        var visit = new long[sulphurCycles.Count];
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
            var fast = Fast();
            long fastElapsed = stopwatch.ElapsedMilliseconds;
            Test.Log($"required [{min}-{max}): {required}, slow: {slowElapsed}, fast: {fastElapsed}");
            if (slow.seconds == fast.seconds) {
                continue;
            }
            int slowVisited = slow.cyclesVisited.Count(it => it > 0);
            Test.Log(("slow\n" + string.Join(", ", slow.cyclesVisited.Select(it => it.ToString().PadLeft(2))),
                    $"seconds: {slow.seconds}, visited: {slowVisited}"), 1);
            Test.Log($"difference: {slow.seconds - fast.seconds}");
            int fastVisited = fast.cyclesVisited.Count(it => it > 0);
            Test.Log(("fast\n" + string.Join(", ", fast.cyclesVisited.Select(it => it.ToString().PadLeft(2))),
                    $"seconds: {fast.seconds}, visited: {fastVisited}"), 1);
            long[] previous = [.. visit];
            for (var j = 0; j < slow.cyclesVisited.Length; j++) {
                visit[j] = Math.Max(slow.cyclesVisited[j], visit[j]);
            }
            maxVisited = Math.Max(maxVisited, slow.cyclesVisited.Count(it => it > 0));
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

    (long[] cyclesVisited, long seconds) Slow() {
        return stateSearch.TryFindCheapest(new State(start.X, -1, new long[sulphurCycles.Count]), out var endState)
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
                if (cycle.Min != x) {
                    long newSeconds = seconds + Math.Abs(x - cycle.Min) + cycle.HalfSeconds();
                    return Find(cycle.Max, sulphurCollected + cycle.Deposits.Count, newSeconds);
                }
                if (cycle.Max != x) {
                    long newSeconds = seconds + Math.Abs(x - cycle.Max) + cycle.HalfSeconds();
                    return Find(cycle.Min, sulphurCollected + cycle.Deposits.Count, newSeconds);
                }
            }
            return 0;
        }
    }

    (long[] cyclesVisited, long seconds) Fast() {
        int end = endTargets.Last().X;
        bool[] visit = [
            .. " _,  _,  1,  _,  1,  _,  1,  1,  _,  1,  _,  _,  1,  _,  _,  _,  1,  1,  1,  1,  1,  _,  _,  _,  _,  _,  _,  _,  _,  1"
                    .Split(", ").Select(it => it == " 1")
        ];
        var bestCycles = sulphurCycles.Select((cycle, i) => (cycle, i)).Where(it => visit[it.i]).ToList();
        var mainCycles = sulphurCycles.Select((cycle, i) => (cycle, i)).Where(it => it.i is 2 or 29).ToList();
        // mainCycles = [.. sulphurCycles.Select((cycle, i) => (cycle, i)).Where(it => it.i is 6 or 16)];
        bestCycles.RemoveAll(it => mainCycles.Any(main => main.i == it.i));
        var mainCount = (long)Math.Ceiling(required / (double)mainCycles.Select(it => it.cycle.Deposits.Count).Sum());
        long[] mainCounts = [mainCount, mainCount];
        var seconds = long.MaxValue;
        List<(SulphurCycle value, int i, long count)> visited =
                [.. mainCycles.Select((tuple, i) => (tuple.cycle, tuple.i, mainCounts[i]))];
        var cyclesVisited = new long[sulphurCycles.Count];
        Collect(end, ref cyclesVisited, ref seconds, visited);
        const long maxCount = 3;
        long max = bestCycles.Sum(it => it.cycle.Deposits.Count)
                / mainCycles.Sum(it => it.cycle.Deposits.Count)
                * maxCount;
        for (var _ = 0; _ <= max; _++) {
            mainCounts[0]--;
            visited.Clear();
            visited.AddRange(mainCycles.Select((tuple, i) => (tuple.cycle, tuple.i, mainCounts[i])));
            Visit(0);
            mainCounts[0]++;
            mainCounts[1]--;
            visited.Clear();
            visited.AddRange(mainCycles.Select((tuple, i) => (tuple.cycle, tuple.i, mainCounts[i])));
            Visit(0);
            mainCounts[0]--;
            visited.Clear();
            visited.AddRange(mainCycles.Select((tuple, i) => (tuple.cycle, tuple.i, mainCounts[i])));
            Visit(0);
        }
        return (cyclesVisited, seconds);

        void Visit(int i) {
            if (i >= bestCycles.Count) {
                Collect(end, ref cyclesVisited, ref seconds, visited);
                return;
            }
            for (var count = 0; count <= maxCount; count++) {
                var tuple = bestCycles[i];
                visited.Add((tuple.cycle, tuple.i, count));
                Visit(i + 1);
                visited.RemoveAt(visited.Count - 1);
            }
        }
    }

    void Collect(int end, ref long[] cyclesVisited, ref long seconds,
            List<(SulphurCycle value, int i, long count)> cycles) {
        long collected = cycles.Sum(it => it.value.Deposits.Count * it.count);
        if (collected != required) {
            return;
        }
        long newSeconds = 0;
        var x = 0;
        (SulphurCycle cycle, long count, bool whole) previous = (default, -1, false);
        var log = new StringBuilder();
        log.AppendLine($"new, required: {required}");
        var j = 1;
        var visited = cycles.Where(it => it.count > 0).OrderBy(it => it.value.Min).ThenBy(it => it.count)
                .Select(it => (it.value, it.count))
                .ToArray();
        for (var i = 0; i < visited.Length; i++) {
            (var cycle, long count) = visited[i];
            if (i + 1 < visited.Length && cycle.Min == visited[i + 1].value.Min) {
                (var cycle2, long count2) = visited[i + 1];
                if (count2 - count > 1) {
                    Test.Log("unhandled");
                    return;
                }
                (int cycleStart, int cycleEnd) = x < cycle.Min ? (cycle.Min, cycle.Max) : (cycle.Max, cycle.Min);
                newSeconds += Math.Abs(x - cycleStart) + cycle.WholeSeconds(count) + cycle2.WholeSeconds(count2);
                if (cycle.Min < x && previous is { count: > 1, whole: false }) {
                    newSeconds -= count == count2 ? 2 * Math.Abs(cycle.Max - x) : 2 * Math.Abs(cycle.Min - x);
                    newSeconds += Math.Abs(cycleEnd - x);
                } else {
                    x = count == count2 ? cycleStart : cycleEnd;
                }
                log.AppendLine("  whole cycle");
                previous = (cycle, count, true);
                i++;
            } else {
                (int cycleStart, int cycleEnd) = x < cycle.Min ? (cycle.Min, cycle.Max) : (cycle.Max, cycle.Min);
                newSeconds += Math.Abs(x - cycleStart) + cycle.HalfSeconds(count);
                x = cycleEnd;
                previous = (cycle, count, false);
            }
            log.AppendLine($"  {j++}: seconds: {newSeconds}, x: {x}, count: {count}, {cycle}, ");
        }
        newSeconds += end - x;
        Array.Fill(cyclesVisited, 0);
        foreach ((_, int i, long count) in cycles) {
            cyclesVisited[i] = count;
        }
        const string slowVisited =
                " 0,  0, 217,  0,  0,  0,  1,  0,  0,  1,  0,  0,  0,  0,  0,  0,  3,  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,  0, 216";
        string fastVisited = string.Join(", ", cyclesVisited.Select(it => it.ToString().PadLeft(2)));
        if (newSeconds >= seconds && fastVisited != slowVisited) {
            return;
        }
        seconds = Math.Min(newSeconds, seconds);
        Test.Log(log);
        Test.Log((fastVisited, newSeconds), 1);
    }

    (MoveState, long seconds) MoveVersion() {
        if (!moveStateSearch.TryFindCheapest(new MoveState(start, Point.RIGHT, []), out var endState)) {
            throw new UnreachableException();
        }
        // Print(endState.Item1);
        return endState;
    }

    void Print(MoveState state) {
        var visited = state.Visited.ToHashSet();
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var point = new Point(x, y);
                Console.Write(visited.Contains(point) ? "X" : " ");
            }
            Console.WriteLine();
        }
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    IEnumerable<(State, long)> IGame<State>.NextWithCost(State current) {
        if (current.SulphurCollected != required) {
            for (var i = 0; i < sulphurCycles.Count; i++) {
                var cycle = sulphurCycles[i];
                (int xStart, int xEnd) = (cycle.Min, cycle.Max);
                if (xStart != current.X || i != current.I) {
                    long distance = Math.Abs(current.X - xStart) + cycle.HalfSeconds();
                    yield return (current.From(cycle, i, xEnd), distance);
                }
                (xStart, xEnd) = (cycle.Max, cycle.Min);
                if (xStart != current.X || i != current.I) {
                    long distance = Math.Abs(current.X - xStart) + cycle.HalfSeconds();
                    yield return (current.From(cycle, i, xEnd), distance);
                }
            }
        } else {
            int end = endTargets.Last().X;
            yield return (current with { X = end }, end - current.X);
        }
    }

    bool IGame<State>.HasReachedEnd(State state) => state.X == endTargets.Last().X;

    IEnumerable<(MoveState, long)> IGame<MoveState>.NextWithCost(MoveState current) {
        var directions = new[] {
            current.Direction, Screen.TurnLeft(current.Direction), Screen.TurnRight(current.Direction)
        };
        foreach (var direction in directions) {
            var nextPoint = current.Point + direction;
            if (nextPoint.X < 0 || nextPoint.X > width - 1 || !walkableCells[nextPoint]) {
                continue;
            }
            var next = current with {
                Point = nextPoint, Direction = direction, Visited = [.. current.Visited, nextPoint]
            };
            if (sulphurDeposits.Contains(nextPoint)) {
                next.SulphurCollected++;
            }
            yield return (next, 1);
        }
    }

    bool IGame<MoveState>.HasReachedEnd(MoveState state) {
        return state.SulphurCollected >= required && state.Point == endTargets.Last();
    }

    // todo: CyclesVisited is debug, remove custom equals too
    readonly record struct State(int X, int I, long[] CyclesVisited, int SulphurCollected = 0) : IState<State> {
        public State From(SulphurCycle cycle, int i, int nextX) {
            var next = new State(nextX, i, [.. CyclesVisited], SulphurCollected + cycle.Deposits.Count);
            next.CyclesVisited[i]++;
            return next;
        }

        public bool Equals(State other) => X == other.X && I == other.I && SulphurCollected == other.SulphurCollected;
        public override int GetHashCode() => HashCode.Combine(X, I, SulphurCollected);
    }

    record struct MoveState(Point Point, Point Direction, List<Point> Visited) : IState<MoveState> {
        public int SulphurCollected { get; set; }

        public bool Equals(MoveState other) => Point == other.Point
                && Direction == other.Direction
                && SulphurCollected == other.SulphurCollected;

        public override int GetHashCode() => HashCode.Combine(Point, Direction, SulphurCollected);
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

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    public static Point operator +(Point a, int value) => new(a.X + value, a.Y + value);
    public static Point operator -(Point a, int value) => new(a.X - value, a.Y - value);
    public static Point operator -(Point value) => new(-value.X, -value.Y);

    public override string ToString() => $"({X}, {Y})";
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
