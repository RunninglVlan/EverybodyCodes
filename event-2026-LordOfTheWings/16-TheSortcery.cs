#!/usr/bin/env dotnet
#:include ../generic/Test.cs

// https://everybody.codes/event/2026/quests/16
Test.Run((notes, part, expected) => {
    char[] shades = notes.ToCharArray();
    int shadeIndex = 0, first, last, flips = 0;
    char shade = shades[shadeIndex];
    while (true) {
        while (true) {
            FindFirstAndLast();
            if (first == shades.Length) {
                goto end;
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
    end:
    Assert.ActualEqualsExpected(flips, expected);
    return;

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
});
