// https://everybody.codes/event/2026/quests/12

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
var grid = new Grid();
var magmaStacks = grid.ReadAttack(notes);
int spells = magmaStacks.Sum(it => it.Reflect());
Console.WriteLine(spells);

class Grid {
    const char MAGMA = '#';

    string[] rows = null!;
    int width;

    public List<MagmaStack> ReadAttack(string notes) {
        List<MagmaStack> stacks = [];
        rows = notes.Split(Environment.NewLine);
        width = rows.First().Length;
        int height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width;) {
                char cell = rows[y][x];
                if (cell != MAGMA) {
                    x++;
                    continue;
                }
                var stackLine = FindLineAt(new Point(x, y));
                var startNeighbor = stackLine.Min + Screen.UP;
                var endNeighbor = stackLine.Max + Screen.TOP_LEFT + Screen.UP;
                var stack = stacks.FirstOrDefault(it => it.Contains(startNeighbor, endNeighbor));
                if (stack == null) {
                    stacks.Add(stack = new MagmaStack([]));
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

class MagmaStack(List<Rect> lines) {
    StackType type = StackType.Rect;
    (int min, int max) widths = (int.MaxValue, int.MinValue);
    (int min, int max) heights;
    Rect gap;

    public bool Contains(Point a, Point b) {
        return lines.Any(it => it.Contains(a) || it.Contains(b));
    }

    public void Add(Rect line) {
        int width = line.Size.X;
        widths = (Math.Min(widths.min, width), Math.Max(widths.max, width));
        lines.Add(line);
        heights = (lines.Count(it => it.Size.X == widths.max), lines.DistinctBy(it => it.Min.Y).Count());
        switch (type) {
            case StackType.Rect:
                if (widths.min != widths.max || heights.min != heights.max) {
                    type = StackType.LShaped;
                }
                break;
            case StackType.LShaped:
                if (lines.Count <= 1) {
                    break;
                }
                var previous = lines[^2];
                if (previous.Min.Y != line.Min.Y) {
                    break;
                }
                type = StackType.RectWithGap;
                gap = new Rect(previous.Max + Screen.UP, line.Min + Screen.DOWN);
                break;
            case StackType.RectWithGap:
                if (line.Max.X == gap.Min.X) {
                    gap = gap with { Max = gap.Max + Screen.DOWN };
                }
                break;
            default:
                throw new ArgumentException("Unknown stack type");
        }
    }

    public int Reflect() {
        switch (type) {
            case StackType.Rect:
                return Rect(widths.min, heights.min);
            case StackType.LShaped:
                int option1 = Math.Min(widths.max, heights.max);
                int option2 = widths.min + heights.min;
                return Math.Min(option1, option2);
            case StackType.RectWithGap:
                int width = widths.max, height = heights.max;
                int horizontalLines = height - gap.Size.Y;
                int horizontalRect1 = Rect(widths.min, gap.Size.Y);
                int horizontalRect2 = Rect(width - widths.min - gap.Size.X, gap.Size.Y);
                int horizontal = horizontalLines + horizontalRect1 + horizontalRect2;
                int verticalLines = width - gap.Size.X;
                int verticalRect1 = Rect(gap.Size.X, gap.Min.Y - lines.First().Min.Y);
                int verticalRect2 = Rect(gap.Size.X, lines.Last().Max.Y - gap.Max.Y);
                int vertical = verticalLines + verticalRect1 + verticalRect2;
                return Math.Min(horizontal, vertical);
            default:
                throw new ArgumentException("Unknown stack type");
        }

        int Rect(int width, int height) => Math.Min(width, height);
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
}

static class Screen {
    public static readonly Point UP = new(0, -1);
    public static readonly Point DOWN = new(0, 1);
    public static readonly Point TOP_LEFT = new(-1, -1);
}
