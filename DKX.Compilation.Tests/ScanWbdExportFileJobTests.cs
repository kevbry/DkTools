using DK.AppEnvironment;
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
            var app = CreateAppContext();

            var pathName = @"x:\src\age.f";
            var exportsPathName = @"x:\bin\.dkx\age.f.exports";
            var job = new ScanWbdkExportFileJob(app, pathName, exportsPathName, FileContext.Function);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(app.FileSystem.GetFileText(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            Assert.IsNotNull(model.Exports);
            Assert.AreEqual(1, model.Exports.Length);

            var export = model.Exports[0];
            Assert.AreEqual("age", export.Name);
            Assert.IsNull(export.ClassName);
            Assert.AreEqual("unsigned(3)", export.ReturnDataType);

            Assert.IsNotNull(export.Arguments);
            Assert.AreEqual(1, export.Arguments.Length);
            ValidateExportArgument(export.Arguments[0], "bdate", "date", false, false);
        }

        [Test]
        public async Task GatewayClassFile()
        {
            var app = CreateAppContext();

            var pathName = @"x:\src\gateway\gateway.cc";
            var exportsPathName = @"x:\bin\.dkx\gateway\gateway.cc.exports";
            var job = new ScanWbdkExportFileJob(app, pathName, exportsPathName, FileContext.ClientClass);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(app.FileSystem.GetFileText(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            Assert.IsNotNull(model.Exports);
            Assert.AreEqual(2, model.Exports.Length);

            var export = model.Exports[0];
            Assert.AreEqual("Start", export.Name);
            Assert.AreEqual("gateway", export.ClassName);
            Assert.AreEqual("int", export.ReturnDataType);
            Assert.IsNull(export.Arguments);

            export = model.Exports[1];
            Assert.AreEqual("Stop", export.Name);
            Assert.AreEqual("gateway", export.ClassName);
            Assert.AreEqual("int", export.ReturnDataType);
            Assert.IsNull(export.Arguments);
        }

        [Test]
        public async Task UtilClassFile()
        {
            var app = CreateAppContext();

            var pathName = @"x:\src\util.nc";
            var exportsPathName = @"x:\bin\.dkx\util.nc.exports";
            var job = new ScanWbdkExportFileJob(app, pathName, exportsPathName, FileContext.NeutralClass);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(app.FileSystem.GetFileText(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            Assert.IsNotNull(model.Exports);
            Assert.AreEqual(1, model.Exports.Length);

            var export = model.Exports[0];
            Assert.AreEqual("StringIsBlankOrNull", export.Name);
            Assert.AreEqual("util", export.ClassName);
            Assert.AreEqual("bool", export.ReturnDataType);
            Assert.IsNotNull(export.Arguments);
            Assert.AreEqual(1, export.Arguments.Length);
            ValidateExportArgument(export.Arguments[0], "str", "string", false, false);
        }

        private void ValidateExportArgument(WbdkExportArgument arg, string name, string dataType, bool isRef, bool isOut)
        {
            Assert.AreEqual(name, arg.Name);
            Assert.AreEqual(dataType, arg.DataType);
            Assert.AreEqual(isRef, arg.Ref);
            Assert.AreEqual(isOut, arg.Out);
        }
    }
}
