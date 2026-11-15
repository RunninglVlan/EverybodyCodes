using System.Diagnostics;

Test.ExpectedSeparator = "---";
// Test.LogEnabled = false;
Test.From = "Part III Example";
// Test.Until = "Part II Example 2";
// https://everybody.codes/event/2026/quests/13
Test.Run((notes, part, expected) => {
    string[] data = notes.Split(Environment.NewLine);
    var signals = 0;
    for (int i = 0, j = 0; i < data.Length; i++, j++) {
        Test.Log($"Clock {j + 1:000}");
        var clock = new PortalClock();
        clock.Read((data[i++], data[i++], data[i++], data[i++]));
        signals += clock.Calibrate(j + 1 == 99);
        Test.Log();
    }
    Assert.ActualEqualsExpected(signals, expected);
});

class PortalClock {
    readonly Hand[] hands = new Hand[3];

    public void Read((string counts, string directions, string positions, string moves) data) {
        Test.Log(data.counts);
        Test.Log(data.directions);
        Test.Log(data.positions);
        Test.Log(data.moves);
        int[] counts = [.. Split(data.counts).Select(int.Parse)];
        int[] directions = [.. Split(data.directions).Select(it => it == "CW" ? 1 : -1)];
        int[] positions = [.. Split(data.positions).Select(int.Parse)];
        long[] moves = [.. Split(data.moves).Select(long.Parse)];
        for (var i = 0; i < hands.Length; i++) {
            hands[i] = new Hand(counts[i], directions[i], positions[i], moves[i]);
        }
        return;

        string[] Split(string row) => row.Split(' ').Last().Split(':');
    }

    public int Calibrate(bool log) {
        var value = 0;
        while (hands.Any(it => it.CanMove)) {
            foreach (var hand in hands) {
                if (!hand.CanMove) {
                    hand.Finished = true;
                    continue;
                }
                hand.Move();
            }
            if (log) {
                Test.Log(string.Join("  ", hands));
            }
            for (var i = 0; i < hands.Length; i++) {
                var iHand = hands[i];
                for (int j = i + 1; j < hands.Length; j++) {
                    if (iHand.Meets(hands[j])) {
                        value++;
                        if (log) {
                            Test.Log($"  {i + 1} and {j + 1} meet: {value}");
                        }
                    }
                }
            }
            foreach (var hand in hands) {
                hand.EndTurn();
            }
        }
        Test.Log($"meetings {value}");
        return value;
    }
}

class Hand(int count, int direction, int currentIndex, long moves) {
    readonly int direction = direction;
    int currentIndex = currentIndex;
    float Current => currentIndex / (float)count;
    float CurrentWrapped => Mathf.Wrap(currentIndex, count) / (float)count;
    float previous = currentIndex / (float)count;
    long moves = moves;
    public bool Finished { get; set; }
    public bool CanMove => moves > 0;

    public void Move() {
        currentIndex += direction;
        moves--;
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
        currentIndex = Mathf.Wrap(currentIndex, count);
        previous = Current;
    }

    public override string ToString() => $"{previous:F} {(!Finished ? "=>" : "ST")} {Current:F}";
}

readonly struct Interval {
    readonly float start, end;

    public Interval(float a, float b) => (start, end) = a < b ? (a, b) : (b, a);

    public bool Intersects(Interval other) => start <= other.end && other.start <= end;

    public bool Contains(Interval other) => start <= other.start && other.end <= end;
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
