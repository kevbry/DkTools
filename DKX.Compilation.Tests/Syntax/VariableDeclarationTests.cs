using DKX.Compilation.DataTypes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Syntax
{
    [TestFixture]
    class VariableDeclarationTests : CompileTestClass
    {
        [Test]
        public async Task VarKeyword()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            var x = 0;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_${DoTestDecoration}()
{
    int x;
    x = 0;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task VarKeywordMultiple()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            var x = 0;
            var y = 1;
            var z = 2;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_${DoTestDecoration}()
{
    int x;
    int y;
    int z;
    x = 0;
    y = 1;
    z = 2;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task MultipleScopes()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            var x = 0;

            if (x == 0)
            {
                var tmp = -1;
            }
            else
            {
                var tmp = 0;
            }
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_${DoTestDecoration}()
{
    int x;
    int tmp;
    int __tmp1;

    x = 0;
    if x == 0
    {
        tmp = -1;
    }
    else
    {
        __tmp1 = 0;
    }
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task VariablesInAProperty()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static int Value
        {
            get
            {
                var x = 0;

                if (x == 0)
                {
                    var tmp = -1;
                    return tmp;
                }
                else
                {
                    var tmp = 0;
                    return tmp;
                }
            }
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
int GetValue()
{
    int x;
    int tmp;
    int __tmp1;

    x = 0;
    if x == 0
    {
        tmp = -1;
        return tmp;
    }
    else
    {
        __tmp1 = 0;
        return __tmp1;
    }
}
"
);
        }

        [Test]
        public async Task JunkAfterInitializer()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            var x = 0 blah blah blah;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.ExpectedStatementEndToken);
        }

        [Test]
        public async Task ExplicitMultiple()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            int x = 0, y = 1, z = 2;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_${DoTestDecoration}()
{
    int x;
    int y;
    int z;
    x = 0;
    y = 1;
    z = 2;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task ExplicitMultipleNotAllWithInitializers()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            int x = 0, y, z = 2;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_${DoTestDecoration}()
{
    int x;
    int y;
    int z;
    x = 0;
    z = 2;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task DuplicateVariableNames()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            int x;
            var x = 0;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateVariable);
        }

        [Test]
        public async Task DuplicateVariableNames2()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            var x = 0;
            int x;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateVariable);
        }

        [Test]
        public async Task DuplicateVariableNames3()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest(int x)
        {
            int x;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateVariable);
        }

        [TestCase("this", ErrorCode.ExpectedVariableName)]
        [TestCase("else", ErrorCode.ExpectedVariableName)]
        [TestCase("static", ErrorCode.ExpectedVariableName)]
        [TestCase("__blah", ErrorCode.InvalidVariableName)]
        public async Task InvalidVariableName(string variableName, ErrorCode expectedErrorCode)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest()
        {
            var ${VariableName} = 0;
            int x;
        }
    }
}
"
.Replace("${VariableName}", variableName)
);
            await RunCompileAsync(app, expectedErrorCode: expectedErrorCode);
        }

        [Test]
        public async Task DuplicateArgumentNames()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static void DoTest(int x, string x)
        {
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateArgumentName);
        }

        [Test]
        public async Task ConflictWithMemberVariable()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        private static int x;

        public static void DoTest()
        {
            int x;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
int x;

void DoTest_${DoTestDecoration}()
{
    int x;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task ConflictWithConstant()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        private const int x = 0;

        public static void DoTest()
        {
            int x;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest

void DoTest_${DoTestDecoration}()
{
    int x;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task ConflictWithProperty()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public static class UnitTest
    {
        public static int Value { get { return 0; } }

        public static void DoTest()
        {
            int Value;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest

int GetValue() { return 0; }

void DoTest_${DoTestDecoration}()
{
    int Value;
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task ObjectInstantiation()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public class UnitTest
    {
        public static UnitTest CreateUnitTest()
        {
            UnitTest ret = new UnitTest();
            return ret;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest

unsigned int CreateUnitTest_${CreateUnitTestDecoration}()
{
    unsigned int ret;
    ret = dkx_addref(dkx_new(0));
    dkx_release(ret);
    return ret;
}
"
.Replace("${CreateUnitTestDecoration}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.UnitTest" }), DataType.EmptyArray))
);
        }

        [Test]
        public async Task SharedObjectReferences()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public class UnitTest
    {
        public static UnitTest CreateUnitTest()
        {
            var ret = new UnitTest();
            var ret2 = ret;
            return ret;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest

unsigned int CreateUnitTest_${CreateUnitTestDecoration}()
{
    unsigned int ret;
    unsigned int ret2;
    ret = dkx_addref(dkx_new(0));
    ret2 = dkx_addref(ret);
    dkx_release(ret);
    dkx_release(ret2);
    return ret;
}
"
.Replace("${CreateUnitTestDecoration}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.UnitTest" }), DataType.EmptyArray))
);
        }

        [Test]
        public async Task SharedObjectReferences2()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public class UnitTest
    {
        public static UnitTest CreateUnitTest()
        {
            var ret = new UnitTest();
            UnitTest ret2 = ret;
            return ret;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest

unsigned int CreateUnitTest_${CreateUnitTestDecoration}()
{
    unsigned int ret;
    unsigned int ret2;
    ret = dkx_addref(dkx_new(0));
    ret2 = dkx_addref(ret);
    dkx_release(ret);
    dkx_release(ret2);
    return ret;
}
"
.Replace("${CreateUnitTestDecoration}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.UnitTest" }), DataType.EmptyArray))
);
        }

        [Test]
        public async Task SwappingObjectReferences()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public class UnitTest
    {
        public static UnitTest CreateUnitTest()
        {
            var ret = new UnitTest();
            UnitTest ret2;
            ret2 = ret;
            return ret;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest

unsigned int CreateUnitTest_${CreateUnitTestDecoration}()
{
    unsigned int ret;
    unsigned int ret2;
    ret = dkx_addref(dkx_new(0));
    ret2 = 0;
    ret2 = dkx_swap(ret2, ret);
    dkx_release(ret);
    dkx_release(ret2);
    return ret;
}
"
.Replace("${CreateUnitTestDecoration}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.UnitTest" }), DataType.EmptyArray))
);
        }

        // TODO: using 'var' statement to instantiate object
        // TODO: member variable in static class must be defined static
        // TODO: using 'this' to get at a member variable instead of local variable with same name
    }
}
