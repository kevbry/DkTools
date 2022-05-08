using DK.Code;
using DKX.Compilation.WbdkExports;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    [TestFixture]
    class ScanWbdExportFileJobTests : CompileTestClass
    {
        [Test]
        public async Task AgeFunctionFile()
        {
            SetupCompileFiles();

            var pathName = @"x:\src\age.f";
            var exportsPathName = @"x:\bin\.dkx\age.f.exports.json";
            var job = new ScanWbdkExportFileJob(App, pathName, exportsPathName, FileContext.Function);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(FS.GetFileText(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            Assert.IsNotNull(model.Exports);
            Assert.AreEqual(1, model.Exports.Length);

            var export = model.Exports[0];
            Assert.AreEqual("age", export.Name);
            Assert.IsNull(export.ClassName);
        }
    }
}
