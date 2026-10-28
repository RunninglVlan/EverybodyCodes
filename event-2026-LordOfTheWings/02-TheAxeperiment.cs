// https://everybody.codes/event/2026/quests/2

const string inputFile = "../Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = """
                            3>1 1>2 1<5 4<2 3>5
                            2>1 2<5 3>1 4>3 2<4 5<3
                            5>1 3<4 1>2 3>5
                            """;
// input = inputExample;
string[] alloyGroups = input.Split(Environment.NewLine);
long checksum = alloyGroups.Select(AlloyNumber).Sum();
Console.WriteLine(checksum);
return;

// Orders alloys from hardest to softest
long AlloyNumber(string group) {
    var alloyPairs = group.Split(' ').Select(AlloyPair.From).ToArray();
    HashSet<char> alloys = [.. alloyPairs.SelectMany(it => it.All())];
    var number = string.Empty;
    while (alloys.Count > 0) {
        char hardest = alloys.First(alloy => !alloyPairs.Any(it => alloy == it.Softer && !number.Contains(it.Harder)));
        number += hardest;
        alloys.Remove(hardest);
    }
    return int.Parse(number);
}

readonly record struct AlloyPair(char Harder, char Softer) {
    public static AlloyPair From(string pair) {
        return pair.Contains('<') ? new AlloyPair(pair[2], pair[0]) : new AlloyPair(pair[0], pair[2]);
    }

    public IEnumerable<char> All() {
        yield return Harder;
        yield return Softer;
    }
}
