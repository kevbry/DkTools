using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.Files;
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
            var fileContext = FileContext.NeutralClass;

            fs.WriteFileText(dkxPathName, source);

            var queue = new TestJobQueue();
            var compileJob = new CompileFileJob(app, queue, dkxPathName, wbdkPathName, objPathName, fileContext);
            await compileJob.ExecuteAsync(cancel: default);

            foreach (var item in queue.ReportItems)
            {
                TestContext.Out.WriteLine($"> {item}");
            }
            Assert.IsFalse(queue.ReportItems.Any(i => i.Severity == ErrorSeverity.Error));

            Assert.IsTrue(fs.FileExists(objPathName), "Object file was not created.");
            Assert.IsFalse(fs.FileExists(wbdkPathName), "WBDK file should not have been created.");

            var objContent = fs.GetFileText(objPathName);
            var obj = JsonConvert.DeserializeObject<ObjectFileModel>(objContent);
            Assert.IsNotNull(obj);

            Assert.AreEqual(dkxPathName, obj.SourcePathName);
            Assert.AreEqual(wbdkPathName, obj.DestinationPathName);

            return obj;
        }

        private async Task<TestJobQueue> SetupCodeError(DkAppContext app, string source)
        {
            var fs = app.FileSystem;

            var dkxPathName = @"x:\src\test.ncx";
            var wbdkPathName = @"x:\src\test.nc";
            var objPathName = @"x:\bin\.dkx\test.ncx.dkxx";
            var fileContext = FileContext.NeutralClass;

            fs.WriteFileText(dkxPathName, source);

            var queue = new TestJobQueue();
            var compileJob = new CompileFileJob(app, queue, dkxPathName, wbdkPathName, objPathName, fileContext);
            await compileJob.ExecuteAsync(cancel: default);

            foreach (var item in queue.ReportItems)
            {
                TestContext.Out.WriteLine($"> {item}");
            }

            return queue;
        }

        [Test]
        public async Task SimpleMethod()
        {
            var app = CreateAppContext();
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
            Assert.AreEqual(Privacy.Private, method.Privacy);
            Assert.AreEqual("void", method.ReturnDataType);
            Assert.IsNull(method.Arguments);
        }

        // TODO: method with arguments

        [Test]
        public async Task SimpleReadOnlyProperty()
        {
            var app = CreateAppContext();
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
            Assert.AreEqual(Privacy.Public, prop.Privacy);
            Assert.AreEqual("int", prop.DataType);
            Assert.AreEqual(true, prop.ReadOnly);
        }

        [Test]
        public async Task SimpleReadWriteProperty()
        {
            var app = CreateAppContext();
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    public int Id
    {
        get
        {
            return 0;
        }
        set
        {
            _id = value;
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
            Assert.AreEqual(Privacy.Public, prop.Privacy);
            Assert.AreEqual("int", prop.DataType);
            Assert.AreEqual(false, prop.ReadOnly);
        }

        [Test]
        public async Task PropertyWithNoGetter()
        {
            var app = CreateAppContext();
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
            var obj = await SetupCodeSuccess(app, @"
class Test
{
    private int _id;
    private string _name;
    public unsigned(9) Rowno;
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

        // TODO: constants
    }
}
