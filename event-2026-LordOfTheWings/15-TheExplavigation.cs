#!/usr/bin/env dotnet
#:include ../generic/Test.cs
#:include ../generic/Geometry.cs
#:include ../generic/Pathfinding.cs
#:include ../generic/StateSearch.cs
#:include ../generic/StateSearchWithCost.cs

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
