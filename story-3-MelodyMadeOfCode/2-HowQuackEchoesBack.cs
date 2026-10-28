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
Console.WriteLine(grid.CountSteps(Predicates.ReachVocalBone()));

readonly record struct Predicates(Predicate<char> IsValid, Predicate<char> IsEnd) {
    public static Predicates ReachVocalBone() {
        return new Predicates(cell => cell == Cells.EMPTY || IsVocalBone(cell), IsVocalBone);
    }

    static bool IsVocalBone(char cell) => cell == Cells.VOCAL_BONE;
}

class Grid {
    static readonly Point[] DIRECTIONS = [Screen.UP, Screen.RIGHT, Screen.DOWN, Screen.LEFT];

    readonly Dictionary<Point, char> cells = [];

    public void Read(string input) {
        string[] rows = input.Split(Environment.NewLine);
        int width = rows[0].Length, height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char cell = rows[y][x];
                if (cell is not (Cells.SOUND_SOURCE or Cells.VOCAL_BONE)) {
                    continue;
                }
                cells.Add(new Point(x, y), cell);
            }
        }
    }

    public int CountSteps(Predicates predicates) {
        var soundSource = cells.First(it => it.Value == Cells.SOUND_SOURCE).Key;
        cells[soundSource] = Cells.SOUND_WAVE;
        var current = soundSource;
        var steps = 0;
        while (true) {
            foreach (var direction in DIRECTIONS) {
                var next = current + direction;
                char nextCell = cells.GetValueOrDefault(next, Cells.EMPTY);
                if (!predicates.IsValid(nextCell)) {
                    continue;
                }
                cells[next] = Cells.SOUND_WAVE;
                current = next;
                steps++;
                if (predicates.IsEnd(nextCell)) {
                    return steps;
                }
            }
        }
    }
}

static class Cells {
    public const char EMPTY = '.';
    public const char SOUND_SOURCE = '@';
    public const char VOCAL_BONE = '#';
    public const char SOUND_WAVE = '+';
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
