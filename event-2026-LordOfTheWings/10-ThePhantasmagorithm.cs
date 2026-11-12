// https://everybody.codes/event/2026/quests/10

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
var grid = new Grid();
grid.Init(notes);
(long rounds, long maxGhostCollisions) = part switch {
    1 => (10, 10_000),
    2 => (int.MaxValue, 10_000),
    _ => (long.MaxValue, 1_000_000_000_000)
};
grid.SimulateRounds(rounds, maxGhostCollisions);
long count = part == 1 ? grid.Collisions : grid.Rounds;
Console.WriteLine(count);

class Grid {
    readonly HashSet<Point> walkableCells = [];
    readonly List<NullGhost> ghosts = [];
    readonly Dictionary<GhostState[], (int index, long[] collisions)> pastGhosts = new(GhostState.Comparer);
    public long Collisions { get; private set; }
    public long Rounds { get; private set; }

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        int width = rows.First().Length, height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = rows[y][x];
                var point = new Point(x, y);
                if (cell != '#') {
                    walkableCells.Add(point);
                }
                if (Screen.TryParseDirection(cell, out var ghostDirection)) {
                    ghosts.Add(new NullGhost(point, ghostDirection));
                }
            }
        }
    }

    public void SimulateRounds(long maxRounds, long maxGhostCollisions) {
        Collisions = 0;
        for (Rounds = 0; Rounds < maxRounds && ghosts.Count > 0; Rounds++) {
            MoveAndCollide();
            ghosts.RemoveAll(it => it.Collisions >= maxGhostCollisions);
            var current = new GhostState[ghosts.Count];
            var currentCollisions = new long[ghosts.Count];
            for (var i = 0; i < ghosts.Count; i++) {
                var ghost = ghosts[i];
                (current[i], currentCollisions[i]) = (new GhostState(ghost.Point, ghost.Direction), ghost.Collisions);
            }
            if (pastGhosts.TryGetValue(current, out var past)) {
                SkipRounds(past.collisions, maxGhostCollisions, pastGhosts.Count - past.index);
                pastGhosts.Clear();
                continue;
            }
            pastGhosts.Add(current, (pastGhosts.Count, currentCollisions));
        }
    }

    void MoveAndCollide() {
        foreach (var ghost in ghosts) {
            var next = ghost.Point + ghost.Direction;
            if (walkableCells.Contains(next)) {
                ghost.Point = next;
            } else {
                ghost.Direction = Screen.TurnRight(ghost.Direction);
                ghost.Collisions++;
                Collisions++;
            }
        }
        foreach (var collision in ghosts.GroupBy(it => it.Point).Where(it => it.Count() > 1)) {
            foreach (var ghost in collision) {
                ghost.Direction = -ghost.Direction;
                ghost.Collisions++;
                Collisions++;
            }
        }
    }

    void SkipRounds(long[] pastCollisions, long maxGhostCollisions, long skipRounds) {
        var collisionDiffs = new long[ghosts.Count];
        var skipTimes = long.MaxValue;
        for (var i = 0; i < ghosts.Count; i++) {
            long collisions = ghosts[i].Collisions;
            long diff = collisions - pastCollisions[i];
            collisionDiffs[i] = diff;
            long times = (maxGhostCollisions - collisions) / diff;
            if (collisions + diff * times >= maxGhostCollisions) {
                return;
            }
            skipTimes = Math.Min(times, skipTimes);
        }
        Rounds += skipRounds * skipTimes;
        for (var i = 0; i < ghosts.Count; i++) {
            ghosts[i].Collisions += collisionDiffs[i] * skipTimes;
        }
    }
}

class NullGhost(Point point, Point direction) {
    public Point Point { get; set; } = point;
    public Point Direction { get; set; } = direction;
    public long Collisions { get; set; }
}

readonly record struct GhostState(Point Point, Point Direction) {
    public static GhostComparer Comparer { get; } = new();

    public class GhostComparer : IEqualityComparer<GhostState[]> {
        public bool Equals(GhostState[]? a, GhostState[]? b) {
            return a == b || a != null && b != null && a.SequenceEqual(b);
        }

        public int GetHashCode(GhostState[] states) {
            var hash = new HashCode();
            foreach (var state in states) {
                hash.Add(state);
            }
            return hash.ToHashCode();
        }
    }
}

readonly record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point value) => new(-value.X, -value.Y);

    public override string ToString() => $"({X}, {Y})";
}

static class Screen {
    static readonly Point UP = new(0, -1);
    static readonly Point DOWN = new(0, 1);
    static readonly Point LEFT = new(-1, 0);
    static readonly Point RIGHT = new(1, 0);

    public static bool TryParseDirection(char c, out Point direction) {
        direction = c switch {
            '>' => RIGHT,
            '<' => LEFT,
            '^' => UP,
            'v' => DOWN,
            _ => default
        };
        return direction != default;
    }

    public static Point TurnRight(Point direction) => new(-direction.Y, direction.X);
}
