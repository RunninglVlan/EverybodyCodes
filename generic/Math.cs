static class Mathf {
    public static bool Approximately(double a, double b) => Math.Abs(a - b) < 0.001;

    public static int Wrap(int value, int max) {
        int remainder = value % max;
        return remainder < 0 ? remainder + max : remainder;
    }
}
