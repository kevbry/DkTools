using DK.Code;
using DKX.Compilation.Files;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    [TestFixture]
    class CompileFileJobTests : CompileTestClass
    {
        [Test]
        public async Task SimpleMethod()
        {
            var app = CreateAppContext();
            var fs = app.FileSystem;

            var dkxPathName = @"x:\src\test.ncx";
            var wbdkPathName = @"x:\src\test.nc";
            var objPathName = @"x:\bin\.dkx\test.ncx.dkxx";
            var fileContext = FileContext.NeutralClass;

            fs.WriteFileText(dkxPathName, @"
class Test
{
    void DoNothing()
    {
    }
}
");

            var compileJob = new CompileFileJob(app, dkxPathName, wbdkPathName, objPathName, fileContext);
            await compileJob.ExecuteAsync(cancel: default);

            Assert.IsTrue(fs.FileExists(objPathName), "Object file was not created.");
            Assert.IsFalse(fs.FileExists(wbdkPathName), "WBDK file should not have been created.");

            var objContent = fs.GetFileText(objPathName);
            var obj = JsonConvert.DeserializeObject<ObjectFileModel>(objContent);
            Assert.IsNotNull(obj);

            Assert.AreEqual(dkxPathName, obj.SourcePathName);
            Assert.AreEqual(wbdkPathName, obj.DestinationPathName);
            Assert.AreEqual("Test", obj.ClassName);
            Assert.IsNull(obj.FileDependencies);
            Assert.IsNull(obj.TableDependencies);
            Assert.IsNull(obj.Properties);

            Assert.IsNotNull(obj.Methods);
            Assert.AreEqual(1, obj.Methods.Length);

            var method = obj.Methods[0];
            Assert.AreEqual("DoNothing", method.Name);
            Assert.AreEqual(Privacy.Private, method.Privacy);
            Assert.AreEqual("void", method.ReturnDataType);
            Assert.IsNull(method.Arguments);
        }
    }
}
