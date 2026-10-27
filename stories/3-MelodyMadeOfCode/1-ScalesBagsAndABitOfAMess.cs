using System;
using System.IO;
using System.Linq;

// https://everybody.codes/story/3/quests/1
const string inputFile = "../../Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = """
                            2456:rrrrrr ggGgGG bbbbBB
                            7689:rrRrrr ggGggg bbbBBB
                            3145:rrRrRr gggGgg bbbbBB
                            6710:rrrRRr ggGGGg bbBBbB
                            """;
// input = inputExample;
var sumOfIdentifiers = 0;
foreach (string scaleData in input.Split(Environment.NewLine)) {
    string[] scaleParts = scaleData.Split(':');
    int identifier = int.Parse(scaleParts.First());
    string[] colorComponents = scaleParts.Last().Split(' ');
    int[] colorValues = [.. colorComponents.Select(ToValue)];
    if (colorValues.Select((it, i) => (value: it, i)).MaxBy(it => it.value).i == 1) {
        sumOfIdentifiers += identifier;
    }
}
Console.WriteLine(sumOfIdentifiers);
return;

int ToValue(string component) {
    char[] binary = [.. component.Select(it => char.IsUpper(it) ? '1' : '0')];
    return Convert.ToInt32(new string(binary), 2);
}
