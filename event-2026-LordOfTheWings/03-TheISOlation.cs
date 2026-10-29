using System.Diagnostics;

// Test.StepEnabled = false;
// Test.From = "Part III";
// Test.Until = "Part II";
// https://everybody.codes/event/2026/quests/3
const string noteFile = "../Notes.txt";
var notes = File.ReadAllText(noteFile);
var part = 1;
Test.Run((notes, part, expected) => {
    var votes = notes.Split(Environment.NewLine)
            .Select(Vote.From)
            .Where(it => it.IsValid());
    Assert.ActualEqualsExpected(votes.Sum(it => it.decimalValue), expected);
});

readonly struct Vote {
    readonly bool isISO2;
    readonly string standard, value;
    public readonly long decimalValue;

    public static Vote From(string vote) {
        var parts = vote.Split(' ');
        var standard = parts.First().Split('-');
        var isISO2 = standard.First().Contains('2');
        return new Vote(isISO2, standard.Last(), parts.Last());
    }

    Vote(bool isISO2, string standard, string value) {
        this.isISO2 = isISO2;
        this.standard = standard;
        this.value = value;
        for (var i = 1; i <= value.Length; i++) {
            if (i > standard.Length) {
                standard += standard;
            }
            var standardDigit = char.GetNumericValue(standard[^i]);
            var valueDigit = char.GetNumericValue(value[^i]);
            var power = i - 1;
            decimalValue += (long)(valueDigit * Math.Pow(standardDigit, power));
        }
    }

    public bool IsValid() {
        if (!isISO2) {
            return true;
        }
        for (var current = long.Parse(value) - 1; current >= 0; current--) {
            if (new Vote(true, standard, current.ToString()).decimalValue == decimalValue) {
                return false;
            }
        }
        return true;
    }
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    static readonly TextReader consoleIn = Console.In;
    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }

    public static void Run(Action<string, int, long> action) {
        File.WriteAllText(LOG_FILE, null);
        var cases = ReadCases();
        Console.SetIn(new StringReader(cases));
        var totalTime = 0L;
        while (Console.In.Peek() != -1) {
            var stopwatch = Stopwatch.StartNew();
            var name = Console.ReadLine()!;
            Log(name, console: true, color: ConsoleColor.Cyan);
            var part = int.Parse(Console.ReadLine()!);
            var notes = ReadUntilNewLine();
            var expected = long.Parse(ReadUntilNewLine());
            try {
                action(notes, part, expected);
            }
            finally {
                var time = stopwatch.ElapsedMilliseconds;
                totalTime += time;
                Log($"Test took {time}ms", console: true);
            }
            Log(console: true);
        }
        Log($"All tests took {totalTime}ms", console: true);
    }

    static string ReadCases() {
        var cases = File.ReadAllText(CASE_FILE);
        var lines = cases.ReplaceLineEndings(Environment.NewLine).Split(Environment.NewLine);
        if (From != null) {
            var start = Array.IndexOf(lines, From);
            if (start < 0) {
                throw new InvalidOperationException($"Line '{From}' not found");
            }
            lines = lines[start..];
        }
        if (Until != null) {
            var end = Array.IndexOf(lines, Until);
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
    public static bool Throw { get; set; }

    public static void ActualEqualsExpected(object actual, object expected) {
        var equals = Equals(actual, expected);
        Console.WriteLine(equals ? $"✅ {actual}" : $"❌ found {actual}, but expected {expected}");
        if (!equals && Throw) {
            throw new Exception();
        }
    }
}
