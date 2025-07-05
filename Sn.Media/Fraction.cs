namespace Sn.Media
{
    /// <summary>
    /// 表示一个分数
    /// </summary>
    /// <param name="Numerator">分子</param>
    /// <param name="Denominator">分母</param>
    public record struct Fraction(int Numerator, int Denominator)
    {
        public double Value => (double)Numerator / Denominator;
        public Fraction Simplified
        {
            get
            {
                int gcd = GCD(Numerator, Denominator);
                return new Fraction(Numerator / gcd, Denominator / gcd);
            }
        }

        /// <summary>
        /// 计算两个整数的最大公约数（GCD）
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return Math.Abs(a); // 返回绝对值以避免负数
        }

        public static Fraction operator +(Fraction a, Fraction b) =>
            new Fraction(a.Numerator * b.Denominator + b.Numerator * a.Denominator, a.Denominator * b.Denominator);
        public static Fraction operator -(Fraction a, Fraction b) =>
            new Fraction(a.Numerator * b.Denominator - b.Numerator * a.Denominator, a.Denominator * b.Denominator);
        public static Fraction operator *(Fraction a, Fraction b) =>
            new Fraction(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        public static Fraction operator /(Fraction a, Fraction b) =>
            new Fraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
    }
}
