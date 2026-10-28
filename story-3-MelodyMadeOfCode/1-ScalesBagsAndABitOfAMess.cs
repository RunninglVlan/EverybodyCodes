// https://everybody.codes/story/3/quests/1

const string inputFile = "../../Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = "15437:rRrrRR gGGGGG BBBBBB sSSSSS";
// input = inputExample;
var scales = input.Split(Environment.NewLine).Select(it => {
    string[] scaleParts = it.Split(':');
    int id = int.Parse(scaleParts.First());
    string[] colorComponents = scaleParts.Last().Split(' ');
    int[] colorValues = [.. colorComponents.Select(Scale.ToValue)];
    return new Scale(id, colorValues);
});
Console.WriteLine(LargestGroupOfSimilarScales());
return;

int SumOfGreenDominantScales() {
    return scales.Where(Scale.IsGreenDominant).Sum(it => it.Id);
}

int DarkestShinyScale() {
    int maxShine = scales.Max(it => it.Shine);
    var darkestShinyScale = scales
            .Where(it => it.Shine == maxShine)
            .MinBy(it => it.ColorValues.SkipLast(1).Sum());
    return darkestShinyScale.Id;
}

int LargestGroupOfSimilarScales() {
    return scales
            .Where(it => it.Matte || it.Shiny)
            .Select(it => (scale: it, maxColor: it.ColorValues.SkipLast(1).Max()))
            .Where(it => it.scale.ColorValues.Count(value => value == it.maxColor) == 1)
            .Select(it => {
                int dominantColor = it.scale.ColorValues.IndexOf(it.maxColor);
                return (id: it.scale.Id, dominantColor, matte: it.scale.Matte);
            })
            .GroupBy(it => (it.dominantColor, it.matte))
            .MaxBy(it => it.Count())!
            .Sum(it => it.id);
}

readonly record struct Scale(int Id, int[] ColorValues) {
    const int GREEN = 1, SHINE = 3;

    public int Shine => ColorValues[SHINE];
    public bool Matte => Shine <= 30;
    public bool Shiny => Shine >= 33;

    public static int ToValue(string color) {
        char[] binary = [.. color.Select(it => char.IsUpper(it) ? '1' : '0')];
        return Convert.ToInt32(new string(binary), 2);
    }

    public static bool IsGreenDominant(Scale scale) {
        var dominantColor = scale.ColorValues
                .Select((it, i) => (value: it, i))
                .MaxBy(it => it.value);
        return dominantColor.i == GREEN;
    }
}
