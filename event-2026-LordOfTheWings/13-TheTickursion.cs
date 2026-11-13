using System.Diagnostics;

// https://everybody.codes/event/2026/quests/13
Test.Run((notes, part, expected) => {
    var clock = new PortalClock();
    clock.Read(notes);
    int signals = clock.Calibrate();
    Assert.ActualEqualsExpected(signals, expected);
});

class PortalClock {
    readonly Hand[] hands = new Hand[3];

    public void Read(string notes) {
        string[] data = notes.Split(Environment.NewLine);
        int[] counts = [.. Split(data[0]).Select(int.Parse)];
        int[] directions = [.. Split(data[1]).Select(it => it == "CW" ? 1 : -1)];
        int[] positions = [.. Split(data[2]).Select(int.Parse)];
        int[] moves = [.. Split(data[3]).Select(int.Parse)];
        for (var i = 0; i < hands.Length; i++) {
            hands[i] = new Hand(counts[i], directions[i], positions[i], moves[i]);
        }
        return;

        string[] Split(string row) => row.Split(' ').Last().Split(':');
    }

    public int Calibrate() {
        var value = 0;
        while (hands.Any(it => it.CanMove)) {
            foreach (var hand in hands) {
                if (!hand.CanMove) {
                    continue;
                }
                hand.Move();
            }
            Test.Log(string.Join("  ", hands));
            for (var i = 0; i < hands.Length; i++) {
                var iHand = hands[i];
                for (int j = i + 1; j < hands.Length; j++) {
                    if (iHand.Meets(hands[j])) {
                        value++;
                        Test.Log($"  {i + 1} and {j + 1} meet: {value}");
                    }
                }
            }
            foreach (var hand in hands) {
                hand.EndTurn();
            }
        }
        return value;
    }
}

class Hand(int count, int direction, int currentIndex, int moves) {
    readonly int direction = direction;
    int currentIndex = currentIndex;
    float Current => currentIndex / (float)count;
    float CurrentWrapped => Mathf.Wrap(currentIndex, count) / (float)count;
    float previous = currentIndex / (float)count;
    int moves = moves;
    public bool CanMove => moves > 0;

    public void Move() {
        currentIndex += direction;
        moves--;
    }

    public bool Meets(Hand other) {
        if (Mathf.Approximately(previous, other.previous)) {
            return false;
        }
        var interval = new Interval(previous, Current);
        var otherInterval = new Interval(other.previous, other.Current);
        if (direction != other.direction || moves == 0 || other.moves == 0) {
            if (interval.Intersects(otherInterval)) {
                return true;
            }
        } else if (direction == other.direction
                && (interval.Contains(otherInterval) || otherInterval.Contains(interval))) {
            return true;
        }
        return Mathf.Approximately(CurrentWrapped, other.CurrentWrapped);
    }

    public void EndTurn() {
        currentIndex = Mathf.Wrap(currentIndex, count);
        previous = Current;
    }

    public override string ToString() => $"{previous:F} {(moves > 0 ? "=>" : "ST")} {Current:F}";
}

readonly struct Interval {
    readonly float start, end;

    public Interval(float a, float b) => (start, end) = a < b ? (a, b) : (b, a);

    public bool Intersects(Interval other) => start <= other.end && other.start <= end;

    public bool Contains(Interval other) => start <= other.start && other.end <= end;
}

static class Mathf {
    public static bool Approximately(double a, double b) => Math.Abs(a - b) < 0.001;
    public static int Wrap(int value, int max) => (value + max) % max;
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
