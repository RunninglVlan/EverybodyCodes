using System.Diagnostics.CodeAnalysis;

// https://everybody.codes/story/3/quests/2
const string inputFile = "../Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = """
                            .......
                            .......
                            .......
                            .#.@...
                            .......
                            .......
                            .......
                            """;
// input = inputExample;
var grid = new Grid();
grid.Read(input);
Console.WriteLine(grid.CountSteps(grid.ReachedVocalBone));

class Grid {
    static readonly Point[] DIRECTIONS = [Screen.UP, Screen.RIGHT, Screen.DOWN, Screen.LEFT];

    Point soundSource, vocalBone;

    [SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
    public void Read(string input) {
        string[] rows = input.Split(Environment.NewLine);
        int width = rows[0].Length, height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                if (rows[y][x] == Cells.SOUND_SOURCE) {
                    soundSource = new Point(x, y);
                } else if (rows[y][x] == Cells.VOCAL_BONE) {
                    vocalBone = new Point(x, y);
                }
            }
        }
    }

    public int CountSteps(Predicate<Point> endPredicate) {
        var current = soundSource;
        HashSet<Point> steps = [current];
        while (true) {
            foreach (var direction in DIRECTIONS) {
                var next = current + direction;
                if (endPredicate(next)) {
                    return steps.Count;
                }
                if (steps.Add(next)) {
                    current = next;
                }
            }
        }
    }

    public bool ReachedVocalBone(Point next) => next == vocalBone;
}

static class Cells {
    public const char SOUND_SOURCE = '@';
    public const char VOCAL_BONE = '#';
}

readonly record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}

static class Screen {
    public static readonly Point UP = new(0, -1);
    public static readonly Point DOWN = new(0, 1);
    public static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);
}
