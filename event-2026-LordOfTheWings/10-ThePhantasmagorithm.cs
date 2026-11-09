using System.Diagnostics;

// https://everybody.codes/event/2026/quests/10
Test.Run((notes, part, expected) => {
    var grid = new Grid();
    grid.Init(notes);
    (long rounds, long maxGhostCollisions, bool skip) = part switch {
        1 => (10, 10_000, false),
        2 => (int.MaxValue, 10_000, false),
        _ => (long.MaxValue, 1_000_000_000_000, true)
    };
    grid.SimulateRounds(rounds, maxGhostCollisions, skip);
    long count = part == 1 ? grid.Collisions : grid.Rounds;
    Assert.ActualEqualsExpected(count, expected);
});

class Grid {
    readonly Dictionary<Point, bool> walkableCells = [];
    readonly List<NullGhost> ghosts = [];
    readonly List<NullGhost[]> pastGhosts = [];
    public long Collisions { get; private set; }
    public long Rounds { get; private set; }

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        int width = rows.First().Length, height = rows.Length;
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

    public void SimulateRounds(long maxRounds, long maxGhostCollisions, bool skip) {
        Collisions = 0;
        for (Rounds = 0; Rounds < maxRounds && ghosts.Count > 0; Rounds++) {
            SimulateRound(maxGhostCollisions, skip);
        }
    }

    void SimulateRound(long maxGhostCollisions, bool skip) {
        MoveAndCollide();
        ghosts.RemoveAll(it => it.Collisions >= maxGhostCollisions);
        if (!skip) {
            return;
        }
        var current = ghosts.ToArray();
        for (var i = 0; i < pastGhosts.Count; i++) {
            var past = pastGhosts[i];
            if (!current.SequenceEqual(past)) {
                continue;
            }
            SkipRounds(past, maxGhostCollisions, pastGhosts.Count - i);
            pastGhosts.Clear();
            return;
        }
        pastGhosts.Add(current);
    }

    void MoveAndCollide() {
        for (var i = 0; i < ghosts.Count; i++) {
            var ghost = ghosts[i];
            var next = ghost.Point + ghost.Direction;
            if (walkableCells[next]) {
                ghost.Point = next;
            } else {
                ghost.Direction = Screen.TurnRight(ghost.Direction);
                ghost.Collisions++;
                Collisions++;
            }
            ghosts[i] = ghost;
        }
        var collisions = ghosts.GroupBy(it => it.Point).Where(it => it.Count() > 1);
        foreach (var collision in collisions) {
            for (var i = 0; i < ghosts.Count; i++) {
                var ghost = ghosts[i];
                if (ghost.Point != collision.Key) {
                    continue;
                }
                ghost.Direction = -ghost.Direction;
                ghost.Collisions++;
                Collisions++;
                ghosts[i] = ghost;
            }
        }
    }

    void SkipRounds(NullGhost[] past, long maxGhostCollisions, long skipRounds) {
        var collisionDiffs = new long[ghosts.Count];
        var skipTimes = long.MaxValue;
        for (var i = 0; i < ghosts.Count; i++) {
            long collisions = ghosts[i].Collisions;
            long diff = collisions - past[i].Collisions;
            collisionDiffs[i] = diff;
            long times = (maxGhostCollisions - collisions) / diff;
            if (collisions + diff * times >= maxGhostCollisions) {
                return;
            }
            skipTimes = Math.Min(times, skipTimes);
        }
        Rounds += skipRounds * skipTimes;
        for (var i = 0; i < ghosts.Count; i++) {
            var ghost = ghosts[i];
            ghost.Collisions += collisionDiffs[i] * skipTimes;
            ghosts[i] = ghost;
        }
    }
}

record struct NullGhost(Point Point, Point Direction, long Collisions = 0) {
    public bool Equals(NullGhost other) => Point == other.Point && Direction == other.Direction;
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
