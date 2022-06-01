using DKX.Compilation.ReportItems;
using NUnit.Framework;
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
// Test.Unit

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
// Test.Unit

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
// Test.Unit

void DoTest_%1()
{
    int x;
    x = 3600;
}
".Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
        }

        [Test]
        public async Task CircularDependencyInSameClass()
        {
            var app = await SetupCompileSingle("x:\\src\\Test.dkx", @"
namespace Test
{
    public class Unit
    {
        public const int A = B;
        public const int B = A;
    }
}
", expectedError: new ReportItem(new Span("x:\\src\\Test.dkx", 76, 77), ErrorCode.CircularConstantDependency, "A"));
        }

        [Test]
        public async Task AcrossClasses()
        {
            var app = await SetupCompileSingle("x:\\src\\Test.dkx", @"
namespace Test
{
    public class ClassA
    {
        public const int ConstA = 1;

        public static void DoTest()
        {
            int a = ConstA - ClassB.ConstB;
        }
    }

    public class ClassB
    {
        public const int ConstB = 2;

        public static void DoTest()
        {
            int x = ClassA.ConstA + ConstB;
        }
    }
}
");
            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.ClassA")}.nc", @"
// Test.ClassA

void DoTest_%1()
{
    int a;
    a = -1;
}
".Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.ClassB")}.nc", @"
// Test.ClassB

void DoTest_%1()
{
    int x;
    x = 3;
}
".Replace("%1", Compiler.ComputeHash("void").ToString("X"))
);
        }

        [Test]
        public async Task CircularDependencyAcrossClasses()
        {
            var app = await SetupCompileSingle("x:\\src\\Test.dkx", @"
namespace Test
{
    public class ClassA
    {
        public const int ConstA = ClassB.ConstB;

        public static void DoTest()
        {
            int a = ConstA - ClassB.ConstB;
        }
    }

    public class ClassB
    {
        public const int ConstB = ClassA.ConstA;

        public static void DoTest()
        {
            int x = ClassA.ConstA + ConstB;
        }
    }
}
", expectedError: new ReportItem(new Span("x:\\src\\Test.dkx", 78, 84), ErrorCode.CircularConstantDependency, "ConstA"));
        }
    }
}
