using System.Diagnostics.CodeAnalysis;

// https://everybody.codes/event/2026/quests/8
const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
const int part = 3;
var grid = new HexGrid();
grid.Init(notes);
(int rounds, bool grow) = part switch {
    1 => (1, false),
    2 => (6, false),
    _ => (int.MaxValue, true)
};
int energy = grid.CalculateEnergy(rounds, grow);
Console.WriteLine(energy);

class HexGrid {
    const int ARMY_WARRIORS = 100_000;

    int width, height;
    readonly Dictionary<Point, Ent> ents = [];
    readonly Point[] starts = new Point[6];
    readonly Point[] sourceDirections = new Point[6];
    readonly Point[][] startDirections = new Point[6][];
    int energy;
    int warriors;

    public void Init(string notes) {
        string[] rows = notes.Split(Environment.NewLine);
        width = rows.First().Length;
        height = rows.Length;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                char plot = rows[y][x];
                if (plot == '.') {
                    continue;
                }
                ents[new Point(x, y)] = new Ent((int)char.GetNumericValue(plot));
            }
        }
        for (var i = 0; i < 6; i++) {
            InitRoundValues(i);
        }
    }

    void InitRoundValues(int round) {
        switch (round) {
            case 0:
                starts[round] = new Point(0, height / 2);
                sourceDirections[round] = Screen.BOTTOM_RIGHT;
                startDirections[round] = [Screen.TOP_RIGHT, Screen.HEX_RIGHT];
                break;
            case 1:
                starts[round] = new Point(width / 4, 0);
                sourceDirections[round] = Screen.BOTTOM_LEFT;
                startDirections[round] = [Screen.HEX_RIGHT, Screen.BOTTOM_RIGHT];
                break;
            case 2:
                starts[round] = new Point(width * 3 / 4, 0);
                sourceDirections[round] = Screen.HEX_LEFT;
                startDirections[round] = [Screen.BOTTOM_RIGHT, Screen.BOTTOM_LEFT];
                break;
            case 3:
                starts[round] = new Point(width - 1, height / 2);
                sourceDirections[round] = Screen.TOP_LEFT;
                startDirections[round] = [Screen.BOTTOM_LEFT, Screen.HEX_LEFT];
                break;
            case 4:
                starts[round] = new Point(width * 3 / 4, height - 1);
                sourceDirections[round] = Screen.TOP_RIGHT;
                startDirections[round] = [Screen.HEX_LEFT, Screen.TOP_LEFT];
                break;
            default:
                starts[round] = new Point(width / 4, height - 1);
                sourceDirections[round] = Screen.HEX_RIGHT;
                startDirections[round] = [Screen.TOP_LEFT, Screen.TOP_RIGHT];
                break;
        }
    }

    public int CalculateEnergy(int rounds, bool grow) {
        for (var round = 0; round < rounds; round++) {
            int i = round % 6;
            var start = starts[i];
            var sourceDirection = sourceDirections[i];
            do {
                for (var current = start; ents.TryGetValue(current, out var ent); current += sourceDirection) {
                    if (ent.IsSeed || ent.Height <= ent.Shadow) {
                        continue;
                    }
                    ent.GotEnergy = true;
                    CastShadow(ent.Height, current, sourceDirection);
                }
            } while (TryNextStart(ref start, startDirections[i]));
            EndRound(grow);
            if (warriors >= ARMY_WARRIORS) {
                break;
            }
        }
        return energy;
    }

    void CastShadow(int entHeight, Point current, Point direction) {
        for (int shadow = entHeight - 1; shadow > 0; shadow--) {
            current += direction;
            if (!ents.TryGetValue(current, out var ent)) {
                break;
            }
            ent.Shadow = Math.Max(shadow, ent.Shadow);
        }
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    bool TryNextStart(ref Point start, Point[] directions) {
        foreach (var direction in directions) {
            var startNext = start + direction;
            if (ents.ContainsKey(startNext)) {
                start = startNext;
                return true;
            }
        }
        return false;
    }

    void EndRound(bool grow) {
        foreach (var ent in ents.Values) {
            if (ent.GotEnergy) {
                energy++;
            }
            if (grow) {
                TryGrow(ent);
            }
            ent.Shadow = 0;
            ent.GotEnergy = false;
        }
        return;

        void TryGrow(Ent ent) {
            if (ent is { GotEnergy: false, IsSeed: false }) {
                return;
            }
            ent.Height++;
            if (ent.Height < Ent.WARRIOR_HEIGHT) {
                return;
            }
            warriors++;
            ent.Height = -Ent.SEED_ROUNDS + 1;
        }
    }
}

class Ent(int height) {
    public const int WARRIOR_HEIGHT = 10;
    public const int SEED_ROUNDS = 3;

    public int Height { get; set; } = height;
    public int Shadow { get; set; }
    public bool GotEnergy { get; set; }
    public bool IsSeed => Height <= 0;
}

readonly record struct Point(int X, int Y) {
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}

static class Screen {
    public static readonly Point TOP_LEFT = new(-1, -1);
    public static readonly Point TOP_RIGHT = new(1, -1);
    public static readonly Point HEX_LEFT = new(-2, 0);
    public static readonly Point HEX_RIGHT = new(2, 0);
    public static readonly Point BOTTOM_LEFT = new(-1, 1);
    public static readonly Point BOTTOM_RIGHT = new(1, 1);
}
