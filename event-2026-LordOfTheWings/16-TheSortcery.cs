#!/usr/bin/env dotnet
#:include ../generic/Test.cs

Test.Until = "Part III";
// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    int copies = part switch {
        1 => 1,
        2 => 10,
        _ => 100
    };
    var tablets = new Tablets(string.Concat(Enumerable.Repeat(notes, copies)));
    tablets.GroupByShade();
    long result = part switch {
        1 => tablets.Flips,
        _ => tablets.Flipped
    };
    Assert.ActualEqualsExpected(result, expected);
});

class Tablets(string notes) {
    readonly char[] shades = notes.ToCharArray();
    int shadeIndex, first, last;
    public int Flips { get; private set; }
    public long Flipped { get; private set; }

    public void GroupByShade() {
        while (true) {
            if (!TryGroup()) {
                return;
            }
            FindNextShade();
        }

        void FindNextShade() {
            char shade = shades[shadeIndex];
            while (shades[shadeIndex] == shade) {
                shadeIndex++;
            }
        }
    }

    bool TryGroup() {
        first++;
        last = shades.Length - 1;
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
            char shade = shades[shadeIndex];
            while (first < shades.Length && shades[first] == shade) {
                first++;
            }
            while (shades[last] != shade) {
                last--;
            }
        }
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
