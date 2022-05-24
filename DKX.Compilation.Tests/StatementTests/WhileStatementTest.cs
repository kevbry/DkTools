using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.StatementTests
{
    [TestFixture]
    class WhileStatementTest : CompileTestClass
    {
        [Test]
        public async Task SimpleWhile()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        while (x < 10)
        {
            x++;
        }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    while x < 10 { x += 1; }
}
");
        }

        [Test]
        public async Task NoBody()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        while (x < 10) x++;
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    while x < 10 { x += 1; }
}
");
        }

        [Test]
        public async Task NoBrackets()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        while x < 10 { x++; }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    while x < 10 { x += 1; }
}
");
        }

        [Test]
        public async Task EmptyStatement()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        while x < 10 { }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    while x < 10 { }
}
");
        }

        [Test]
        public async Task EmptyStatement2()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        while (x < 10) ;
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    while x < 10 { }
}
");
        }

        [Test]
        public async Task ConditionNotBoolean()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        while (1) ;
    }
}
", null, ReportItem.FromOneBased(@"x:\src\test.dkx", 6, 16, 6, 17, ErrorCode.ConditionMustBeBool));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task ConditionIsBool(bool value)
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        while (%1) x++;
    }
}
".Replace("%1", value.ToString().ToLower()), @"
void TestMethod()
{
    int x;
    x = 0;
    while %1 { x += 1; }
}
".Replace("%1", value ? "1" : "0"));
        }
    }
}
