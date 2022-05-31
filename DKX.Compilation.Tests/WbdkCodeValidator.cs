using DK;
using NUnit.Framework;
using System.Text;

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
                Assert.IsTrue(expIndex >= expSource.Length == actIndex >= actSource.Length, "Expected and actual code ends differently.{0}", StringDiff(expSource, expIndex, actSource, actIndex));
                if (expIndex >= expSource.Length) break;

                Assert.AreEqual(expSource[expIndex], actSource[actIndex], "Characters differ.{0}", StringDiff(expSource, expIndex, actSource, actIndex));
                expIndex++;
                actIndex++;
            }
        }

        private static string StringDiff(string expSource, int expIndex, string actSource, int actIndex)
        {
            return $"\nExpected: {BuildContext(expSource, expIndex)}\nActual: {BuildContext(actSource, actIndex)}";
        }

        private const int ContextLength = 50;

        private static string BuildContext(string source, int errorIndex)
        {
            var sb = new StringBuilder();

            if (errorIndex > source.Length) errorIndex = source.Length;

            var start = errorIndex - ContextLength;
            if (start < 0) start = 0;

            sb.Append(source.Substring(start, errorIndex - start));
            sb.Append(">>>>>");

            if (errorIndex + 1 <= source.Length)
            {
                sb.Append(source[errorIndex]);
                sb.Append("<<<<<");
                errorIndex++;

                var end = errorIndex + ContextLength;
                if (end > source.Length) sb.Append(source.Substring(errorIndex));
                else sb.Append(source.Substring(errorIndex, end - errorIndex));
            }

            return sb.ToString();
        }
    }
}
