using System.Diagnostics;

Test.StepEnabled = false;
Test.From = "Part III";
// Test.Until = "Part III";
// Test.LogEnabled = false;
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
    protected long growth;
    public List<long> Branches { get; } = [];
    public int BranchingPoints { get; set; }
    public abstract long Value { get; }

    public void Init(long growthValue) {
        growth = growthValue;
        Branches.Clear();
        Branches.Add(0);
        BranchingPoints = 0;
    }

    public virtual void BeforeGrowth() { }
    public virtual void OnElementChange(long value) { }
    public virtual void AfterGrowth(out long secondsDelta) => secondsDelta = 1;
}

class CountElements : CountSize {
    public override long Value => Branches.Sum() + BranchingPoints;
}

class CountLeaves : CountSize {
    public override long Value => Branches.Count - BranchingPoints;
    long timeTilGrowth;

    public override void BeforeGrowth() {
        timeTilGrowth = int.MaxValue;
    }

    public override void OnElementChange(long value) {
        timeTilGrowth = Math.Min(growth - value, timeTilGrowth);
    }

    public override void AfterGrowth(out long secondsDelta) {
        secondsDelta = timeTilGrowth;
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
        countSize.Init(Growth);
        long seconds = 1, secondsDelta = 1;
        while (true) {
            countSize.BeforeGrowth();
            int count = countSize.Branches.Count;
            for (var i = 0; i < count; i++) {
                countSize.Branches[i] += secondsDelta;
                if (countSize.Branches[i] < Growth) {
                    countSize.OnElementChange(countSize.Branches[i]);
                    continue;
                }
                long splitSize = (countSize.Branches[i] - 1) / 2;
                countSize.Branches[i] = splitSize;
                countSize.OnElementChange(splitSize);
                countSize.Branches.Add(splitSize);
                countSize.Branches.Add(1);
                countSize.BranchingPoints++;
            }
            Test.Log(
                    $"seconds: {seconds}, branches: {countSize.Branches.Count}, branchingPoints: {countSize.BranchingPoints}, size: {countSize.Value}");
            if (countSize.Value >= Limit) {
                break;
            }
            countSize.AfterGrowth(out secondsDelta);
            seconds += secondsDelta;
        }
        Test.Log();
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
