// https://everybody.codes/event/2026/quests/14

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
var portal = new PortalGrid();
portal.Read(notes);
int bindings = part switch {
    1 => portal.Repair(),
    2 => portal.RepairOptimally(false),
    _ => portal.RepairOptimally(true)
};
Console.WriteLine(bindings);

class PortalGrid {
    const char FRAME = ' ';

    readonly Dictionary<char, Piece> pieces = [];

    public void Read(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        int width = rows[0].Length, height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var point = new Point(x, y);
                char value = rows[y][x];
                foreach (var direction in Point.Directions4) {
                    var neighborPoint = point + direction;
                    char neighborValue = neighborPoint.InBounds(width, height)
                            ? rows[neighborPoint.Y][neighborPoint.X]
                            : FRAME;
                    if (neighborValue == value) {
                        continue;
                    }
                    var binding = new Binding(point, neighborPoint);
                    Bindings(value, neighborValue).Add(binding);
                    Bindings(neighborValue, value).Add(binding);
                }
            }
        }
    }

    HashSet<Binding> Bindings(char pieceValue, char neighborValue) {
        if (!pieces.TryGetValue(pieceValue, out var piece)) {
            pieces.Add(pieceValue, piece = new Piece([]));
        }
        if (!piece.Bindings.TryGetValue(neighborValue, out var bindings)) {
            piece.Bindings.Add(neighborValue, bindings = []);
        }
        return bindings;
    }

    public int Repair() {
        return pieces.Values.SelectMany(piece => piece.Bindings.Values.SelectMany(it => it)).ToHashSet().Count;
    }

    public int RepairOptimally(bool useCharges) {
        if (!useCharges) {
            return RepairOptimally(new PriorityQueue<char, int>());
        }
        var minBindings = int.MaxValue;
        PriorityQueue<char, int> queue = new();
        char[] pieceValues = [.. pieces.Keys.Where(it => it != FRAME)];
        for (var i = 0; i < pieceValues.Length; i++) {
            for (int j = i + 1; j < pieceValues.Length; j++) {
                queue.Clear();
                Add(pieceValues[i]);
                Add(pieceValues[j]);
                int bindings = RepairOptimally(queue);
                minBindings = Math.Min(bindings, minBindings);
            }
        }
        return minBindings;

        void Add(char value) {
            queue.Enqueue(value, 0);
            foreach (char neighbor in pieces[value].Bindings.Keys) {
                queue.Enqueue(neighbor, 0);
            }
        }
    }

    int RepairOptimally(PriorityQueue<char, int> queue) {
        Dictionary<char, int> connected = [];
        queue.Enqueue(FRAME, 0);
        while (queue.TryDequeue(out char current, out int currentBindings)) {
            if (!connected.TryAdd(current, currentBindings)) {
                continue;
            }
            foreach ((char next, var bindings) in pieces[current].Bindings) {
                queue.Enqueue(next, bindings.Count);
            }
        }
        return connected.Values.Sum();
    }
}

record Piece(Dictionary<char, HashSet<Binding>> Bindings);

readonly record struct Binding(Point A, Point B) {
    public bool Equals(Binding other) => A == other.A && B == other.B || A == other.B && B == other.A;
    public override int GetHashCode() => A.GetHashCode() ^ B.GetHashCode();
}

readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    static readonly Point RIGHT = new(1, 0);

    public static Point[] Directions4 { get; } = [RIGHT, LEFT, UP, DOWN];

    public bool InBounds(int width, int? height = null) {
        return X >= 0 && X < width && Y >= 0 && Y < (height ?? width);
    }

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}
