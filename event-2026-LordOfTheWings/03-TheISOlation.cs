// https://everybody.codes/event/2026/quests/3

const string noteFile = "../Notes.txt";
var notes = File.ReadAllText(noteFile);
var votes = notes.Split(Environment.NewLine)
        .Select(Vote.From)
        .Where(it => it.IsValid());
Console.WriteLine(votes.Sum(it => it.decimalValue));

readonly struct Vote {
    readonly bool isISO2;
    readonly string standard, value;
    public readonly long decimalValue;

    public static Vote From(string vote) {
        var parts = vote.Split(' ');
        var standard = parts.First().Split('-');
        var isISO2 = standard.First().Contains('2');
        return new Vote(isISO2, standard.Last(), parts.Last());
    }

    Vote(bool isISO2, string standard, string value) {
        this.isISO2 = isISO2;
        this.standard = standard;
        this.value = value;
        for (var i = 1; i <= value.Length; i++) {
            if (i > standard.Length) {
                standard += standard;
            }
            var standardDigit = char.GetNumericValue(standard[^i]);
            var valueDigit = char.GetNumericValue(value[^i]);
            var power = i - 1;
            decimalValue += (long)(valueDigit * Math.Pow(standardDigit, power));
        }
    }

    public bool IsValid() {
        if (!isISO2) {
            return true;
        }
        for (var current = long.Parse(value) - 1; current >= 0; current--) {
            if (new Vote(true, standard, current.ToString()).decimalValue == decimalValue) {
                return false;
            }
        }
        return true;
    }
}
