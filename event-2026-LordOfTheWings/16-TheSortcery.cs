#!/usr/bin/env dotnet
#:include ../generic/Test.cs

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
    List<char> current = [.. notes];
    readonly List<char> next = new(notes.Length);
    char shade;
    int leftNonGroupStart, rightGroupEnd, rightNonGroupEnd;
    public int Flips { get; private set; }
    public long Flipped { get; private set; }

    public void GroupByShade() {
        while (current.Count > 0) {
            shade = current[0];
            leftNonGroupStart = 1;
            rightGroupEnd = current.Count - 1;
            FindLeftNonGroup();
            if (leftNonGroupStart > rightGroupEnd) {
                break;
            }
            FindRightGroup();
            rightNonGroupEnd = current.Count;
            AddRightNonGroup();
            while (true) {
                if (leftNonGroupStart > rightGroupEnd) {
                    break;
                }
                Flips++;
                int length = rightGroupEnd + 1 - leftNonGroupStart;
                Flipped += length;
                AddLeftNonGroup();
                rightNonGroupEnd = rightGroupEnd;
                while (current[rightNonGroupEnd] == shade) {
                    rightNonGroupEnd--;
                }
                rightGroupEnd = rightNonGroupEnd;
                if (leftNonGroupStart > rightGroupEnd) {
                    break;
                }
                Flips++;
                length = rightGroupEnd + 1 - leftNonGroupStart;
                Flipped += length;
                FindLeftNonGroup();
                FindRightGroup();
                rightNonGroupEnd++;
                AddRightNonGroup();
            }
            next.Reverse();
            current = [.. next];
            next.Clear();
        }
    }

    void FindLeftNonGroup() {
        while (leftNonGroupStart < current.Count && current[leftNonGroupStart] == shade) {
            leftNonGroupStart++;
        }
    }

    void FindRightGroup() {
        while (current[rightGroupEnd] != shade) {
            rightGroupEnd--;
        }
    }

    void AddRightNonGroup() {
        int rightNonGroupStart = rightGroupEnd + 1;
        current.Reverse(rightNonGroupStart, rightNonGroupEnd - rightNonGroupStart);
        next.AddRange(current[rightNonGroupStart..rightNonGroupEnd]);
    }

    void AddLeftNonGroup() {
        int leftNonGroup = leftNonGroupStart;
        for (; leftNonGroup < current.Count; leftNonGroup++) {
            char tablet = current[leftNonGroup];
            if (tablet == shade) {
                break;
            }
            next.Add(tablet);
        }
        leftNonGroupStart = leftNonGroup;
    }
}
