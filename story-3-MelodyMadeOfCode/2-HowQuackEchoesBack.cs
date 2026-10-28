// https://everybody.codes/story/3/quests/2

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
var grid = new Grid();
grid.Read(notes);
int steps = grid.CountSteps(part switch {
    1 => ReachVocalBone(),
    2 => SurroundVocalBone(),
    _ => SurroundVocalBones()
});
Console.WriteLine(steps);
return;

Part ReachVocalBone() => new(cell => IsEmpty(cell) || IsVocalBone(cell), IsVocalBone);
Part SurroundVocalBone() => new(IsEmpty, grid.SurroundedVocalBone);
Part SurroundVocalBones() => new(IsEmpty, grid.SurroundedVocalBones, 3);
bool IsEmpty(char cell) => cell == Cells.EMPTY;
bool IsVocalBone(char cell) => cell == Cells.VOCAL_BONE;

readonly record struct Part(Predicate<char> IsValid, Predicate<char> IsEnd, int Times = 1);

class Grid {
    static readonly Point[] DIRECTIONS = [Screen.UP, Screen.RIGHT, Screen.DOWN, Screen.LEFT];

    readonly Dictionary<Point, char> cells = [];

    public void Read(string input) {
        cells.Clear();
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
        foreach (var bone in cells.Where(it => it.Value == Cells.VOCAL_BONE).Select(it => it.Key).ToArray()) {
            FillSurrounded(bone);
        }
    }

    public int CountSteps(Part part) {
        var soundSource = cells.First(it => it.Value == Cells.SOUND_SOURCE).Key;
        cells[soundSource] = Cells.SOUND_WAVE;
        var current = soundSource;
        var steps = 0;
        while (true) {
            foreach (var direction in DIRECTIONS) {
                for (var _ = 0; _ < part.Times; _++) {
                    var next = current + direction;
                    char nextCell = cells.GetValueOrDefault(next, Cells.EMPTY);
                    if (!part.IsValid(nextCell)) {
                        break;
                    }
                    cells[next] = Cells.SOUND_WAVE;
                    current = next;
                    steps++;
                    FillSurrounded(current);
                    if (part.IsEnd(nextCell)) {
                        return steps;
                    }
                }
            }
        }
    }

    void FillSurrounded(Point current) {
        foreach (var direction in DIRECTIONS) {
            var neighbor = current + direction;
            char neighborCell = cells.GetValueOrDefault(neighbor, Cells.EMPTY);
            if (neighborCell != Cells.EMPTY || !Surrounded(neighbor, out var toFill)) {
                continue;
            }
            foreach (var point in toFill) {
                cells.Add(point, Cells.SOUND_WAVE);
            }
        }
    }

    public bool SurroundedVocalBone(char cell) {
        var vocalBone = cells.First(it => it.Value == Cells.VOCAL_BONE).Key;
        return Surrounded(vocalBone, out _);
    }

    public bool SurroundedVocalBones(char cell) {
        var vocalBones = cells.Where(it => it.Value == Cells.VOCAL_BONE);
        return vocalBones.All(it => Surrounded(it.Key, out _));
    }

    bool Surrounded(Point point, out HashSet<Point> toFill) {
        toFill = [point];
        int obstacles = DIRECTIONS
                .Select(it => cells.GetValueOrDefault(point + it, Cells.EMPTY))
                .Count(neighbor => neighbor != Cells.EMPTY);
        return obstacles == 4 || ConfirmSurrounded(point, out toFill);
    }

    bool ConfirmSurrounded(Point point, out HashSet<Point> toFill) {
        const int max = 100;
        Queue<Point> queue = new();
        var visited = toFill = [point];
        queue.Enqueue(point);
        while (queue.TryDequeue(out var current)) {
            foreach (var direction in DIRECTIONS) {
                var next = current + direction;
                char nextCell = cells.GetValueOrDefault(next, Cells.EMPTY);
                if (nextCell != Cells.EMPTY) {
                    continue;
                }
                if (visited.Add(next)) {
                    queue.Enqueue(next);
                }
                if (visited.Count == max) {
                    return false;
                }
            }
        }
        return visited.Count < max;
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
