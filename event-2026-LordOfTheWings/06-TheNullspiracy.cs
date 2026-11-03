using System.Diagnostics;

Assert.Throw = true;
// https://everybody.codes/event/2026/quests/6
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
var part = 1;
Dictionary<string, string[]> ingredients = [];
Test.Run((notes, part, expected) => {
    ingredients.Clear();
    foreach (string[] parts in notes.Split(Environment.NewLine).Select(it => it.Split(':'))) {
        ingredients.Add(parts.First(), parts.Last().Split(','));
    }
    int count = NullIngredients(part switch {
        1 => MadeFrom,
        _ => UsedIn
    });
    Assert.ActualEqualsExpected(count, expected);
});
return;

IEnumerable<string> MadeFrom(string current) {
    if (!ingredients.TryGetValue(current, out string[]? madeFrom)) {
        yield break;
    }
    foreach (string next in madeFrom) {
        yield return next;
    }
}

IEnumerable<string> UsedIn(string current) {
    foreach ((string next, string[] _) in ingredients.Where(it => it.Value.Contains(current))) {
        yield return next;
    }
}

int NullIngredients(Func<string, IEnumerable<string>> nextIngredients) {
    const string nullIngredient = "NULL";
    var queue = new Queue<string>();
    var result = new HashSet<string>();
    foreach (string next in nextIngredients(nullIngredient)) {
        queue.Enqueue(next);
        result.Add(next);
    }
    while (queue.TryDequeue(out string? current)) {
        foreach (string next in nextIngredients(current)) {
            if (next != nullIngredient && result.Add(next)) {
                queue.Enqueue(next);
            }
        }
    }
    return result.Count;
}

static class Test {
    const string CASE_FILE = "../Test.Cases.txt";
    const string LOG_FILE = "../Test.Log.txt";

    static readonly TextReader consoleIn = Console.In;
    public static bool StepEnabled { get; set; } = true;
    public static string? From { get; set; }
    public static string? Until { get; set; }

    public static void Run(Action<string, int, string> action) {
        File.WriteAllText(LOG_FILE, null);
        string cases = ReadCases();
        Console.SetIn(new StringReader(cases));
        long totalTime = 0L;
        while (Console.In.Peek() != -1) {
            var stopwatch = Stopwatch.StartNew();
            string name = Console.ReadLine()!;
            Log(name, console: true, color: ConsoleColor.Cyan);
            int part = int.Parse(Console.ReadLine()!);
            string notes = ReadUntilNewLine();
            string expected = ReadUntilNewLine();
            try {
                action(notes, part, expected);
            }
            finally {
                long time = stopwatch.ElapsedMilliseconds;
                totalTime += time;
                Log($"Test took {time}ms", console: true);
            }
            Log(console: true);
        }
        Log($"All tests took {totalTime}ms", console: true);
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
        bool equals = Equals(actual.ToString(), expected);
        Console.WriteLine(equals ? $"✅ {actual}" : $"❌ found {actual}, but expected {expected}");
        if (!equals && Throw) {
            throw new Exception();
        }
    }
}
