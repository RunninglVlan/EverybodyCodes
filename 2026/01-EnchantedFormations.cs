using System;
using System.IO;
using System.Linq;

// https://everybody.codes/event/2026/quests/1
const string inputFile = "Input.txt";
string input = File.ReadAllText(inputFile);
// input = "3,11,12,16,24,25";
int[] outpostPineCones = [.. input.Split(',').Select(int.Parse)];
Console.WriteLine(RequiredPineCones(new SquareFrame()));
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
    protected int counter = start;
    public abstract int Value { get; }

    public void Next() => counter++;
}

class FilledSquare() : Formation(1) {
    public override int Value => counter * counter;
}

class SquareFrame() : Formation(3) {
    public override int Value {
        get {
            int filled = counter * counter;
            int emptyCounter = counter - 2;
            int empty = emptyCounter * emptyCounter;
            return filled - empty;
        }
    }
}
