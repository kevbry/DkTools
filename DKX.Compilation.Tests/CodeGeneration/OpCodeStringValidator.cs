using DKX.Compilation.CodeGeneration.OpCodes;
using NUnit.Framework;

namespace DKX.Compilation.Tests.CodeGeneration
{
    class OpCodeStringValidator
    {
        public static void Validate(string expected, string actual)
        {
            var expOps = new OpCodeParser(expected);
            var actOps = new OpCodeParser(actual);

            while (true)
            {
                var expType = expOps.Read();
                var actType = actOps.Read();
                Assert.AreEqual(expType, actType, $"Op code type differs:{StringDiff(expOps, actOps)}");
                Assert.AreEqual(expOps.Text, actOps.Text, $"Token differs: {StringDiff(expOps, actOps)}");
                Assert.AreEqual(expOps.EndOfFile, actOps.EndOfFile, $"End of file is not the same: {StringDiff(expOps, actOps)}");
                if (expOps.EndOfFile) break;
                Assert.AreNotEqual(OpCodeType.None, expType);
                Assert.AreNotEqual(OpCodeType.None, actType);
            }
        }

        private static string StringDiff(OpCodeParser expOps, OpCodeParser actOps)
        {
            return $"\nExpected: {expOps.Source.Substring(0, expOps.Position)}\nActual: {actOps.Source.Substring(0, actOps.Position)}";
        }
    }
}
