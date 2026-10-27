using System;
using System.IO;
using System.Linq;

// https://everybody.codes/event/2026/quests/1
const string inputFile = "Input.txt";
string input = File.ReadAllText(inputFile);
int[] outpostPineCones = [.. input.Split(',').Select(int.Parse)];
Console.WriteLine(RequiredPineCones(new Formation(1)));
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

class Formation(int start) {
    int counter = start;

    public int Value => counter * counter;

    public void Next() => counter++;
}
