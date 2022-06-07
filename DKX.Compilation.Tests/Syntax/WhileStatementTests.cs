using DKX.Compilation.DataTypes;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Syntax
{
    [TestFixture]
    class WhileStatementTests : CompileTestClass
    {
        [Test]
        public async Task SimpleWhileStatement()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            var x = 0;
            while (x < 10)
            {
                Console.WriteLine(x.ToString());
                x++;
            }
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    int x;
    x = 0;
    while x < 10
    {
        puts(makestring(x));
        x += 1;
    }
}
"
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task SingleStatementBody()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            var x = 0;
            while (x < 10) x++;
            Console.WriteLine(x.ToString());
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    int x;
    x = 0;
    while x < 10
    {
        x += 1;
    }
    puts(makestring(x));
}
"
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task WhileOne()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            var x = 0;
            while (1) x++;
            Console.WriteLine(x.ToString());
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.ExpressionMustBeBool);
        }

        [Test]
        public async Task WhileTrue()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            var x = 0;
            while (true) x++;
            Console.WriteLine(x.ToString());
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    int x;
    x = 0;
    while 1
    {
        x += 1;
    }
    puts(makestring(x));
}
"
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task WhileBreak()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            var x = 0;
            while (true)
            {
                x++;
                Console.WriteLine(x.ToString());
                if (x >= 10) break;
            }
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    int x;
    x = 0;
    while 1
    {
        x += 1;
        puts(makestring(x));
        if x >= 10 { break; }
    }
}
"
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task WhileContinue()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            var x = 0;
            while (true)
            {
                x++;
                Console.WriteLine(x.ToString());
                if (x < 10) continue;
                break;
            }
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    int x;
    x = 0;
    while 1
    {
        x += 1;
        puts(makestring(x));
        if x < 10 { continue; }
        break;
    }
}
"
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task BreakOutsideWhile()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            break;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.NoBreakScope);
        }

        [Test]
        public async Task ContinueOutsideWhile()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void Run()
        {
            continue;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.NoContinueScope);
        }
    }
}
