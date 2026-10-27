using System;
using System.IO;
using System.Linq;

// https://everybody.codes/event/2026/quests/1
const string inputFile = "Input.txt";
string input = File.ReadAllText(inputFile);
int[] outpostPineCones = [.. input.Split(',').Select(int.Parse)];
Console.WriteLine(RequiredPineCones());
return;

int RequiredPineCones() {
    var required = 0;
    var squareNumber = 0;
    int square = NextSquare();
    foreach (int pineCones in outpostPineCones) {
        if (pineCones == square) {
            continue;
        }
        while (pineCones > square) {
            square = NextSquare();
        }
        required += square - pineCones;
    }
    return required;

    int NextSquare() {
        squareNumber++;
        return squareNumber * squareNumber;
    }
}
