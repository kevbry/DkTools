using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using NUnit.Framework;

namespace DKX.Compilation.Tests.Scopes
{
    [TestFixture]
    class ScopesTests
    {
        [Test]
        public void Test1()
        {
            var source = @"
namespace Test
{
    public class StringHelper
    {
        public string Trim(string str) { str[0] = """"; }
    }

    public class CharHelper
    {
        public char ToLower(char ch) { return ch >= 'A' && ch <= 'Z' ? ch + 32 : ch; }
        public char ToUpper(char ch) { return ch >= 'a' && ch <= 'z' ? ch - 32 : ch; }
    }
}
";
            var cp = new DkxCodeParser(source);
            var fileScope = new FileScope(@"x:\src\test.dkx", cp, ProcessingDepth.Full);
            fileScope.ProcessTokens(cp.ReadAll().Tokens);

            foreach (var ri in fileScope.ReportItems) TestContext.Out.WriteLine(ri.ToString());
            Assert.IsFalse(fileScope.HasErrors);

            Assert.IsNotNull(fileScope.Namespace);

            var ns = fileScope.Namespace;
            TestContext.Out.WriteLine($"Namespace: {ns.Name}");
            foreach (var cls in ns.Classes)
            {
                TestContext.Out.WriteLine($"  Class: {cls.Name}");
                foreach (var method in cls.Methods)
                {
                    TestContext.Out.WriteLine($"    Method: {method.Signature}");
                }
            }
        }
    }
}
