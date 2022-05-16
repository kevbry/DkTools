using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    [TestFixture]
    class CompileTests : CompileTestClass
    {
        private async Task TestSingleFile(string dkxPathName, string wbdkPathName, string dkxCode, string expectedWbdkCode)
        {
            var app = CreateAppContext();
            SetupCompile(app);
            app.LoadAppSettings();

            app.FileSystem.WriteFileText(dkxPathName, dkxCode);

            var compiler = new Compiler(app);
            await compiler.CompileAsync(cancel: default);
            Assert.IsFalse(compiler.HasErrors, "Compiler returned errors.");

            Assert.IsTrue(app.FileSystem.FileExists(@"x:\src\__Test.nc"), "WBDK file was not generated.");
            var wbdkCode = app.FileSystem.GetFileText(@"x:\src\__Test.nc");
            TestContext.Out.WriteLine(wbdkCode);
            WbdkCodeValidator.Validate(expectedWbdkCode, wbdkCode);
        }

        [Test]
        public async Task Increment()
        {
            await TestSingleFile(@"x:\src\Test.dkx", @"x:\src\__Test.nc", @"
class Test
{
    void Test1()
    {
        int x = 0;
        x++;
    }
}",
@"
void Test1()
{
	int x;
	x = 0;
	x += 1;
}");
        }
    }
}
