// https://everybody.codes/event/2026/quests/1

const string inputFile = "Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = "3,11,12,16,24,25";
// input = inputExample;
int[] outpostPineCones = [.. input.Split(',').Select(int.Parse)];
Console.WriteLine(RequiredPineCones(new AlmostFilledSquare()));
return;

int RequiredPineCones(Formation formation) {
    var required = 0;
    foreach (int pineCones in outpostPineCones) {
        if (pineCones == formation.Value) {
            continue;
        }
        while (pineCones > formation.Value) {
            formation.Next();
        }
        required += formation.Value - pineCones;
    }
    return required;
}

abstract class Formation(int start) {
    protected int size = start;
    public abstract int Value { get; }

    public virtual void Next() => size++;
}

class FilledSquare() : Formation(1) {
    public override int Value => size * size;
}

class SquareFrame() : Formation(3) {
    public override int Value {
        get {
            int filled = size * size;
            int emptySize = size - 2;
            int empty = emptySize * emptySize;
            return filled - empty;
        }
    }
}

class AlmostFilledSquare() : Formation(3) {
    public override int Value => size * size - 1;

    public override void Next() => size += 2;
}
