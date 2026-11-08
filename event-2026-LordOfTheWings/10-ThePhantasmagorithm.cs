using System.Diagnostics;
using System.Text;

Test.StepEnabled = false;
Test.From = "Part III Example";
// Test.Until = "Part III Example";
// Test.LogEnabled = false;
// https://everybody.codes/event/2026/quests/10
Test.Run((notes, part, expected) => {
    var grid = new Grid();
    grid.Init(notes);
    (long rounds, long maxGhostCollisions) = part switch {
        1 => (10, 10_000),
        2 => (int.MaxValue, 10_000),
        _ => (long.MaxValue, 1_000_000_000_000)
    };
    grid.SimulateRounds(rounds, maxGhostCollisions);
    long count = part == 1 ? grid.Collisions : grid.Rounds;
    Assert.ActualEqualsExpected(count, expected);
});

class Grid {
    int width, height;
    readonly Dictionary<Point, bool> walkableCells = [];
    readonly List<NullGhost> ghosts = [];
    readonly List<GhostState[]> roundGhosts = [];
    public long Collisions { get; private set; }
    public long Rounds { get; private set; }
    readonly Dictionary<Point, int> roundCollisions = [];

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        width = rows.First().Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = rows[y][x];
                var point = new Point(x, y);
                walkableCells[point] = cell != '#';
                if (Screen.TryParseDirection(cell, out var ghostDirection)) {
                    ghosts.Add(new NullGhost(point, ghostDirection));
                }
            }
        }
    }

    public void SimulateRounds(long maxRounds, long maxGhostCollisions) {
        const bool console = true;
        roundGhosts.Add([.. ghosts.Select(it => it.State())]);
        Collisions = 0;
        Rounds = 0;
        for (; Rounds < maxRounds && ghosts.Count > 0; Rounds++) {
            roundCollisions.Clear();
            // Test.Log($"Round: {Rounds + 1}", console);
            foreach (var ghost in ghosts) {
                var next = ghost.Point + ghost.Direction;
                if (walkableCells[next]) {
                    ghost.Point = next;
                } else {
                    ghost.Direction = Screen.TurnRight(ghost.Direction);
                    AddCollision(next, ghost);
                }
            }
            foreach (var collision in ghosts.GroupBy(it => it.Point).Where(it => it.Count() > 1)) {
                foreach (var ghost in collision) {
                    ghost.Direction = -ghost.Direction;
                    AddCollision(ghost.Point, ghost);
                }
            }
            // Test.Log($"Collisions: {roundCollisions.Values.Sum()}", console);
            // Test.Log(console: console);
            // Test.Log(this, console);
            ghosts.RemoveAll(it => it.Collisions >= maxGhostCollisions);
            Progress();
            var currentGhosts = ghosts.Select(it => it.State()).ToArray();
            roundGhosts.Add(currentGhosts);
            for (var i = 0; i < roundGhosts.Count - 1; i++) {
                var pastGhosts = roundGhosts[i];
                if (!currentGhosts.SequenceEqual(pastGhosts)) {
                    continue;
                }
                Test.Log($"same ghosts at {i}", console);
                Test.Step();
                SkipRounds(maxGhostCollisions, Rounds - i, pastGhosts, currentGhosts);
                roundGhosts.Clear();
                break;
            }
        }
    }

    void AddCollision(Point point, NullGhost ghost) {
        int count = roundCollisions.GetValueOrDefault(point);
        roundCollisions[point] = count + 1;
        Collisions++;
        ghost.Collisions++;
    }

    void SkipRounds(long maxGhostCollisions, long skipRounds, GhostState[] past, GhostState[] current) {
        var collisionDiffs = new int[current.Length];
        for (var i = 0; i < current.Length; i++) {
            collisionDiffs[i] = current[i].Collisions - past[i].Collisions;
        }
        // need to get rid of this loop too
        while (true) {
            Rounds += skipRounds;
            var safe = true;
            for (var i = 0; i < ghosts.Count; i++) {
                ghosts[i].Collisions += collisionDiffs[i];
                if (maxGhostCollisions - ghosts[i].Collisions < collisionDiffs[i]) {
                    safe = false;
                }
            }
            Progress();
            if (!safe) {
                break;
            }
        }
    }

    int interval = 10;

    void Progress() {
        if (ghosts.Count <= 0) {
            return;
        }
        int maxCollisions = ghosts.Max(it => it.Collisions);
        if (maxCollisions % interval != 0) {
            return;
        }
        Test.Log($"Round: {Rounds + 1}, ghosts: {ghosts.Count}, maxCollisions: {maxCollisions}", console: true);
        interval *= 10;
    }

    public override string ToString() {
        var result = new StringBuilder();
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var point = new Point(x, y);
                char cell = walkableCells[point] ? Cells.EMPTY : Cells.WALL;
                var ghost = ghosts.FirstOrDefault(it => it.Point == point);
                if (ghost != null) {
                    cell = Screen.ToChar(ghost.Direction);
                }
                if (cell != Cells.WALL && roundCollisions.TryGetValue(point, out int count)) {
                    result.Append(count);
                } else {
                    result.Append(cell);
                }
            }
            if (y != height - 1) {
                result.AppendLine();
            }
        }
        return result.ToString();
    }
}

static class Cells {
    public const char EMPTY = '.';
    public const char WALL = '#';
}

class NullGhost(Point point, Point direction) {
    public Point Point { get; set; } = point;
    public Point Direction { get; set; } = direction;
    public int Collisions { get; set; }

    public GhostState State() => new(Point, Direction, Collisions);
}

readonly record struct GhostState(Point Point, Point Direction, int Collisions) {
    public bool Equals(GhostState other) => Point == other.Point && Direction == other.Direction;
    public override int GetHashCode() => HashCode.Combine(Point, Direction);
}

readonly record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point value) => new(-value.X, -value.Y);
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

    public static char ToChar(Point direction) => direction switch {
        _ when direction == RIGHT => '>',
        _ when direction == LEFT => '<',
        _ when direction == UP => '^',
        _ when direction == DOWN => 'v',
        _ => throw new ArgumentException($"Unsupported direction: {direction}")
    };

    public static Point TurnRight(Point direction) => new(-direction.Y, direction.X);
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }
    public static bool LogEnabled { get; set; } = true;

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
            string notes = ReadUntilNewLine();
            string expected = ReadUntilNewLine();
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

    static string ReadUntilNewLine() {
        List<string> lines = [];
        for (string line; !string.IsNullOrEmpty(line = Console.ReadLine()!);) {
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

    public static void Log(object? line = null, bool console = false, ConsoleColor? color = null) {
        if (!LogEnabled) {
            return;
        }
        LogInternal(line, console, color);
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
