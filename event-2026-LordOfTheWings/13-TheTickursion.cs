using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// https://everybody.codes/event/2026/quests/13
Test.Run((notes, part, expected) => {
    var clock = new PortalClock();
    clock.Read(notes);
    int signals = clock.Calibrate();
    Assert.ActualEqualsExpected(signals, expected);
});

class PortalClock {
    int[] hands = null!;
    int[] directions = null!;
    int[] current = null!;
    int[] previous = null!;
    int[] moves = null!;

    public void Read(string notes) {
        string[] data = notes.Split(Environment.NewLine);
        hands = [.. Split(data[0]).Select(int.Parse)];
        directions = [.. Split(data[1]).Select(it => it == "CW" ? 1 : -1)];
        current = [.. Split(data[2]).Select(int.Parse)];
        moves = [.. Split(data[3]).Select(int.Parse)];
        return;

        string[] Split(string row) => row.Split(' ').Last().Split(':');
    }

    public int Calibrate() {
        var value = 0;
        Log();
        previous = [.. current];
        while (moves.Any(it => it > 0)) {
            for (var i = 0; i < hands.Length; i++) {
                if (moves[i] <= 0) {
                    continue;
                }
                current[i] = (current[i] + directions[i] + hands[i]) % hands[i];
                moves[i]--;
            }
            Log();
            Test.Log("  " + string.Join("  ", current
                            .Select((it, i) => $"{Range.From(previous[i], it, hands[i])}, {directions[i]}")));
            for (var i = 0; i < hands.Length; i++) {
                for (int j = i + 1; j < hands.Length; j++) {
                    if (Meet(i, j)) {
                        value++;
                        Test.Log($"    {i + 1} and {j + 1} meet");
                    }
                }
            }
            for (var i = 0; i < hands.Length; i++) {
                previous[i] = current[i];
            }
        }
        return value;
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    bool Meet(int i, int j) {
        float iCount = hands[i];
        float jCount = hands[j];
        float iCurrent = current[i] / iCount;
        float jCurrent = current[j] / jCount;
        if (directions[i] != directions[j]) {
            var iRange = Range.From(previous[i], current[i], iCount);
            var jRange = Range.From(previous[j], current[j], jCount);
            if (iRange.Intersects(jRange)) {
                return true;
            }
        }
        return Mathf.Approximately(iCurrent, jCurrent);
    }

    void Log() {
        Test.Log(string.Join("  ", current
                .Select((it, i) => $"{it,2}/{hands[i]} {(float)it / hands[i]:F}")));
    }
}

readonly record struct Range(float Start, float End) {
    public static Range From(int previous, int current, float count) {
        return previous < current
                ? new Range(previous / count, current / count)
                : new Range(current / count, previous / count);
    }

    public bool Intersects(Range other) => Start <= other.End && other.Start <= End;

    public override string ToString() => $"{Start:F}-{End:F}";
}

static class Mathf {
    public static bool Approximately(double a, double b) => Math.Abs(a - b) < 0.001;
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
