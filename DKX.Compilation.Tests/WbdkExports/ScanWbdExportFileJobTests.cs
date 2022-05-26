using DK.Code;
using DKX.Compilation.Tests.Schema;
using DKX.Compilation.WbdkExports;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.WbdkExports
{
    [TestFixture]
    class ScanWbdExportFileJobTests : CompileTestClass
    {
        [Test]
        public async Task AgeFunctionFile()
        {
            var app = CreateAppContext();

            var pathName = @"x:\src\age.f";
            var exportsPathName = @"x:\bin\.dkx\age.f.wbdkx";
            var tableHashProvider = new TestTableHashProvider();
            var job = new ScanWbdkExportFileJob(app, pathName, exportsPathName, FileContext.Function, tableHashProvider);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(await app.FileSystem.ReadFileTextAsync(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            // Validate exports
            Assert.IsNotNull(model.Exports);
            Assert.AreEqual(1, model.Exports.Length);

            var export = model.Exports[0];
            Assert.AreEqual("age", export.Name);
            Assert.IsNull(export.ClassName);
            Assert.AreEqual("unsigned(3)", export.ReturnDataType);

            Assert.IsNotNull(export.Arguments);
            Assert.AreEqual(1, export.Arguments.Length);
            ValidateExportArgument(export.Arguments[0], "bdate", "date", false, false);

            // Validate table dependencies
            Assert.IsNotNull(model.TableDependencies);
            Assert.AreEqual(1, model.TableDependencies.Length);

            var td = model.TableDependencies[0];
            Assert.AreEqual("cust", td.TableName);
            Assert.AreEqual(tableHashProvider.GetTableHash("cust"), td.Hash);
        }

        [Test]
        public async Task GatewayClassFile()
        {
            var app = CreateAppContext();

            var pathName = @"x:\src\gateway\gateway.cc";
            var exportsPathName = @"x:\bin\.dkx\gateway\gateway.cc.wbdkx";
            var tableHashProvider = new TestTableHashProvider();
            var job = new ScanWbdkExportFileJob(app, pathName, exportsPathName, FileContext.ClientClass, tableHashProvider);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(await app.FileSystem.ReadFileTextAsync(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            // Validate exports
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

            // Validate table dependencies
            Assert.IsNull(model.TableDependencies);
        }

        [Test]
        public async Task UtilClassFile()
        {
            var app = CreateAppContext();

            var pathName = @"x:\src\util.nc";
            var exportsPathName = @"x:\bin\.dkx\util.nc.wbdkx";
            var tableHashProvider = new TestTableHashProvider();
            var job = new ScanWbdkExportFileJob(app, pathName, exportsPathName, FileContext.NeutralClass, tableHashProvider);
            await job.ExecuteAsync(cancel: default);

            var model = JsonConvert.DeserializeObject<WbdkExportsModel>(await app.FileSystem.ReadFileTextAsync(exportsPathName));
            Assert.IsNotNull(model);

            Assert.AreEqual(pathName, model.SourceFile);

            // Validate exports
            Assert.IsNotNull(model.Exports);
            Assert.AreEqual(2, model.Exports.Length);

            var export = model.Exports[0];
            Assert.AreEqual("StringIsBlankOrNull", export.Name);
            Assert.AreEqual("util", export.ClassName);
            Assert.AreEqual("bool", export.ReturnDataType);
            Assert.IsNotNull(export.Arguments);
            Assert.AreEqual(1, export.Arguments.Length);
            ValidateExportArgument(export.Arguments[0], "str", "string", false, false);

            export = model.Exports[1];
            Assert.AreEqual("GetInstitutionName", export.Name);
            Assert.AreEqual("util", export.ClassName);
            Assert.AreEqual("string(80)", export.ReturnDataType);
            Assert.IsNull(export.Arguments);

            // Validate table dependencies
            Assert.IsNotNull(model.TableDependencies);
            Assert.AreEqual(1, model.TableDependencies.Length);

            var td = model.TableDependencies[0];
            Assert.AreEqual("info", td.TableName);
            Assert.AreEqual(tableHashProvider.GetTableHash("info"), td.Hash);
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
