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
            Group();
            next.Reverse();
            current = [.. next];
            next.Clear();
        }
    }

    void Group() {
        while (true) {
            if (leftNonGroupStart > rightGroupEnd) {
                break;
            }
            Flips++;
            Flipped += rightGroupEnd + 1 - leftNonGroupStart;
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
            Flipped += rightGroupEnd + 1 - leftNonGroupStart;
            FindLeftNonGroup();
            FindRightGroup();
            rightNonGroupEnd++;
            AddRightNonGroup();
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
        int nonGroup = leftNonGroupStart;
        for (; nonGroup < current.Count; nonGroup++) {
            char tablet = current[nonGroup];
            if (tablet == shade) {
                break;
            }
            next.Add(tablet);
        }
        leftNonGroupStart = nonGroup;
    }
}
