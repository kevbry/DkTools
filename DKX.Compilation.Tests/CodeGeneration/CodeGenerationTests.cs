using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Tests.Files;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.CodeGeneration
{
    [TestFixture]
    class CodeGenerationTests : CompileTestClass
    {
        private async Task SetupGeneratorTest(DkAppContext app, ObjectFileModel model, string expectedWbdkOutput)
        {
            var queue = new TestJobQueue();

            var dkxPathName = @"x:\src\test.dkx";
            var wbdkPathName = @"x:\src\__test.nc";
            var objPathName = @"x:\bin\.dkx\test.dkxx";

            model.SourcePathName = dkxPathName;
            model.ClassName = "Test";

            var objReader = new TestObjectFileReader(model);

            var genJob = new GenerateCodeJob(
                app: app,
                compileQueue: queue,
                dkxPathName: dkxPathName,
                objPathName: objPathName,
                objectFileReader: objReader,
                reporter: queue);

            await genJob.ExecuteAsync(cancel: default);

            foreach (var item in queue.ReportItems)
            {
                await TestContext.Out.WriteLineAsync($"> {item}");
            }
            Assert.IsFalse(queue.ReportItems.Any(i => i.Severity == ErrorSeverity.Error), "Compiler returned errors.");

            Assert.IsTrue(app.FileSystem.FileExists(dkxPathName));

            Assert.IsTrue(app.FileSystem.FileExists(wbdkPathName));
            var actualWbdkCode = app.FileSystem.GetFileText(wbdkPathName);
            TestContext.Out.WriteLine("Generated Output:");
            TestContext.Out.WriteLine(actualWbdkCode);
;
            WbdkCodeValidator.Validate(expectedWbdkOutput, actualWbdkCode);
        }

        [Test]
        public async Task SimpleVariable()
        {
            var app = CreateAppContext();

            await SetupGeneratorTest(app,
                new ObjectFileModel
                {
                    Methods = new ObjectMethod[]
                    {
                        new ObjectMethod
                        {
                            Name = "TestMethod",
                            Privacy = Privacy.Public,
                            FileContext = FileContext.NeutralClass,
                            ReturnDataType = "void",
                            Arguments = null,
                            Body = new ObjectBody
                            {
                                Variables = new ObjectVariable[]
                                {
                                    new ObjectVariable
                                    {
                                        Name = "x",
                                        DataType = "int"
                                    }
                                },
                                Code = "asn(@x,0)"
                            }
                        }
                    }
                },
                @"
void TestMethod()
{
    int x;
    x = 0;
}
");
        }

        [Test]
        public async Task Increment()
        {
            var app = CreateAppContext();

            await SetupGeneratorTest(app,
                new ObjectFileModel
                {
                    Methods = new ObjectMethod[]
                    {
                        new ObjectMethod
                        {
                            Name = "TestMethod",
                            Privacy = Privacy.Public,
                            FileContext = FileContext.NeutralClass,
                            ReturnDataType = "void",
                            Arguments = null,
                            Body = new ObjectBody
                            {
                                Variables = new ObjectVariable[]
                                {
                                    new ObjectVariable
                                    {
                                        Name = "x",
                                        DataType = "int"
                                    }
                                },
                                Code = "asn(@x,0),inc(@x)"
                            }
                        }
                    }
                },
                @"
void TestMethod()
{
    int x;
    x = 0;
    x += 1;
}
");
        }
    }
}
