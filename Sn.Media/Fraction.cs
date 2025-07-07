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

        public override string ToString()
        {
            return $"{Numerator}/{Denominator}, {Value}";
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

        public static Fraction FromValue(double value)
        {
            if (value == 0)
                return new Fraction(0, 1); // 特殊情况: 0 可以表示为 0/1
                                           // 使用 Math.Abs(value) 处理负值
            double absValue = Math.Abs(value);
            int sign = value < 0 ? -1 : 1; // 记录符号
                                           // 将浮点数转化为分数
            int denominator = 1;
            while (absValue % 1 > 0)
            {
                absValue *= 10;
                denominator *= 10;
            }
            int numerator = (int)absValue * sign;
            // 简化分数
            int gcd = GCD(Math.Abs(numerator), denominator);
            if (gcd != 0) // 避免除以零
            {
                numerator /= gcd;
                denominator /= gcd;
            }
            return new Fraction(numerator, denominator);
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
