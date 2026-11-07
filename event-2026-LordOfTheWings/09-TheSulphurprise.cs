using System.Diagnostics;

Test.StepEnabled = false;
Test.From = "Part II";
Test.Until = "Part III";
// Test.LogEnabled = false;
// https://everybody.codes/event/2026/quests/9
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 1;
Test.Run((notes, part, expected) => {
    Func<List<long>, int, long> count = part switch {
        1 => (branches, branchingPoints) => branches.Sum() + branchingPoints,
        _ => (branches, branchingPoints) => branches.Count - branchingPoints
    };
    long totalTime = notes.Split(Environment.NewLine).Select(Plant.From).Sum(it => it.Grow(count));
    Assert.ActualEqualsExpected(totalTime, expected);
    // Console.WriteLine(totalTime);
});

record Plant(long Growth, long Limit) {
    public static Plant From(string properties) {
        string[] data = properties.Split(", ");
        string growth = data[1].Split('=').Last();
        string limit = data[2].Split('=').Last();
        return new Plant(long.Parse(growth), long.Parse(limit));
    }

    public long Grow(Func<List<long>, int, long> countElements) {
        List<long> branches = [0];
        var branchingPoints = 0;
        var seconds = 1;
        while (true) {
            int count = branches.Count;
            for (var i = 0; i < count; i++) {
                branches[i]++;
                if (branches[i] < Growth) {
                    continue;
                }
                long splitSize = (branches[i] - 1) / 2;
                branches[i] = splitSize;
                branches.Add(splitSize);
                branches.Add(1);
                branchingPoints++;
            }
            Test.Log($"seconds: {seconds}, branches: {branches.Count}, elements: {Elements()}");
            if (Elements() >= Limit) {
                break;
            }
            seconds++;
        }
        Test.Log();
        return seconds;

        long Elements() => countElements(branches, branchingPoints);
    }
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    static readonly TextReader consoleIn = Console.In;
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
        var current = Console.In;
        Console.SetIn(consoleIn);
        Console.ReadLine();
        Console.SetIn(current);
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
