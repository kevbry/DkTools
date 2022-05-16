using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Files
{
    [TestFixture]
    class CompileFileJobTests : CompileTestClass
    {
        private async Task<ObjectFileModel> SetupCodeSuccess(DkAppContext app, string source)
        {
            var fs = app.FileSystem;

            var dkxPathName = @"x:\src\test.ncx";
            var wbdkPathName = @"x:\src\test.nc";
            var objPathName = @"x:\bin\.dkx\test.ncx.dkxx";

            fs.WriteFileText(dkxPathName, source);

            var queue = new TestJobQueue();
            var compileJob = new CompileFileJob(app, dkxPathName, objPathName, queue);
            await compileJob.ExecuteAsync(cancel: default);

            foreach (var item in queue.ReportItems)
            {
                await TestContext.Out.WriteLineAsync($"> {item}");
            }
            Assert.IsFalse(queue.ReportItems.Any(i => i.Severity == ErrorSeverity.Error), "Compiler returned errors.");

            Assert.IsTrue(fs.FileExists(objPathName), "Object file was not created.");
            Assert.IsFalse(fs.FileExists(wbdkPathName), "WBDK file should not have been created.");

            var objContent = fs.GetFileText(objPathName);
            await TestContext.Out.WriteLineAsync($"Object File:\n{objContent}");
            var obj = JsonConvert.DeserializeObject<ObjectFileModel>(objContent);
            Assert.IsNotNull(obj);

            Assert.AreEqual(dkxPathName, obj.SourcePathName);

            return obj;
        }

        private async Task<TestJobQueue> SetupCodeError(DkAppContext app, string source)
        {
            var fs = app.FileSystem;

            var dkxPathName = @"x:\src\test.ncx";
            var objPathName = @"x:\bin\.dkx\test.ncx.dkxx";

            fs.WriteFileText(dkxPathName, source);

            var queue = new TestJobQueue();
            var compileJob = new CompileFileJob(app, dkxPathName, objPathName, queue);
            await compileJob.ExecuteAsync(cancel: default);

            foreach (var item in queue.ReportItems)
            {
                await TestContext.Out.WriteLineAsync($"> {item}");
            }

            return queue;
        }

        [Test]
        public async Task SimpleMethod()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    void DoNothing()
    {
    }
}
");

            Assert.AreEqual("Test", obj.ClassName);
            Assert.IsNull(obj.FileDependencies);
            Assert.IsNull(obj.TableDependencies);
            Assert.IsNull(obj.Properties);

            Assert.IsNotNull(obj.Methods);
            Assert.AreEqual(1, obj.Methods.Length);

            var method = obj.Methods[0];
            Assert.AreEqual("DoNothing", method.Name);
            Assert.AreEqual(Privacy.Public, method.Privacy);
            Assert.AreEqual("void", method.ReturnDataType);
            Assert.IsNull(method.Arguments);
        }

        [Test]
        public async Task MethodWithArguments()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    public int GetSomeRecord(int id, string(38) name, ref uint counter, out date dateNew, out time timeNew)
    {
    }
}
");

            Assert.AreEqual("Test", obj.ClassName);
            Assert.IsNull(obj.FileDependencies);
            Assert.IsNull(obj.TableDependencies);
            Assert.IsNull(obj.Properties);

            Assert.IsNotNull(obj.Methods);
            Assert.AreEqual(1, obj.Methods.Length);

            var method = obj.Methods[0];
            Assert.AreEqual("GetSomeRecord", method.Name);
            Assert.AreEqual(Privacy.Public, method.Privacy);
            Assert.AreEqual("int", method.ReturnDataType);
            Assert.IsNotNull(method.Arguments);
            Assert.AreEqual(5, method.Arguments.Length);

            var arg = method.Arguments[0];
            Assert.AreEqual("id", arg.Name);
            Assert.AreEqual("int", arg.DataType);
            Assert.AreEqual(ArgumentPassType.ByValue, arg.PassType);

            arg = method.Arguments[1];
            Assert.AreEqual("name", arg.Name);
            Assert.AreEqual("string(38)", arg.DataType);
            Assert.AreEqual(ArgumentPassType.ByValue, arg.PassType);

            arg = method.Arguments[2];
            Assert.AreEqual("counter", arg.Name);
            Assert.AreEqual("uint", arg.DataType);
            Assert.AreEqual(ArgumentPassType.ByReference, arg.PassType);

            arg = method.Arguments[3];
            Assert.AreEqual("dateNew", arg.Name);
            Assert.AreEqual("date", arg.DataType);
            Assert.AreEqual(ArgumentPassType.Out, arg.PassType);

            arg = method.Arguments[4];
            Assert.AreEqual("timeNew", arg.Name);
            Assert.AreEqual("time", arg.DataType);
            Assert.AreEqual(ArgumentPassType.Out, arg.PassType);
        }

        [Test]
        public async Task SimpleReadOnlyProperty()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    public int Zero
    {
        get
        {
            return 0;
        }
    }
}
");

            Assert.AreEqual("Test", obj.ClassName);
            Assert.IsNull(obj.FileDependencies);
            Assert.IsNull(obj.TableDependencies);
            Assert.IsNull(obj.Methods);

            Assert.IsNotNull(obj.Properties);
            Assert.AreEqual(1, obj.Properties.Length);

            var prop = obj.Properties[0];
            Assert.AreEqual("Zero", prop.Name);
            Assert.AreEqual("int", prop.DataType);
            Assert.AreEqual(true, prop.ReadOnly);

            Assert.IsNotNull(prop.Getters, "Getters array is null.");
            Assert.AreEqual(1, prop.Getters.Length, "Wrong number of getters.");
            var getter = prop.Getters[0];
            Assert.AreEqual(FileContext.NeutralClass, getter.FileContext);
            Assert.AreEqual(Privacy.Public, getter.Privacy);

            Assert.IsNull(prop.Setters);
        }

        [Test]
        public async Task SimpleReadWriteProperty()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    public int Id
    {
        get
        {
        }
        set
        {
        }
    }
}
");

            Assert.AreEqual("Test", obj.ClassName);
            Assert.IsNull(obj.FileDependencies);
            Assert.IsNull(obj.TableDependencies);
            Assert.IsNull(obj.Methods);

            Assert.IsNotNull(obj.Properties);
            Assert.AreEqual(1, obj.Properties.Length);

            var prop = obj.Properties[0];
            Assert.AreEqual("Id", prop.Name);
            Assert.AreEqual("int", prop.DataType);
            Assert.AreEqual(false, prop.ReadOnly);
        }

        [Test]
        public async Task PropertyWithNoGetter()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var queue = await SetupCodeError(app, @"
class Test
{
    public int Id
    {
        set
        {
            _id = value;
        }
    }
}
");
            Assert.True(queue.ReportItems.Any(i => i.Code == ErrorCode.PropertyHasNoGetter));
        }

        [Test]
        public async Task PropertyWithNoGetterOrSetter()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var queue = await SetupCodeError(app, @"
class Test
{
    public int Id
    {
    }
}
");
            Assert.True(queue.ReportItems.Any(i => i.Code == ErrorCode.PropertyHasNoGetterOrSetter));
        }

        [Test]
        public async Task MemberVariables()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    private int _id;
    private string _name;
    unsigned(9) Rowno;
}
");

            Assert.AreEqual("Test", obj.ClassName);
            Assert.IsNull(obj.FileDependencies);
            Assert.IsNull(obj.TableDependencies);
            Assert.IsNull(obj.Properties);

            Assert.IsNull(obj.Methods);
            Assert.IsNull(obj.Properties);

            Assert.IsNotNull(obj.MemberVariables);
            Assert.AreEqual(3, obj.MemberVariables.Length);

            var mv = obj.MemberVariables[0];
            Assert.AreEqual("_id", mv.Name);
            Assert.AreEqual("int", mv.DataType);

            mv = obj.MemberVariables[1];
            Assert.AreEqual("_name", mv.Name);
            Assert.AreEqual("string", mv.DataType);

            mv = obj.MemberVariables[2];
            Assert.AreEqual("Rowno", mv.Name);
            Assert.AreEqual("unsigned(9)", mv.DataType);
        }

        [TestCase("public")]
        [TestCase("protected")]
        public async Task MemberVariablesCanOnlyBePrivate(string privacy)
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var queue = await SetupCodeError(app, @"
class Test
{
    " + privacy + @" int Id;
}
");
            Assert.True(queue.ReportItems.Any(i => i.Code == ErrorCode.MemberVariableMustBePrivate));
        }

        [Test]
        public async Task DuplicateVariableNames()
        {
            var app = CreateAppContext();
            SetupCompileFiles(app);
            app.LoadAppSettings();
            var queue = await SetupCodeError(app, @"
class Test
{
    int Temp;
    string Temp { get { return """"; } }
    const int Temp = 0;
}
");
            Assert.True(queue.ReportItems.Any(i => i.Code == ErrorCode.DuplicateVariable));
        }
    }
}
