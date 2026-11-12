using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// Test.From = "Part III Example 1";
// Test.Until = "Part III Example 1";
// https://everybody.codes/event/2026/quests/12
Test.Run((notes, part, expected) => {
    var grid = new Grid();
    var magmaStacks = grid.ReadAttack(notes);
    int spells = magmaStacks.Sum(it => it.Reflect());
    Assert.ActualEqualsExpected(spells, expected);
});

class Grid {
    const char MAGMA = '#';

    string[] rows = null!;
    int width;

    public List<Stack> ReadAttack(string notes) {
        List<Stack> stacks = [];
        rows = notes.Split(Environment.NewLine);
        width = rows.First().Length;
        int height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width;) {
                char cell = rows[y][x];
                var point = new Point(x, y);
                if (cell != MAGMA) {
                    x++;
                    continue;
                }
                var stackLine = FindLineAt(point);
                var startNeighbor = stackLine.Min + Screen.UP;
                var endNeighbor = stackLine.Max + Screen.TOP_LEFT + Screen.UP;
                var stack = stacks.FirstOrDefault(it => it.Contains(startNeighbor, endNeighbor));
                if (stack == null) {
                    stacks.Add(stack = new Stack([]));
                }
                stack.Add(stackLine);
                x += stackLine.Size.X;
            }
        }
        return stacks;
    }

    Rect FindLineAt(Point min) {
        var max = min;
        while (max.X < width && rows[max.Y][max.X] == MAGMA) {
            max.X++;
        }
        max.Y++;
        return new Rect(min, max);
    }
}

class Stack(List<Rect> lines) {
    StackType type = StackType.Rect;
    (int min, int max) widths = (int.MaxValue, int.MinValue);
    (int min, int max) heights;

    public bool Contains(Point a, Point b) {
        return lines.Any(it => it.Contains(a) || it.Contains(b));
    }

    [SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
    public void Add(Rect line) {
        int width = line.Size.X;
        widths = (Math.Min(widths.min, width), Math.Max(widths.max, width));
        lines.Add(line);
        heights = (lines.Count(it => it.Size.X == widths.max), lines.Count);
        if (type == StackType.Rect) {
            if (widths.min != widths.max || heights.min != heights.max) {
                type = StackType.LShaped;
            }
        } else if (type == StackType.LShaped) {
            if (lines.Count > 1 && lines[^2].Min.Y == line.Min.Y) {
                type = StackType.RectWithGap;
            }
        }
    }

    public int Reflect() {
        return type switch {
            StackType.Rect => Math.Min(widths.min, heights.min),
            StackType.LShaped => LShaped(),
            // StackType.RectWithGap => expr,
            _ => throw new ArgumentException("Unknown stack type")
        };

        int LShaped() {
            int option1 = Math.Min(widths.max, heights.max);
            int option2 = widths.min + heights.min;
            return Math.Min(option1, option2);
        }
    }
}

enum StackType {
    Rect,
    LShaped,
    RectWithGap
}

readonly record struct Rect(Point Min, Point Max) {
    public Point Size => Max - Min;

    public bool Contains(Point point) {
        (int x, int y) = point;
        return x >= Min.X && y >= Min.Y && x < Max.X && y < Max.Y;
    }
}

record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);

    public override string ToString() => $"({X}, {Y})";
}

static class Screen {
    public static readonly Point UP = new(0, -1);
    public static readonly Point TOP_LEFT = new(-1, -1);
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
