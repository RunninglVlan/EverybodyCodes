// https://everybody.codes/event/2026/quests/13

const string noteFile = "../Notes.txt";
string notes = File.ReadAllText(noteFile);
string[] data = notes.Split(Environment.NewLine);
var signals = 0L;
var clock = new PortalClock();
for (var i = 0; i < data.Length; i++) {
    clock.Read((data[i++], data[i++], data[i++], data[i++]));
    signals += clock.Calibrate();
}
Console.WriteLine(signals);

class PortalClock {
    const int HANDS = 3;

    readonly Hand[] hands = new Hand[HANDS];
    readonly Dictionary<int[], (long value, long[] moves)> pastHands = new(ArrayComparer<int>.Instance);

    public void Read((string counts, string directions, string positions, string moves) data) {
        int[] counts = [.. Split(data.counts).Select(int.Parse)];
        int[] directions = [.. Split(data.directions).Select(it => it == "CW" ? 1 : -1)];
        int[] positions = [.. Split(data.positions).Select(int.Parse)];
        long[] moves = [.. Split(data.moves).Select(long.Parse)];
        for (var i = 0; i < hands.Length; i++) {
            hands[i] = new Hand(counts[i], directions[i], positions[i], moves[i]);
        }
        pastHands.Clear();
        return;

        string[] Split(string row) => row.Split(' ').Last().Split(':');
    }

    public long Calibrate() {
        var value = 0L;
        while (hands.Any(it => it.CanMove)) {
            (int[] current, long[] currentMoves) = Move();
            for (var i = 0; i < hands.Length; i++) {
                var hand = hands[i];
                for (int j = i + 1; j < hands.Length; j++) {
                    if (hand.Meets(hands[j])) {
                        value++;
                    }
                }
                hand.EndTurn();
            }
            if (pastHands.TryGetValue(current, out var past)) {
                value += SkipMoves(past.moves, value - past.value);
                pastHands.Clear();
                continue;
            }
            pastHands.Add(current, (value, currentMoves));
        }
        return value;
    }

    (int[] current, long[] currentMoves) Move() {
        var current = new int[HANDS];
        var currentMoves = new long[HANDS];
        for (var i = 0; i < hands.Length; i++) {
            var hand = hands[i];
            if (!hand.CanMove) {
                hand.Finished = true;
                continue;
            }
            hand.Move();
            current[i] = hand.CurrentIndex;
            currentMoves[i] = hand.Moves;
        }
        return (current, currentMoves);
    }

    long SkipMoves(long[] pastMoves, long valueDelta) {
        var moveDiffs = new long[hands.Length];
        var skipTimes = long.MaxValue;
        for (var i = 0; i < hands.Length; i++) {
            long moves = hands[i].Moves;
            long diff = pastMoves[i] - moves;
            if (diff == 0) {
                continue;
            }
            moveDiffs[i] = diff;
            skipTimes = Math.Min(moves / diff, skipTimes);
        }
        for (var i = 0; i < hands.Length; i++) {
            hands[i].Moves -= moveDiffs[i] * skipTimes;
        }
        return valueDelta * skipTimes;
    }
}

class Hand(int count, int direction, int currentIndex, long moves) {
    readonly int direction = direction;
    float previous = currentIndex / (float)count;
    public int CurrentIndex { get; private set; } = currentIndex;
    float Current => CurrentIndex / (float)count;
    float CurrentWrapped => Mathf.Wrap(CurrentIndex, count) / (float)count;
    public long Moves { get; set; } = moves;
    public bool Finished { get; set; }
    public bool CanMove => Moves > 0;

    public void Move() {
        CurrentIndex += direction;
        Moves--;
    }

    public bool Meets(Hand other) {
        if (Mathf.Approximately(previous, other.previous)) {
            return false;
        }
        if (direction != other.direction || Finished || other.Finished) {
            if (IntervalsIntersect(other)) {
                return true;
            }
        } else if (direction == other.direction && AnyIntervalContains(other)) {
            return true;
        }
        return Mathf.Approximately(CurrentWrapped, other.CurrentWrapped);
    }

    bool IntervalsIntersect(Hand other) {
        return Interval().Intersects(other.Interval());
    }

    bool AnyIntervalContains(Hand other) {
        var interval = Interval();
        var otherInterval = other.Interval();
        return interval.Contains(otherInterval) || otherInterval.Contains(interval);
    }

    Interval Interval() {
        if (Current < 0 && Mathf.Approximately(previous, 0)) {
            return new Interval(previous + 1, Current + 1);
        }
        return new Interval(previous, Current);
    }

    public void EndTurn() {
        CurrentIndex = Mathf.Wrap(CurrentIndex, count);
        previous = Current;
    }

    public override string ToString() => $"{previous:F} {(!Finished ? "=>" : "ST")} {Current:F}";
}

readonly struct Interval {
    readonly float min, max;

    public Interval(float a, float b) => (min, max) = a < b ? (a, b) : (b, a);

    public bool Intersects(Interval other) => min <= other.max && other.min <= max;
    public bool Contains(Interval other) => min <= other.min && other.max <= max;
}

sealed class ArrayComparer<T> : IEqualityComparer<T[]> {
    public static readonly ArrayComparer<T> Instance = new();

    public bool Equals(T[]? a, T[]? b) {
        return a == b || a != null && b != null && a.SequenceEqual(b);
    }

    public int GetHashCode(T[] items) {
        var hash = new HashCode();
        foreach (var item in items) {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

static class Mathf {
    public static bool Approximately(double a, double b) => Math.Abs(a - b) < 0.001;

    public static int Wrap(int value, int max) {
        int remainder = value % max;
        return remainder < 0 ? remainder + max : remainder;
    }
}
