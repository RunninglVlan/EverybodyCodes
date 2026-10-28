// https://everybody.codes/event/2026/quests/2

const string inputFile = "../Input.txt";
string input = File.ReadAllText(inputFile);
const string inputExample = """
                            4<3 2<5 3>1 1>2 1<5 4<1 3>2 4<2 3>5 4<5
                            4>1 2>1 3>2 1<5 4>5 2<5 3>1 4>3 2<4 5<3
                            5>1 5>2 4>1 2<4 3>2 3<4 1>2 3>5 1<3 4>5
                            """;
// input = inputExample;
string[] alloyGroups = input.Split(Environment.NewLine);
int checksum = alloyGroups.Select(AlloyNumber).Sum();
Console.WriteLine(checksum);
return;

// Orders alloys from hardest to softest
int AlloyNumber(string group) {
    const string alloys = "12345";
    var number = string.Empty;
    var alloyPairs = group.Split(' ').Select(AlloyPair.From).ToList();
    while (alloyPairs.Count > 0) {
        (char? alloy, int count) softest = (null, 0);
        foreach (char alloy in alloys) {
            int count = alloyPairs.Count(it => it.Softer == alloy);
            if (count > softest.count) {
                softest = (alloy, count);
            }
        }
        number = softest.alloy + number;
        if (alloyPairs.Count == 1) {
            number = alloyPairs.First().Harder + number;
        }
        alloyPairs.RemoveAll(it => it.Softer == softest.alloy);
    }
    return int.Parse(number);
}

readonly record struct AlloyPair(char Harder, char Softer) {
    public static AlloyPair From(string pair) {
        return pair.Contains('<') ? new AlloyPair(pair[2], pair[0]) : new AlloyPair(pair[0], pair[2]);
    }
}
