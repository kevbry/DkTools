using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.StatementTests
{
    [TestFixture]
    class ForStatementTests : CompileTestClass
    {
        [Test]
        public async Task SimpleFor()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int i, j;
        for (i = 0; i < 10; i++)
        {
            j = i;
        }
    }
}
", @"
void TestMethod()
{
    int i;
    int j;
    i = 0;
    for (; i < 10; i += 1) { j = i; }
}
");
        }

        [Test]
        public async Task MultipleInitializers()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int i, j;
        for (i = 0, j = 0; i < 10; i++)
        {
            j = i;
        }
    }
}
", @"
void TestMethod()
{
    int i;
    int j;
    i = 0;
    j = 0;
    for (; i < 10; i += 1) { j = i; }
}
");
        }

        [Test]
        public async Task VariableDeclaration()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        for (int i = 0; i < 10; i++) ;
    }
}
", @"
void TestMethod()
{
    int i;
    i = 0;
    for (; i < 10; i += 1) { }
}
");
        }

        [Test]
        public async Task MultipleVariableDeclarations()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        for (int i = 0, ii = 10; i < ii; i++) { }
    }
}
", @"
void TestMethod()
{
    int i;
    int ii;
    i = 0;
    ii = 10;
    for (; i < ii; i += 1) { }
}
");
        }

        [Test]
        public async Task VariableMustBeInitialized()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        for (int i; i < 10; i++) ;
    }
}
", null, ReportItem.FromOneBased(@"x:\src\test.dkx", 6, 18, 6, 19, ErrorCode.VariableInitializationRequired));
        }

        // TODO: Variable declaration with 'var'
        // TODO: Cannot define more than 1 variable when using 'var'

        [Test]
        public async Task NoInitializer()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int i;
        for (; i < 10; i++) { }
    }
}
", @"
void TestMethod()
{
    int i;
    for (; i < 10; i += 1) { }
}
");
        }

        [Test]
        public async Task NoCondition()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int i;
        for (i = 0; ; i++) { }
    }
}
", null, expectedError: ReportItem.FromOneBased(@"x:\src\test.dkx", 7, 21, ErrorCode.ExpectedExpression));
        }

        [Test]
        public async Task NoIteration()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int i;
        for (i = 0; i < 10;) { i++; }
    }
}
", @"
void TestMethod()
{
    int i;
    i = 0;
    for (; i < 10; ) { i += 1; }
}
");
        }

        [Test]
        public async Task MultipleIteration()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        int i;
        int j;
        for (i = 0, j = 0; i < 10; i++, j++) { }
    }
}
", null, ReportItem.FromOneBased(@"x:\src\test.dkx", 8, 39, ErrorCode.ExpectedToken, ')'));
        }

        [Test]
        public async Task NestedLoops()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
            }
        }
    }
}
", @"
void TestMethod()
{
    int i;
    int j;
    i = 0;
    for (; i < 10; i += 1)
    {
        j = 0;
        for (; j < 10; j += 1)
        {
        }
    }
}
");
        }

        [Test]
        public async Task NestedConditionIsNotBool()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; 1; j++)
            {
            }
        }
    }
}
", null, ReportItem.FromOneBased(@"x:\src\test.dkx", 8, 29, 8, 30, ErrorCode.ConditionMustBeBool));
        }
    }
}
