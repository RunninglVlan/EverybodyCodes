using System;
using System.IO;
using System.Linq;

// https://everybody.codes/story/3/quests/1
const string inputFile = "../../Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = """
                            2456:rrrrrr ggGgGG bbbbBB sSsSsS
                            7689:rrRrrr ggGggg bbbBBB ssSSss
                            3145:rrRrRr gggGgg bbbbBB sSsSsS
                            6710:rrrRRr ggGGGg bbBBbB ssSSss
                            """;
// input = inputExample;
var scales = input.Split(Environment.NewLine).Select(it => {
    string[] scaleParts = it.Split(':');
    int id = int.Parse(scaleParts.First());
    string[] colorComponents = scaleParts.Last().Split(' ');
    int[] colorValues = [.. colorComponents.Select(ToValue)];
    return new Scale(id, colorValues);
});
Console.WriteLine(DarkestShinyScale());
return;

int SumOfGreenDominantScales() {
    return scales.Where(Scale.IsGreenDominant).Sum(it => it.Id);
}

int DarkestShinyScale() {
    int maxShininess = scales.Max(it => it.ColorValues[Scale.SHININESS]);
    var darkestShinyScale = scales
            .Where(it => it.ColorValues[Scale.SHININESS] == maxShininess)
            .MinBy(it => it.ColorValues.SkipLast(1).Sum());
    return darkestShinyScale.Id;
}

int ToValue(string component) {
    char[] binary = [.. component.Select(it => char.IsUpper(it) ? '1' : '0')];
    return Convert.ToInt32(new string(binary), 2);
}

readonly record struct Scale(int Id, int[] ColorValues) {
    const int GREEN = 1;
    public const int SHININESS = 3;

    public static bool IsGreenDominant(Scale scale) {
        var dominantColor = scale.ColorValues
                .Select((it, i) => (value: it, i))
                .MaxBy(it => it.value);
        return dominantColor.i == GREEN;
    }
}
