using DK;
using NUnit.Framework;

namespace DKX.Compilation.Tests
{
    class WbdkCodeValidator
    {
        public static void Validate(string expSource, string actSource)
        {
            var expIndex = 0;
            var actIndex = 0;

            while (true)
            {
                while (expIndex < expSource.Length && expSource[expIndex].IsWhiteSpace()) expIndex++;
                while (actIndex < actSource.Length && actSource[actIndex].IsWhiteSpace()) actIndex++;
                Assert.IsTrue(expIndex >= expSource.Length == actIndex >= actSource.Length, $"Expected and actual code ends differently.{StringDiff(expSource, expIndex, actSource, actIndex)}");
                if (expIndex >= expSource.Length) break;

                Assert.AreEqual(expSource[expIndex], actSource[actIndex], $"Characters differ.{StringDiff(expSource, expIndex, actSource, actIndex)}");
                expIndex++;
                actIndex++;
            }
        }

        private static string StringDiff(string expSource, int expIndex, string actSource, int actIndex)
        {
            return $"\nExpected: {expSource.Substring(0, expIndex + 1 <= expSource.Length ? expIndex + 1 : expSource.Length)}\nActual: {actSource.Substring(0, actIndex + 1 <= actSource.Length ? actIndex + 1 : actSource.Length)}";
        }
    }
}
