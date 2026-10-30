using System.Diagnostics;

// Test.From = "Part III Example 3";
// Test.Until = "Part III Example 3";
// https://everybody.codes/event/2026/quests/4
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
{
    int part = 3;
    Test.Run((notes, part, expected) => {
        var results = notes.Split(Environment.NewLine).Select(Result.From).ToArray();
        var ranking = part != 3 ? TimeRanking(results) : PointRanking(results);
        int checksum = 0;
        (int place, int score) last = (0, 0);
        int place = 1;
        foreach (int score in ranking) {
            bool isTiedWithPrevious = score == last.score;
            int currentPlace = isTiedWithPrevious ? last.place : place;
            last = (currentPlace, score);
            checksum += score * currentPlace;
            place++;
        }
        Assert.ActualEqualsExpected(checksum, expected);
    });
}
return;

IEnumerable<int> TimeRanking(Result[] allResults) {
    return allResults
            .GroupBy(result => result.Id)
            .Select(results => (parts: results.Count(), totalTime: results.Sum(it => it.Time)))
            .OrderByDescending(it => it.parts)
            .ThenBy(it => it.totalTime)
            .Select(it => it.totalTime);
}

IEnumerable<int> PointRanking(Result[] allResults) {
    const int pointCoefficient = 50;
    int ids = allResults.Select(it => it.Id).Max();
    int[] scores = new int[ids];
    int quests = allResults.Select(it => it.Quest).Max();
    int parts = allResults.Select(it => it.Part).Max();
    for (int quest = 1; quest <= quests; quest++) {
        var questResults = allResults.Where(it => it.Quest == quest).ToArray();
        for (int part = 1; part <= parts; part++) {
            Test.Log($"Quest {quest}, Part {part}:");
            var bestResults = questResults
                    .Where(it => it.Part <= part)
                    .GroupBy(it => it.Id)
                    .Where(results => results.Any(it => it.Part == part))
                    .Select(results => (id: results.Key, totalTime: results.Sum(it => it.Time)))
                    .OrderBy(it => it.totalTime)
                    .ToArray();
            int lastPoints = 0;
            for (int i = 0, points = pointCoefficient * part; i < bestResults.Length; i++, points--) {
                var result = bestResults[i];
                bool isTiedWithPrevious = i > 0 && result.totalTime == bestResults[i - 1].totalTime;
                int currentPoints = isTiedWithPrevious ? lastPoints : points;
                if (currentPoints <= 0) {
                    break;
                }
                Test.Log($"id={result.id}, time={result.totalTime}, points={currentPoints}");
                scores[result.id - 1] += currentPoints;
                lastPoints = currentPoints;
            }
            Test.Log();
        }
    }
    {
        Test.Log("Final Ranking:");
        int i = 1;
        foreach (int score in scores.OrderByDescending(it => it)) {
            // Test.Log($"{i++}. points={score}");
        }
    }
    return scores.OrderByDescending(it => it);
}

readonly record struct Result(int Id, int Quest, int Part, int Time) {
    public static Result From(string result) {
        string[] parts = result.Split(' ');
        string id = parts[0].Split('=').Last();
        string quest = parts[1].Split('=').Last();
        string part = parts[2].Split('=').Last();
        string time = parts[3].Split('=').Last();
        return new Result(int.Parse(id), int.Parse(quest), int.Parse(part), int.Parse(time));
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
