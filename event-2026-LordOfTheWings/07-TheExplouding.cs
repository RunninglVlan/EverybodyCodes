using System.Diagnostics;

Assert.Throw = true;
// https://everybody.codes/event/2026/quests/7
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 1;
var queue = new Queue<(Cloud, int)>();
var reached = new HashSet<Cloud>();
Test.Run((notes, part, expected) => {
    var clouds = notes.Split(Environment.NewLine).Select(Cloud.From).ToDictionary(it => it.Id, it => it);
    var start = clouds.Values.MaxBy(it => it.Connections.Length)!;
    Propagate(start, part switch {
        1 => 5,
        _ => 10
    });
    while (queue.TryDequeue(out var currentItem)) {
        (var current, int impulse) = currentItem;
        impulse--;
        foreach (var next in current.Connections.Select(id => clouds[id])) {
            Propagate(next, impulse);
        }
    }
    int totalEnergy = clouds.Values.Select(it => it.Energy).Sum();
    Assert.ActualEqualsExpected(totalEnergy, expected);
});
return;

void Propagate(Cloud cloud, int impulse) {
    cloud.Energy += impulse;
    if (reached.Add(cloud)) {
        queue.Enqueue((cloud, impulse));
    }
}

record Cloud(int Id, int[] Connections) {
    public int Energy { get; set; }

    public static Cloud From(string cloud) {
        string[] data = cloud.Split(", ");
        string id = data[0].Split('=').Last();
        string energy = data[1].Split('=').Last();
        int[] connections = [.. data[2].Split('=').Last().Split(',').Select(int.Parse)];
        return new Cloud(int.Parse(id), connections) { Energy = int.Parse(energy) };
    }
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
