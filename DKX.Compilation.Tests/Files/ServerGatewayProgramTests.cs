using DKX.Compilation.ReportItems;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    [TestFixture]
    class ServerGatewayProgramTests : CompileTestClass
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task Program(bool gateway)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [%SPProgram(""TestProg"")]
        public ${context} static int Main(string fileName)
        {
            System.Console.WriteLine(fileName);
            return 0;
        }
    }
}
".Replace("%SP", gateway ? "Gateway" : "Server")
.Replace("${context}", gateway ? "client" : "server"));
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\TestProg.{(gateway ? "gp" : "sp")}", @"
// Processes.TestProgram
int main(char(255) fileName)
{
    puts(fileName);
    return 0;
}
");
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ProgramRequiresContext(bool gateway)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [%SPProgram(""TestProg"")]
        public static int Main(string fileName)
        {
            System.Console.WriteLine(fileName);
            return 0;
        }
    }
}
".Replace("%SP", gateway ? "Gateway" : "Server"));
            await RunCompileAsync(app, expectedError: new ReportItem(new Span("x:\\src\\TestProgram.dkx", 72, 85), ErrorCode.AttributeMustHaveContext,
                gateway ? "GatewayProgram" : "ServerProgram",
                gateway ? "client" : "server"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ProgramAndNeutralClassMethod(bool gateway)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, "x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [${AttributeName}(""TestProg"")]
        public static ${ContextKeyword} int Main(string fileName)
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
"
.Replace("${AttributeName}", gateway ? "GatewayProgram" : "ServerProgram")
.Replace("${ContextKeyword}", gateway ? "client" : "server")
);
            await RunCompileAsync(app);

            var className = Compiler.GetWbdkClassName("Processes.TestProgram");
            var decoGenerateReport = Compiler.ComputeHash("void, string").ToString("X");

            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\TestProg.{(gateway ? "gp" : "sp")}", @"
// Processes.TestProgram
int main(char(255) fileName)
{
    ${ClassName}.GenerateReport_${GenerateReportDecoration}(fileName);
    return 0;
}
"
.Replace("${GenerateReportDecoration}", decoGenerateReport)
.Replace("${ClassName}", className)
);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{className}.nc", @"
// Processes.TestProgram
void GenerateReport_${GenerateReportDecoration}(char(255) fileName)
{
    puts(fileName);
}
"
.Replace("${GenerateReportDecoration}", decoGenerateReport)
);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ProgramAndNeutralClassProperty(bool gateway)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [${AttributeName}(""TestProg"")]
        public static ${ContextKeyword} int Main(string fileName)
        {
            if (fileName == ReportName) System.Console.WriteLine(""match"");
            else System.Console.WriteLine(""no match"");
            return 0;
        }

        private static string ReportName { get { return ""TestReport""; } }
    }
}
"
.Replace("${AttributeName}", gateway ? "GatewayProgram" : "ServerProgram")
.Replace("${ContextKeyword}", gateway ? "client" : "server")
);
            await RunCompileAsync(app);

            var className = Compiler.GetWbdkClassName("Processes.TestProgram");

            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\TestProg.{(gateway ? "gp" : "sp")}", @"
// Processes.TestProgram
int main(char(255) fileName)
{
    if fileName == ${ClassName}.GetReportName() { puts(""match""); }
    else { puts(""no match""); }
    return 0;
}
"
.Replace("${ClassName}", className)
);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{className}.nc", @"
// Processes.TestProgram
char(255) GetReportName() { return ""TestReport""; }
");
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ProgramRequiresStatic(bool gateway)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [${AttributeName}(""TestProg"")]
        public ${ContextKeyword} int Main(string fileName)
        {
            System.Console.WriteLine(fileName);
            return 0;
        }
    }
}
"
.Replace("${AttributeName}", gateway ? "GatewayProgram" : "ServerProgram")
.Replace("${ContextKeyword}", gateway ? "client" : "server")
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.AttributeMustBeStatic);
        }

        [TestCase(false, "")]
        [TestCase(false, ":/\\<>")]
        [TestCase(true, "")]
        public async Task BadPathName(bool gateway, string pathName)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [${AttributeName}(""${PathName}"")]
        public static ${ContextKeyword} int Main(string fileName)
        {
            System.Console.WriteLine(fileName);
            return 0;
        }
    }
}
"
.Replace("${AttributeName}", gateway ? "GatewayProgram" : "ServerProgram")
.Replace("${ContextKeyword}", gateway ? "client" : "server")
.Replace("${PathName}", pathName)
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.AttributeExpectedProgramPathName);
        }

        [TestCase(false, "\"TestProg.sp\", \"\"")]
        [TestCase(false, "\"TestProg.sp\", 0")]
        [TestCase(false, "")]
        public async Task WrongNumberOfArguments(bool gateway, string arguments)
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\TestProgram.dkx", @"
namespace Processes
{
    public class TestProgram
    {
        [${AttributeName}(${AttributeArguments})]
        public static ${ContextKeyword} int Main(string fileName)
        {
            System.Console.WriteLine(fileName);
            return 0;
        }
    }
}
"
.Replace("${AttributeName}", gateway ? "GatewayProgram" : "ServerProgram")
.Replace("${ContextKeyword}", gateway ? "client" : "server")
.Replace("${AttributeArguments}", arguments)
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.AttributeRequiresSingleArgument);
        }
    }
}
