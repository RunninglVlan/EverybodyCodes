using System.Diagnostics;
using System.Text;

Test.StepEnabled = false;
// Test.From = "Part III";
// Test.Until = "Part III";
Test.LogEnabled = false;
// https://everybody.codes/event/2026/quests/9
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 1;
Test.Run((notes, part, expected) => {
    CountSize count = part switch {
        1 => new CountElements(),
        _ => new CountLeaves()
    };
    long totalTime = notes.Split(Environment.NewLine).Select(Plant.From).Sum(it => it.Grow(count));
    Assert.ActualEqualsExpected(totalTime, expected);
    // Console.WriteLine(totalTime);
});

abstract class CountSize {
    protected Dictionary<long, long> branchSizeCount = null!;
    Dictionary<long, long> branchSizeCountNext = null!;
    protected long branchingPoints;
    public abstract long Value { get; }
    protected long secondsDelta;
    protected long growth;

    public void Init(long growthValue, out long seconds) {
        growth = growthValue;
        branchSizeCount = [];
        branchSizeCountNext = new Dictionary<long, long> { { 1, 1 } };
        branchingPoints = 0;
        seconds = 1;
        AfterGrowth();
    }

    public long Grow() {
        long delta = secondsDelta;
        foreach ((long size, long count) in branchSizeCount) {
            long branchSize = size;
            branchSize += delta;
            if (branchSize < growth) {
                Increase(branchSize, count);
                continue;
            }
            long splitSize = (growth - 1) / 2;
            Increase(splitSize, count * 2);
            Increase(1, count);
            branchingPoints += count;
        }
        AfterGrowth();
        return delta;

        void Increase(long size, long countDelta) {
            long countExisting = branchSizeCountNext.GetValueOrDefault(size);
            branchSizeCountNext[size] = countExisting + countDelta;
        }
    }

    protected virtual void AfterGrowth() {
        (branchSizeCount, branchSizeCountNext) = (branchSizeCountNext, branchSizeCount);
        branchSizeCountNext.Clear();
        secondsDelta = 1;
    }

    public override string ToString() {
        var result = new StringBuilder();
        result.AppendLine(
                $"branches: {branchSizeCount.Values.Sum()}, branchingPoints: {branchingPoints}, size: {Value}");
        foreach ((long size, long count) in branchSizeCount.OrderBy(it => it.Key)) {
            result.AppendLine($"  {count} branches of size: {size}");
        }
        return result.ToString();
    }
}

class CountElements : CountSize {
    public override long Value {
        get {
            long branchElements = branchSizeCount.Sum(it => it.Key * it.Value);
            return branchElements + branchingPoints;
        }
    }
}

class CountLeaves : CountSize {
    public override long Value => branchSizeCount.Values.Sum() - branchingPoints;

    protected override void AfterGrowth() {
        base.AfterGrowth();
        secondsDelta = growth - branchSizeCount.Keys.Max();
    }
}

record Plant(long Growth, long Limit) {
    public static Plant From(string properties) {
        string[] data = properties.Split(", ");
        string growth = data[1].Split('=').Last();
        string limit = data[2].Split('=').Last();
        return new Plant(long.Parse(growth), long.Parse(limit));
    }

    public long Grow(CountSize countSize) {
        countSize.Init(Growth, out long seconds);
        while (countSize.Value < Limit) {
            seconds += countSize.Grow();
            Test.Log($"seconds: {seconds}, {countSize}", console: true);
            Test.Step();
        }
        Test.Log($"{this}, seconds: {seconds}", console: true);
        Test.Log(console: true);
        return seconds;
    }
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
