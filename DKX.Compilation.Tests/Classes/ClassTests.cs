using DKX.Compilation.DataTypes;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Classes
{
    [TestFixture]
    class ClassTests : CompileTestClass
    {
        [Test]
        public async Task MemberClass()
        {
            var app = await SetupCompileSingle(@"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private int _no;
        private string _name;
        private static string _inst;

        public int Number { get { return _no; } }
        public string Name { get { return _name; } set { _name = value; } }
        public static string Inst { get { return _inst; } set { _inst = value; } }

        public void SetData(int no, string name)
        {
            _no = no;
            _name = name;
        }

        public static void SetInstitution(string inst)
        {
            _inst = inst;
        }
    }

    static class User
    {
        public static void DoTest()
        {
            int no;
            Member mbr = new Member();
            no = mbr.Number;
            mbr.SetData(123, ""John"");
            string name = mbr.Name;
            name = ""Bob"";
            mbr.Name = name;
            mbr.Name = ""Bob2"";
        }
    }
}
");
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

char(255) _inst;

int GetNumber(unsigned int this) { return dkx_getint4(this, 0); }
char(255) GetName(unsigned int this) { return dkx_getstr(this, 4); }
void SetName(unsigned int this, char(255) value) { dkx_setstr(this, 4, value); }
char(255) GetInst() { return _inst; }
void SetInst(char(255) value) { _inst = value; }

void SetData_%1(unsigned int this, int no, char(255) name)
{
    dkx_setint4(this, 0, no);
    dkx_setstr(this, 4, name);
}

void SetInstitution_%2(char(255) inst)
{
    _inst = inst;
}
".Replace("%1", Compiler.ComputeHash("void, int, string").ToString("X"))
.Replace("%2", Compiler.ComputeHash("void, string").ToString("X"))
);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.User")}.nc", @"
// Test.User

void DoTest_%0()
{
    int no;
    unsigned int mbr;
    char(255) name;
    mbr = dkx_addref(dkx_new(516));
    no = %M.GetNumber(mbr);
    %M.SetData_%1(mbr, 123, ""John"");
    name = %M.GetName(mbr);
    name = ""Bob"";
    %M.SetName(mbr, name);
    %M.SetName(mbr, ""Bob2"");
    dkx_release(mbr);
}
".Replace("%0", Compiler.ComputeHash("void").ToString("X"))
.Replace("%1", Compiler.ComputeHash("void, int, string").ToString("X"))
.Replace("%M", Compiler.GetWbdkClassName("Test.Member"))
);
        }

        [Test]
        public async Task StaticMethodCall()
        {
            var app = await SetupCompileSingle(@"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private static string _inst;

        public static void SetInstitution(string inst)
        {
            _inst = inst;
        }
    }

    static class User
    {
        public static void DoTest()
        {
            Member.SetInstitution(""Bank1"");
        }
    }
}
");
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

char(255) _inst;

void SetInstitution_%1(char(255) inst)
{
    _inst = inst;
}
".Replace("%1", Compiler.ComputeHash("void, string").ToString("X"))
);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.User")}.nc", @"
// Test.User

