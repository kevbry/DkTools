using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    [TestFixture]
    class ServerProgramTests : CompileTestClass
    {
        [Test]
        public async Task ServerProgram()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [ServerProgram(""TestProg"")]
        public static int Main(string fileName)
        {
            System.Console.WriteLine(fileName);
            return 0;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\TestProg.sp", @"
// Processes.TestProgram
int main(char(255) fileName)
{
    puts(fileName);
    return 0;
}
");
        }

        [Test]
        public async Task ServerProgramAndNeutralClassMethod()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [ServerProgram(""TestProg"")]
        public static int Main(string fileName)
        {
            GenerateReport(fileName);
            return 0;
        }

        private static void GenerateReport(string fileName)
        {
            System.Console.WriteLine(fileName);
        }
    }
}
");
            await RunCompileAsync(app);

            var className = Compiler.GetWbdkClassName("Processes.TestProgram");
            var decoGenerateReport = Compiler.ComputeHash("void, string").ToString("X");

            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\TestProg.sp", @"
// Processes.TestProgram
int main(char(255) fileName)
{
    %C.GenerateReport_%1(fileName);
    return 0;
}
".Replace("%1", decoGenerateReport).Replace("%C", className));
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{className}.nc", @"
// Processes.TestProgram
void GenerateReport_%1(char(255) fileName)
{
    puts(fileName);
}
".Replace("%1", decoGenerateReport));
        }

        [Test]
        public async Task ServerProgramAndNeutralClassProperty()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [ServerProgram(""TestProg"")]
        public static int Main(string fileName)
        {
            if (fileName == ReportName) System.Console.WriteLine(""match"");
            else System.Console.WriteLine(""no match"");
            return 0;
        }

        private static string ReportName { get { return ""TestReport""; } }
    }
}
");
            await RunCompileAsync(app);

            var className = Compiler.GetWbdkClassName("Processes.TestProgram");

            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\TestProg.sp", @"
// Processes.TestProgram
int main(char(255) fileName)
{
    if fileName == %C.GetReportName() { puts(""match""); }
    else { puts(""no match""); }
    return 0;
}
".Replace("%C", className));
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{className}.nc", @"
// Processes.TestProgram
char(255) GetReportName() { return ""TestReport""; }
");
        }
    }
}
