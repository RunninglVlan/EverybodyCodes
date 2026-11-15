using System.Diagnostics;

Test.ExpectedSeparator = "---";
// https://everybody.codes/event/2026/quests/13
Test.Run((notes, part, expected) => {
    string[] data = notes.Split(Environment.NewLine);
    var signals = 0L;
    var clock = new PortalClock();
    for (var i = 0; i < data.Length; i++) {
        clock.Read((data[i++], data[i++], data[i++], data[i++]));
        signals += clock.Calibrate();
    }
    Assert.ActualEqualsExpected(signals, expected);
});

class PortalClock {
    const int HANDS = 3;

    readonly Hand[] hands = new Hand[HANDS];
    readonly Dictionary<int[], (long value, long[] moves)> pastHands = new(ArrayComparer<int>.Instance);

    public void Read((string counts, string directions, string positions, string moves) data) {
        int[] counts = [.. Split(data.counts).Select(int.Parse)];
        int[] directions = [.. Split(data.directions).Select(it => it == "CW" ? 1 : -1)];
        int[] positions = [.. Split(data.positions).Select(int.Parse)];
        long[] moves = [.. Split(data.moves).Select(long.Parse)];
        for (var i = 0; i < hands.Length; i++) {
            hands[i] = new Hand(counts[i], directions[i], positions[i], moves[i]);
        }
        pastHands.Clear();
        return;

        string[] Split(string row) => row.Split(' ').Last().Split(':');
    }

    public long Calibrate() {
        var value = 0L;
        while (hands.Any(it => it.CanMove)) {
            (int[] current, long[] currentMoves) = Move();
            for (var i = 0; i < hands.Length; i++) {
                var hand = hands[i];
                for (int j = i + 1; j < hands.Length; j++) {
                    if (hand.Meets(hands[j])) {
                        value++;
                    }
                }
                hand.EndTurn();
            }
            if (pastHands.TryGetValue(current, out var past)) {
                value += SkipMoves(past.moves, value - past.value);
                pastHands.Clear();
                continue;
            }
            pastHands.Add(current, (value, currentMoves));
        }
        return value;
    }

    (int[] current, long[] currentMoves) Move() {
        var current = new int[HANDS];
        var currentMoves = new long[HANDS];
        for (var i = 0; i < hands.Length; i++) {
            var hand = hands[i];
            if (!hand.CanMove) {
                hand.Finished = true;
                continue;
            }
            hand.Move();
            current[i] = hand.CurrentIndex;
            currentMoves[i] = hand.Moves;
        }
        return (current, currentMoves);
    }

    long SkipMoves(long[] pastMoves, long valueDelta) {
        var moveDiffs = new long[hands.Length];
        var skipTimes = long.MaxValue;
        for (var i = 0; i < hands.Length; i++) {
            long moves = hands[i].Moves;
            long diff = pastMoves[i] - moves;
            if (diff == 0) {
                continue;
            }
            moveDiffs[i] = diff;
            skipTimes = Math.Min(moves / diff, skipTimes);
        }
        for (var i = 0; i < hands.Length; i++) {
            hands[i].Moves -= moveDiffs[i] * skipTimes;
        }
        return valueDelta * skipTimes;
    }
}

class Hand(int count, int direction, int currentIndex, long moves) {
    readonly int direction = direction;
    float previous = currentIndex / (float)count;
    public int CurrentIndex { get; private set; } = currentIndex;
    float Current => CurrentIndex / (float)count;
    float CurrentWrapped => Mathf.Wrap(CurrentIndex, count) / (float)count;
    public long Moves { get; set; } = moves;
    public bool Finished { get; set; }
    public bool CanMove => Moves > 0;

    public void Move() {
        CurrentIndex += direction;
        Moves--;
    }

    public bool Meets(Hand other) {
        if (Mathf.Approximately(previous, other.previous)) {
            return false;
        }
        if (direction != other.direction || Finished || other.Finished) {
            if (IntervalsIntersect(other)) {
                return true;
            }
        } else if (direction == other.direction && AnyIntervalContains(other)) {
            return true;
        }
        return Mathf.Approximately(CurrentWrapped, other.CurrentWrapped);
    }

    bool IntervalsIntersect(Hand other) {
        return Interval().Intersects(other.Interval());
    }

    bool AnyIntervalContains(Hand other) {
        var interval = Interval();
        var otherInterval = other.Interval();
        return interval.Contains(otherInterval) || otherInterval.Contains(interval);
    }

    Interval Interval() {
        if (Current < 0 && Mathf.Approximately(previous, 0)) {
            return new Interval(previous + 1, Current + 1);
        }
        return new Interval(previous, Current);
    }

    public void EndTurn() {
        CurrentIndex = Mathf.Wrap(CurrentIndex, count);
        previous = Current;
    }

    public override string ToString() => $"{previous:F} {(!Finished ? "=>" : "ST")} {Current:F}";
}

readonly struct Interval {
    readonly float min, max;

    public Interval(float a, float b) => (min, max) = a < b ? (a, b) : (b, a);

    public bool Intersects(Interval other) => min <= other.max && other.min <= max;
    public bool Contains(Interval other) => min <= other.min && other.max <= max;
}

sealed class ArrayComparer<T> : IEqualityComparer<T[]> {
    public static readonly ArrayComparer<T> Instance = new();

    public bool Equals(T[]? a, T[]? b) {
        return a == b || a != null && b != null && a.SequenceEqual(b);
    }

    public int GetHashCode(T[] items) {
        var hash = new HashCode();
        foreach (var item in items) {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

static class Mathf {
    public static bool Approximately(double a, double b) => Math.Abs(a - b) < 0.001;

    public static int Wrap(int value, int max) {
        int remainder = value % max;
        return remainder < 0 ? remainder + max : remainder;
    }
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
