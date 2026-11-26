#!/usr/bin/env dotnet
#:include ../generic/Test.cs

// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    string shelf = part switch {
        1 => notes,
        _ => Enumerable.Range(0, 10).Aggregate(string.Empty, (it, _) => it + notes)
    };
    var tablets = new Tablets(shelf);
    tablets.GroupByShade();
    int result = part switch {
        1 => tablets.Flips,
        _ => tablets.Flipped
    };
    Test.Log(tablets.Flips);
    Assert.ActualEqualsExpected(result, expected);
});

class Tablets(string notes) {
    readonly char[] shades = notes.ToCharArray();
    public int Flips { get; private set; }
    public int Flipped { get; private set; }

    public void GroupByShade() {
        var shadeIndex = 0;
        while (true) {
            if (!TryGroup(shadeIndex)) {
                return;
            }
            FindNextShade();
        }

        void FindNextShade() {
            char shade = shades[shadeIndex];
            for (; shades[shadeIndex] == shade; shadeIndex++) { }
        }
    }

    bool TryGroup(int shadeIndex) {
        char shade = shades[shadeIndex];
        int first, last;
        while (true) {
            FindFirstAndLast();
            if (first == shades.Length) {
                return false;
            }
            if (first > last) {
                break;
            }
            Flip();
        }
        return true;

        void FindFirstAndLast() {
            for (first = shadeIndex + 1; first < shades.Length && shades[first] == shade; first++) { }
            for (last = shades.Length - 1; shades[last] != shade; last--) { }
        }

        void Flip() {
            for (int f = first, l = last; f < l; f++, l--) {
                (shades[f], shades[l]) = (shades[l], shades[f]);
            }
            Flips++;
            int length = last + 1 - first;
            Flipped += length;
        }
    }
}
