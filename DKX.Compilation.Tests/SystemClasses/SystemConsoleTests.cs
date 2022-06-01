using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.SystemClasses
{
    [TestFixture]
    class SystemConsoleTests : CompileTestClass
    {
        [Test]
        public async Task WriteOutput()
        {
            var app = await SetupCompileSingle(@"x:\src\Test.dkx", @"
namespace Test
{
    class Unit
    {
        public static void DoTest()
        {
            System.Console.WriteLine(""Testing..."");
        }
    }
}
");
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Unit")}.nc", @"
// Test.Unit
void DoTest_%1()
{
    puts(""Testing..."");
}
"
.Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
        }
    }
}
