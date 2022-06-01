using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Classes
{
    [TestFixture]
    class ConstantsTests : CompileTestClass
    {
        [Test]
        public async Task SingleConstant()
        {
            var app = await SetupCompileSingle("x:\\src\\Test.dkx", @"
namespace Test
{
    public class Unit
    {
        public const int Zero = 0;

        public static void DoTest()
        {
            int x = Zero;
        }
    }
}
");

            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Unit")}.nc", @"
void DoTest_%1()
{
    int x;
    x = 0;
}
".Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
        }

        [Test]
        public async Task SingleConstantWithMath()
        {
            var app = await SetupCompileSingle("x:\\src\\Test.dkx", @"
namespace Test
{
    public class Unit
    {
        public const int SecondsPerHour = 60 * 60;

        public static void DoTest()
        {
            int x = SecondsPerHour;
        }
    }
}
");

            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Unit")}.nc", @"
void DoTest_%1()
{
    int x;
    x = 3600;
}
".Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
        }

        [Test]
        public async Task ConstantsCombined()
        {
            var app = await SetupCompileSingle("x:\\src\\Test.dkx", @"
namespace Test
{
    public class Unit
    {
        public const int SecondsPerHour = MinutesPerHour * SecondsPerMinute;
        public const int SecondsPerMinute = 60;
        public const int MinutesPerHour = 60;

        public static void DoTest()
        {
            int x = SecondsPerHour;
        }
    }
}
");

            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Unit")}.nc", @"
void DoTest_%1()
{
    int x;
    x = 3600;
}
".Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
        }
    }
}
