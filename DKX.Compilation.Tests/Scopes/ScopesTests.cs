using DK.Code;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tokens;
using NUnit.Framework;
using System.Linq;

namespace DKX.Compilation.Tests.Scopes
{
    [TestFixture]
    class ScopesTests
    {
        [Test]
        public void MemberClass()
        {
            var dkxCode = @"
namespace Test
{
    class Member
    {
        int _no;
        string _name;

        public int Number { get { return _no; } }
        public string Name { get { return _name; } set { _name = value; } }

        public void SetData(int no, string name)
        {
            _no = no;
            _name = name;
        }
    }
}
";
            var wbdkCode = @"
int Member__no;
char(255) Member__name;

int Member_GetNumber() { return Member__no; }
char(255) Member_GetName() { return Member__name; }
void Member_SetName(char(255) value) { Member__name = value; }

void Member_SetData(int no, char(255) name)
{
    Member__no = no;
    Member__name = name;
}
";
            var cp = new DkxCodeParser(dkxCode);
            var fileScope = new FileScope(@"x:\src\test.dkx", cp, ProcessingDepth.Full);
            fileScope.ProcessTokens(cp.ReadAll().Tokens);

            foreach (var ri in fileScope.ReportItems) TestContext.Out.WriteLine(ri.ToString());
            Assert.IsFalse(fileScope.HasErrors, "Compiler returned errors.");

            var fileContexts = fileScope.GetFileContexts().ToArray();
            Assert.AreEqual(1, fileContexts.Length);
            Assert.AreEqual(FileContext.NeutralClass, fileContexts[0]);

            var actualWbdkCode = fileScope.GenerateWbdkCode(fileContexts[0]);
            TestContext.Out.WriteLine($"Expected WBDK Code:\n{wbdkCode}");
            TestContext.Out.WriteLine($"Actual WBDK Code:\n{actualWbdkCode}");
            WbdkCodeValidator.Validate(wbdkCode, actualWbdkCode);
        }
    }
}
