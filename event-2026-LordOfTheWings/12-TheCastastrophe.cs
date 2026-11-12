using System.Diagnostics;

// Test.From = "Part II Example 1";
// Test.Until = "Part II Example 2";
// https://everybody.codes/event/2026/quests/12
Test.Run((notes, part, expected) => {
    var grid = new Grid();
    var magmaBlocks = grid.ReadAttack(notes);
    int spells = Reflect(magmaBlocks);
    Assert.ActualEqualsExpected(spells, expected);
});
return;

int Reflect(List<List<Rect>> blocks) {
    var spells = 0;
    foreach (var block in blocks) {
        int height = block.Count;
        if (height == 1) {
            spells++;
            continue;
        }
        var widths = block.Select(it => it.Size.X).Aggregate((min: int.MaxValue, max: int.MinValue),
                (current, x) => (Math.Min(current.min, x), Math.Max(current.max, x)));
        if (widths.min == widths.max) {
            spells += Math.Min(widths.min, height);
            continue;
        }
        (int min, int max) heights = (block.Count(it => it.Size.X == widths.max), height);
        int option1 = Math.Min(widths.max, heights.max);
        int option2 = widths.min + heights.min;
        spells += Math.Min(option1, option2);
    }
    return spells;
}

class Grid {
    const char MAGMA = '#';

    string[] rows = null!;
    int width, height;

    public List<List<Rect>> ReadAttack(string notes) {
        List<List<Rect>> blocks = [];
        rows = notes.Split(Environment.NewLine);
        width = rows.First().Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width;) {
                char cell = rows[y][x];
                var point = new Point(x, y);
                if (cell != MAGMA) {
                    x++;
                    continue;
                }
                var rect = blocks.SelectMany(it => it).FirstOrDefault(it => it.Contains(point));
                if (rect == default) {
                    var block = FindBlockAt(point);
                    blocks.Add(block);
                    rect = block.First(it => it.Contains(point));
                }
                x += rect.Size.X;
            }
        }
        return blocks;
    }

    List<Rect> FindBlockAt(Point current) {
        List<Rect> lines = [];
        while (true) {
            var line = FindLineAt(current);
            lines.Add(line);
            if (!TryNext(line, ref current)) {
                break;
            }
        }
        return lines;
    }

    Rect FindLineAt(Point min) {
        var max = min;
        while (max.X < width && rows[max.Y][max.X] == MAGMA) {
            max.X++;
        }
        max.Y++;
        return new Rect(min, max);
    }

    bool TryNext(Rect last, ref Point current) {
        int nextY = current.Y + 1;
        if (nextY >= height) {
            return false;
        }
        current.Y = nextY;
        int nextX = last.Min.X;
        if (rows[nextY][nextX] == MAGMA) {
            for (nextX--; nextX > 0 && rows[nextY][nextX] == MAGMA; nextX--) {
                current.X = nextX;
            }
            return true;
        }
        for (nextX++; nextX < last.Max.X; nextX++) {
            if (rows[nextY][nextX] != MAGMA) {
                continue;
            }
            current.X = nextX;
            return true;
        }
        return false;
    }
}

readonly record struct Rect(Point Min, Point Max) {
    public Point Size => Max - Min;

    public bool Contains(Point point) {
        (int x, int y) = point;
        return x >= Min.X && y >= Min.Y && x < Max.X && y < Max.Y;
    }
}

record struct Point(int X, int Y) {
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);

    public override string ToString() => $"({X}, {Y})";
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