void DoTest_%2()
{
    %M.SetInstitution_%1(""Bank1"");
}
".Replace("%1", Compiler.ComputeHash("void, string").ToString("X"))
.Replace("%2", Compiler.ComputeHash("void").ToString("X"))
.Replace("%M", Compiler.GetWbdkClassName("Test.Member"))
);
        }

        [Test]
        public async Task AcrossFiles()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await app.FileSystem.WriteFileTextAsync("x:\\src\\Member.dkx", @"
namespace Test
{
    public class Member
    {
        private int _no;
        private string _name;

        public int Number { get { return _no; } }
        public string Name { get { return _name; } set { _name = value; } }
        public int Update() { return 0; }
    }
}
");
            await app.FileSystem.WriteFileTextAsync("x:\\src\\UnitTest.dkx", @"
namespace Test
{
    public class UnitTest
    {
        public static void DoTest()
        {
            Member mbr = new Member();
            mbr.Name = ""Bob"";
            mbr.Update();
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member
int GetNumber(unsigned int this) { return dkx_getint4(this, 0); }
char(255) GetName(unsigned int this) { return dkx_getstr(this, 4); }
void SetName(unsigned int this, char(255) value) { dkx_setstr(this, 4, value); }
int Update_%1(unsigned int this) { return 0; }
"
.Replace("%1", Compiler.ComputeHash("int").ToString("X"))
);

            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_%1()
{
    unsigned int mbr;
    mbr = dkx_addref(dkx_new(516));
    %M.SetName(mbr, ""Bob"");
    %M.Update_%2(mbr);
    dkx_release(mbr);
}
"
.Replace("%1", Compiler.ComputeHash("void").ToString("X"))
.Replace("%2", Compiler.ComputeHash("int").ToString("X"))
.Replace("%M", Compiler.GetWbdkClassName("Test.Member"))
);
            await DumpBsonFileAsync(app, DkxProjectPathName);

            var fileDeps = await GetFileDependenciesAsync(app, $"x:\\src\\UnitTest.dkx");
            Assert.AreEqual(1, fileDeps.Length);
            Assert.AreEqual("x:\\src\\member.dkx", fileDeps[0].ToLower());
        }

        [Test]
        public async Task UsingThisToAccessMemberVariable()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private int no;

        public void SetData(int no)
        {
            this.no = no;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

void SetData_${SetDataDecoration}(unsigned int this, int no)
{
    dkx_setint4(this, 0, no);
}
".Replace("${SetDataDecoration}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task UsingThisToAccessStaticMemberVariable()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private static int no;

        public void SetData(int no)
        {
            this.no = no;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticMemberCannotHaveObjectReference);
        }

        [Test]
        public async Task UsingThisToAccessProperty()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private int no;

        public int No { get { return no; } set { no = value; } }

        public void SetData(int no)
        {
            this.No = no;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

int GetNo(unsigned int this) { return dkx_getint4(this, 0); }
void SetNo(unsigned int this, int value) { dkx_setint4(this, 0, value); }

void SetData_${SetDataDecoration}(unsigned int this, int no)
{
    ${ClassName}.SetNo(this, no);
}
"
.Replace("${ClassName}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${SetDataDecoration}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task UsingThisToAccessMethod()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private int no;

        public void SetNo(int no) { this.no = no; }

        public void SetData(int no)
        {
            this.SetNo(no);
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

void SetNo_${SetNoDecoration}(unsigned int this, int no) { dkx_setint4(this, 0, no); }

void SetData_${SetDataDecoration}(unsigned int this, int no)
{
    ${ClassName}.SetNo_${SetNoDecoration}(this, no);
}
"
.Replace("${ClassName}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${SetNoDecoration}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { DataType.Int }))
.Replace("${SetDataDecoration}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task CantCallStaticMethodWithThis()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        public static void SetData() { }

        public void DoTest()
        {
            this.SetData();
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticMemberCannotHaveObjectReference);
        }

        [Test]
        public async Task CantCallStaticMethodWithObjectReference()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        public static void SetData() { }
    }

    class UnitTest
    {
        public void DoTest()
        {
            var mbr = new Member();
            mbr.SetData();
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticMemberCannotHaveObjectReference);
        }

        [Test]
        public async Task CallStaticMethodFromNonStaticCode()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        public static void SetData() { }

        public void DoTest()
        {
            SetData();
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

void SetData_${SetDataDecoration}() { }

void DoTest_${DoTestDecoration}(unsigned int this)
{
    ${ClassName}.SetData_${SetDataDecoration}();
}
"
.Replace("${ClassName}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${SetDataDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task CantCallNonStaticMethodWithStaticClassName()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        public void SetData() { }
    }

    class UnitTest
    {
        public void DoTest()
        {
            Member.SetData();
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.MemberRequiresAnObjectReference);
        }

        [Test]
        public async Task PropertyWithSameNameAsDataType()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    class Member
    {
        private int _no;
        public int No { get { return _no; } }
    }

    class Party
    {
        private Member _mbr;
        public Member Member { get { return _mbr; } }

        public void PartyTest()
        {
            var mbr = Member;
        }
    }

    class UnitTest
    {
        public static void DoTest()
        {
            var party = new Party();
            var mbr = party.Member;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member
int GetNo(unsigned int this) { return dkx_getint4(this, 0); }
");
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Party")}.nc", @"
// Test.Party
unsigned int GetMember(unsigned int this) { return dkx_getuns4(this, 0); }
void PartyTest_${PartyTestDecoration}(unsigned int this)
{
    unsigned int mbr;
    mbr = dkx_addref(${PartyClass}.GetMember(this));
    dkx_release(mbr);
}
"
.Replace("${PartyClass}", Compiler.GetWbdkClassName("Test.Party"))
.Replace("${PartyTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
            await ValidateOutputAsync(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
// Test.UnitTest
void DoTest_${DoTestDecoration}()
{
    unsigned int party;
    unsigned int mbr;
    party = dkx_addref(dkx_new(4));
    mbr = dkx_addref(${PartyClass}.GetMember(party));
    dkx_release(party);
    dkx_release(mbr);
}
"
.Replace("${PartyClass}", Compiler.GetWbdkClassName("Test.Party"))
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task StaticClassesCannotHaveNonStaticMemberVariables()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private int _no;
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticClassesCannotHaveNonStaticMembers);
        }

        [Test]
        public async Task StaticClassesCannotHaveNonStaticMethods()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private void DoSomething() { }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticClassesCannotHaveNonStaticMembers);
        }

        [Test]
        public async Task StaticClassesCannotHaveNonStaticProperties()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private int Number { get { return 0; } }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticClassesCannotHaveNonStaticMembers);
        }

        [Test]
        public async Task CannotReadPrivateProperty()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private static int Number { get { return 0; } }
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            var no = Member.Number;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessMemberDueToPrivacy);
        }

        [Test]
        public async Task CannotWriteToPrivateProperty()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private static int _no;
        private static int Number { get { return _no; } set { _no = value; } }
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            Member.Number = 0;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessMemberDueToPrivacy);
        }

        [Test]
        public async Task CannotCallPrivateMethod()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private static void DoStuff() { }
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            Member.DoStuff();
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessMemberDueToPrivacy);
        }

        [Test]
        public async Task CannotAccessPrivateMemberVariable()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class Member
    {
        private static int _no;
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            Member._no = 0;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessMemberDueToPrivacy);
        }

        // TODO: Property where the getter/setter conflicts with another method (e.g. property Id conflicts with GetId())
        // TODO: Class cannot have same name as namespace
        // TODO: 2 classes cannot have the same name (in same file or different files)
        // TODO: Call class in another namespace with just the class name (using statement at top)
        // TODO: Cannot instantiate a static class ('new' keyword)
        // TODO: Cannot write to read-only property
        // TODO: private property cannot be marked public or protected
    }
}
