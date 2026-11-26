#!/usr/bin/env dotnet
#:include ../generic/Test.cs

// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    var tablets = new Tablets(notes);
    tablets.GroupByShade();
    Assert.ActualEqualsExpected(tablets.Flips, expected);
});

class Tablets(string notes) {
    readonly char[] shades = notes.ToCharArray();
    public int Flips { get; private set; }

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
        }
    }
}
