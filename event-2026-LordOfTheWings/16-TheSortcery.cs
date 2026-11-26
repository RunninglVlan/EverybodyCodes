#!/usr/bin/env dotnet
#:include ../generic/Test.cs

// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    int flips = GroupTablets(notes.ToCharArray());
    Assert.ActualEqualsExpected(flips, expected);
});
return;

int GroupTablets(char[] shades) {
    int shadeIndex = 0, first, last, flips = 0;
    char shade = shades[shadeIndex];
    while (true) {
        while (true) {
            FindFirstAndLast();
            if (first == shades.Length) {
                return flips;
            }
            if (first > last) {
                break;
            }
            Flip();
            flips++;
        }
        FindNextShade();
        shade = shades[shadeIndex];
    }

    void FindFirstAndLast() {
        for (first = shadeIndex + 1; first < shades.Length && shades[first] == shade; first++) { }
        for (last = shades.Length - 1; shades[last] != shade; last--) { }
    }

    void Flip() {
        for (int f = first, l = last; f < l; f++, l--) {
            (shades[f], shades[l]) = (shades[l], shades[f]);
        }
    }

    void FindNextShade() {
        for (; shades[shadeIndex] == shade; shadeIndex++) { }
    }
}
