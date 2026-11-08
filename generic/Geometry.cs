readonly record struct Point(int X, int Y) {
    static readonly Point UP = new(0, 1);
    static readonly Point DOWN = new(0, -1);
    static readonly Point LEFT = new(-1, 0);
    static readonly Point RIGHT = new(1, 0);

    public static Point[] Directions8 { get; } = [
        RIGHT, LEFT, UP, DOWN,
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
    ];

    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    public static Point operator +(Point a, int value) => new(a.X + value, a.Y + value);
    public static Point operator -(Point a, int value) => new(a.X - value, a.Y - value);
    public static Point operator -(Point value) => new(-value.X, -value.Y);

    public override string ToString() => $"({X}, {Y})";
}

static class Screen {
    public static readonly Point UP = new(0, -1);
    public static readonly Point DOWN = new(0, 1);
    public static readonly Point LEFT = new(-1, 0);
    public static readonly Point RIGHT = new(1, 0);
    public static readonly Point TOP_LEFT = new(-1, -1);
    public static readonly Point TOP_RIGHT = new(1, -1);
    public static readonly Point HEX_LEFT = new(-2, 0);
    public static readonly Point HEX_RIGHT = new(2, 0);
    public static readonly Point BOTTOM_LEFT = new(-1, 1);
    public static readonly Point BOTTOM_RIGHT = new(1, 1);

    public static bool TryParseDirection(char c, out Point direction) {
        direction = c switch {
            '>' => RIGHT,
            '<' => LEFT,
            '^' => UP,
            'v' => DOWN,
            _ => default
        };
        return direction != default;
    }

    public static Point TurnRight(Point direction) => new(-direction.Y, direction.X);
}