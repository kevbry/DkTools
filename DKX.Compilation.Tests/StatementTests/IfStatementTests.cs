using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.StatementTests
{
    [TestFixture]
    class IfStatementTests : CompileTestClass
    {
        [Test]
        public async Task If()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        if (x == 0) { x = 1; }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    if x == 0 { x = 1; }
}
");
        }

        [Test]
        public async Task IfElse()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        if (x == 0) { x = 1; }
        else { x = 2; }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    if x == 0 { x = 1; } else { x = 2; }
}
");
        }

        [Test]
        public async Task IfElseIfElse()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        if (x == 0) { x = 1; }
        else if (x == 1) { x = 2; }
        else { x = 3; }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    if x == 0 { x = 1; }
    else if x == 1 { x = 2; }
    else { x = 3; }
}
");
        }

        [Test]
        public async Task NoBodies()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        if (x == 0) x = 1;
        else if (x == 1) x = 2;
        else x = 3;
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    if x == 0 { x = 1; }
    else if x == 1 { x = 2; }
    else { x = 3; }
}
");
        }

        [Test]
        public async Task NoBracketsFormat()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        if x == 0 { x = 1; }
        else if x == 1 { x = 2; }
        else { x = 3; }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    if x == 0 { x = 1; }
    else if x == 1 { x = 2; }
    else { x = 3; }
}
");
        }

        [Test]
        public async Task IfElseIf()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int x = 0;
        if (x == 0) { x = 1; }
        else if (x == 1) { x = 2; }
    }
}
", @"
void TestMethod()
{
    int x;
    x = 0;
    if x == 0 { x = 1; }
    else if x == 1 { x = 2; }
}
");
        }

        [Test]
        public async Task NegativeIfAlone()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        if
    }
}
", null, expectedError: new ReportItem(@"x:\src\test.dkx", 6, 4, -1, -1, ErrorCode.ExpectedExpression));
        }

        [Test]
        public async Task NegativeNoBody()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        if (1)
    }
}
", null, expectedError: new ReportItem(@"x:\src\test.dkx", 6, 4, -1, -1, ErrorCode.ExpectedStatement));
        }

        [Test]
        public async Task NegativeNoElseBody()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        if (1) { } else
    }
}
", null, expectedError: new ReportItem(@"x:\src\test.dkx", 6, 4, -1, -1, ErrorCode.ExpectedStatement));
        }

        [Test]
        public async Task NotBoolean()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        if (1) { }
    }
}
", null, expectedError: new ReportItem(@"x:\src\test.dkx", 5, 12, 5, 13, ErrorCode.ConditionMustBeBool));
        }
    }
}
