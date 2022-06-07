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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
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
            await ValidateOutputAsync(app, "Test.User", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.User")}.nc", @"
void DoTest_%0()
{
    int no;
    unsigned int mbr;
    char(255) name;
    mbr = dkx_new(516);
    no = %M.GetNumber(mbr);
    %M.SetData_%1(mbr, 123, ""John"");
    name = %M.GetName(mbr);
    name = ""Bob"";
    %M.SetName(mbr, name);
    %M.SetName(mbr, ""Bob2"");
    (void)dkx_release(mbr);
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
char(255) _inst;

void SetInstitution_%1(char(255) inst)
{
    _inst = inst;
}
".Replace("%1", Compiler.ComputeHash("void, string").ToString("X"))
);
            await ValidateOutputAsync(app, "Test.User", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.User")}.nc", @"
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
int GetNumber(unsigned int this) { return dkx_getint4(this, 0); }
char(255) GetName(unsigned int this) { return dkx_getstr(this, 4); }
void SetName(unsigned int this, char(255) value) { dkx_setstr(this, 4, value); }
int Update_%1(unsigned int this) { return 0; }
"
.Replace("%1", Compiler.ComputeHash("int").ToString("X"))
);

            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void DoTest_%1()
{
    unsigned int mbr;
    mbr = dkx_new(516);
    %M.SetName(mbr, ""Bob"");
    (void)%M.Update_%2(mbr);
    (void)dkx_release(mbr);
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
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
    class Member
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
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
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
int GetNo(unsigned int this) { return dkx_getint4(this, 0); }
");
            await ValidateOutputAsync(app, "Test.Party", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Party")}.nc", @"
unsigned int GetMember(unsigned int this)
{
    return dkx_addref(dkx_getuns4(this, 0));
}
void PartyTest_${PartyTestDecoration}(unsigned int this)
{
    unsigned int mbr;
    mbr = ${PartyClass}.GetMember(this);
    (void)dkx_release(mbr);
}
"
.Replace("${PartyClass}", Compiler.GetWbdkClassName("Test.Party"))
.Replace("${PartyTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void DoTest_${DoTestDecoration}()
{
    unsigned int party;
    unsigned int mbr;
    party = dkx_new(4);
    mbr = ${PartyClass}.GetMember(party);
    (void)dkx_release(party);
    (void)dkx_release(mbr);
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

        [Test]
        public async Task ClassNameCannotMatchNamespace()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test.Member
{
}

namespace Test
{
    static class Member
    {
        private static int _no;
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.ClassNameConflictsWithNamespace);
        }

        [Test]
        public async Task ClassNameCannotMatchNamespacePrefix()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test.Member.Accounts
{
}

namespace Test
{
    static class Member
    {
        private static int _no;
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.ClassNameConflictsWithNamespace);
        }

        [Test]
        public async Task DuplicateClassName()
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

    class Member
    {
        private int _name;
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateClass);
        }

        [Test]
        public async Task DuplicateClassNameInDifferentFiles()
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
}
");
            await SetCompileFileAsync(app, @"x:\src\Test2.dkx", @"
namespace Test
{
    class Member
    {
        private int _name;
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateClass);
        }

        [Test]
        public async Task UsingNamespace()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    static class UnitTest
    {
        public static void DoTest()
        {
            Console.WriteLine(""Hello!"");
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void DoTest_${DoTestDecoration}()
{
    puts(""Hello!"");
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
);
        }

        [Test]
        public async Task CantInstantiateStaticClass()
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
            var mbr = new Member();
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.StaticClassCannotBeInstantiated);
        }

        [Test]
        public async Task CantInstantiatePrimitiveDataType()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
namespace Test
{
    static class UnitTest
    {
        public static void DoTest()
        {
            var mbr = new int();
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DataTypeCannotBeInstantiated);
        }

        [Test]
        public async Task CannotWriteToReadOnlyProperty()
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
        public int Number { get { return _no; } }
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            var mbr = new Member();
            mbr.Number = 123;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.PropertyIsReadOnly);
        }

        [TestCase("public")]
        [TestCase("protected")]
        public async Task PropertyGetterCannotBeMarkedPublicOrProtected(string privacy)
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
        private int Number { ${Privacy} get { return _no; } }
    }
}
"
.Replace("${Privacy}", privacy)
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.PropertyAccessorMoreAccessibleThanProperty);
        }

        [TestCase("public")]
        [TestCase("protected")]
        public async Task PropertySetterCannotBeMarkedPublicOrProtected(string privacy)
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
        private int Number { get { return _no; } ${Privacy} set { _no = value; } }
    }
}
"
.Replace("${Privacy}", privacy)
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.PropertyAccessorMoreAccessibleThanProperty);
        }

        [Test]
        public async Task PropertyWithPrivateSetterCanBeRead()
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
        public int Number { get { return _no; } private set { _no = value; } }
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            var mbr = new Member();
            var no = mbr.Number;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void DoTest_${DoTestDecoration}()
{
    unsigned int mbr;
    int no;
    mbr = dkx_new(4);
    no = ${MemberClass}.GetNumber(mbr);
    (void)dkx_release(mbr);
}
"
.Replace("${DoTestDecoration}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
);
        }

        [Test]
        public async Task PropertyWithPrivateSetterCannotBeWrittenTo()
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
        public int Number { get { return _no; } private set { _no = value; } }
    }

    static class UnitTest
    {
        public static void DoTest()
        {
            var mbr = new Member();
            mbr.Number = 123;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessMemberDueToPrivacy);
        }

        [Test]
        public async Task PropertyWithDuplicateGetter()
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
        public int Number { get { return _no; } get { return _no; } }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicatePropertyGetter);
        }

        [Test]
        public async Task PropertyWithDuplicateSetter()
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
        public int Number { get { return _no; } set { _no = value; } set { _no = value } }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicatePropertySetter);
        }

        [Test]
        public async Task PropertyWithNoGetter()
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
        public int Number { set { _no = value } }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.PropertyHasNoGetter);
        }

        [Test]
        public async Task PropertyWithNoGetterOrSetter()
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
        public int Number { }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.PropertyHasNoGetterOrSetter);
        }

        [Test]
        public async Task DuplicateMethod()
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

        public void DoTest() { }
        public void DoTest() { }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.DuplicateMethod);
        }

        [Test]
        public async Task MethodOverloading()
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

        public void DoTest() { }
        public void DoTest(int no) { _no = no; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member();
            mbr.DoTest(123);
            mbr.DoTest();
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
void DoTest_${DoTestDeco1}(unsigned int this)
{
}
void DoTest_${DoTestDeco2}(unsigned int this, int no)
{
    dkx_setint4(this, 0, no);
}
"
.Replace("${DoTestDeco1}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${DoTestDeco2}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { DataType.Int }))
);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    unsigned int mbr;
    mbr = dkx_new(4);
    ${MemberClass}.DoTest_${DoTestDeco2}(mbr, 123);
    ${MemberClass}.DoTest_${DoTestDeco1}(mbr);
    (void)dkx_release(mbr);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${DoTestDeco1}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${DoTestDeco2}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task Constructor()
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

        public Member(int no) { _no = no; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member(123);
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
unsigned int Member_${CtorDeco}(int no)
{
    unsigned int this;
    this = dkx_new(4);
    dkx_setint4(this, 0, no);
    return this;
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int }))
);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    unsigned int mbr;
    mbr = ${MemberClass}.Member_${CtorDeco}(123);
    (void)dkx_release(mbr);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task MultipleConstructors()
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
        private string _name;

        public Member(int no) { _no = no; _name = ""unknown""; }
        public Member(int no, string name) { _no = no; _name = name; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member(123);
            mbr = new Member(456, ""Julio"");
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
unsigned int Member_${Ctor1Deco}(int no)
{
    unsigned int this;
    this = dkx_new(516);
    dkx_setint4(this, 0, no);
    dkx_setstr(this, 4, ""unknown"");
    return this;
}

unsigned int Member_${Ctor2Deco}(int no, char(255) name)
{
    unsigned int this;
    this = dkx_new(516);
    dkx_setint4(this, 0, no);
    dkx_setstr(this, 4, name);
    return this;
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${Ctor1Deco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int }))
.Replace("${Ctor2Deco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int, DataType.String255 }))
);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    unsigned int mbr;
    mbr = ${MemberClass}.Member_${Ctor1Deco}(123);
    mbr = dkx_swapnoadd(mbr, ${MemberClass}.Member_${Ctor2Deco}(456, ""Julio""));
    (void)dkx_release(mbr);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${Ctor1Deco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int }))
.Replace("${Ctor2Deco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int, DataType.String255 }))
);
        }

        [Test]
        public async Task CannotCallConstructorDirectly()
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
        private string _name;

        public Member(int no) { _no = no; _name = ""unknown""; }
        public Member(int no, string name) { _no = no; _name = name; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member(123);
            mbr = mbr.Member(456);
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.MethodNotCallable);
        }

        [Test]
        public async Task CannotUseDefaultConstructorWhenNotDefined()
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
        private string _name;

        public Member(int no) { _no = no; _name = ""unknown""; }
        public Member(int no, string name) { _no = no; _name = name; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.NoConstructorWithSameNumberOfArguments);
        }

        [TestCase("private")]
        [TestCase("protected")]
        public async Task PrivateDefaultConstructor(string privacy)
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
        private string _name;

        ${privacy} Member() { }
        public Member(int no) { _no = no; _name = ""unknown""; }
        public Member(int no, string name) { _no = no; _name = name; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member;
        }
    }
}
"
.Replace("${privacy}", privacy)
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessConstructorDueToPrivacy);
        }

        [TestCase("private")]
        [TestCase("protected")]
        public async Task PrivateNonDefaultConstructor(string privacy)
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
        private string _name;

        public Member() { }
        ${privacy} Member(int no) { _no = no; _name = ""unknown""; }
        public Member(int no, string name) { _no = no; _name = name; }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = new Member(123);
        }
    }
}
"
.Replace("${privacy}", privacy)
);
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.CannotAccessConstructorDueToPrivacy);
        }

        [Test]
        public async Task ObjectReferencesWithMemberVariables()
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
        private string _name;

        public Member(int no) { _no = no; _name = ""unknown""; }
    }

    class UnitTest
    {
        Member _mbr1;
        Member _mbr2;

        public void Run()
        {
            var mbr = new Member(123);
            _mbr1 = mbr;
            _mbr2 = new Member(456);
            _mbr1 = _mbr2;
            _mbr2 = null;
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}(unsigned int this)
{
    unsigned int mbr;
    mbr = ${MemberClass}.Member_${CtorDeco}(123);
    dkx_setuns4(this, 0, dkx_swaplink(this, dkx_getuns4(this, 0), mbr));
    dkx_setuns4(this, 4, dkx_releasedefer(dkx_swaplink(this, dkx_getuns4(this, 4), ${MemberClass}.Member_${CtorDeco}(456))));
    dkx_releasenow();
    dkx_setuns4(this, 0, dkx_swaplink(this, dkx_getuns4(this, 0), dkx_getuns4(this, 4)));
    dkx_setuns4(this, 4, dkx_swaplink(this, dkx_getuns4(this, 4), 0));
    (void)dkx_release(mbr);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task ObjectReferencesWithProperties()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    class Member
    {
        private int _no;
        private string _name;
        private Member _joint;

        public Member(int no, string name) { _no = no; _name = name; }

        public Member Joint { get { return _joint; } set { _joint = value; } }
        public string Name { get { return _name; } }
    }

    static class UnitTest
    {
        public static void Run()
        {
            Member mbr = new Member(123, ""Jimbo"");
            mbr.Joint = new Member(456, ""Ned"");
            Console.WriteLine(mbr.Joint.Name);

            var jntMbr = mbr.Joint;
            Member jntMbr2 = mbr.Joint;
            jntMbr.Joint = jntMbr2;
            jntMbr.Joint = jntMbr2.Joint;
            Console.WriteLine(jntMbr.Name);
        }
    }
}
");
            await RunCompileAsync(app);
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
unsigned int GetJoint(unsigned int this)
{
    return dkx_addref(dkx_getuns4(this, 516));
}
void SetJoint(unsigned int this, unsigned int value)
{
    dkx_setuns4(this, 516, dkx_swaplink(this, dkx_getuns4(this, 516), value));
    (void)dkx_release(value);
}

char(255) GetName(unsigned int this)
{
    return dkx_getstr(this, 4);
}

unsigned int Member_${CtorDeco}(int no, char(255) name)
{
    unsigned int this;
    this = dkx_new(520);
    dkx_setint4(this, 0, no);
    dkx_setstr(this, 4, name);
    return this;
}
"
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int, DataType.String255 }))
);
        await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    unsigned int mbr;
    unsigned int jntMbr;
    unsigned int jntMbr2;

    mbr = ${MemberClass}.Member_${CtorDeco}(123, ""Jimbo"");
    ${MemberClass}.SetJoint(mbr, ${MemberClass}.Member_${CtorDeco}(456, ""Ned""));
    puts(${MemberClass}.GetName(dkx_releasedefer(${MemberClass}.GetJoint(mbr))));
    dkx_releasenow();

    jntMbr = ${MemberClass}.GetJoint(mbr);
    jntMbr2 = ${MemberClass}.GetJoint(mbr);
    ${MemberClass}.SetJoint(jntMbr, dkx_addref(jntMbr2));
    ${MemberClass}.SetJoint(jntMbr, ${MemberClass}.GetJoint(jntMbr2));
    puts(${MemberClass}.GetName(jntMbr));
    (void)dkx_release(mbr);
    (void)dkx_release(jntMbr);
    (void)dkx_release(jntMbr2);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(new DataType(BaseType.Class, new string[] { "Test.Member" }), new DataType[] { DataType.Int, DataType.String255 }))
);
        }

        [Test]
        public async Task ObjectReferencesWithMethods()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    class Member
    {
        private int _no;

        public Member(int no) { _no = no; }

        public static void ShowMember(Member member)
        {
        }

        public static Member CreateMember(int no)
        {
            return new Member(no);
        }
    }

    static class UnitTest
    {
        public static void Run()
        {
            var mbr = Member.CreateMember(123);
            Member.ShowMember(mbr);
            Member.ShowMember(Member.CreateMember(456));
            mbr = Member.CreateMember(789);
            Member.CreateMember(101112);
        }
    }
}
");
            await RunCompileAsync(app);
            var memberDataType = new DataType(BaseType.Class, new string[] { "Test.Member" });
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
unsigned int Member_${CtorDeco}(int no)
{
    unsigned int this;
    this = dkx_new(4);
    dkx_setint4(this, 0, no);
    return this;
}

void ShowMember_${ShowMemberDeco}(unsigned int member)
{
    (void)dkx_release(member);
}

unsigned int CreateMember_${CreateMemberDeco}(int no)
{
    return ${MemberClass}.Member_${CtorDeco}(no);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(memberDataType, new DataType[] { DataType.Int }))
.Replace("${ShowMemberDeco}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { memberDataType }))
.Replace("${CreateMemberDeco}", Compiler.GetMethodDecoration(memberDataType, new DataType[] { DataType.Int }))
);
            await ValidateOutputAsync(app, "Test.UnitTest", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.UnitTest")}.nc", @"
void Run_${RunDeco}()
{
    unsigned int mbr;
    mbr = ${MemberClass}.CreateMember_${CreateMemberDeco}(123);
    ${MemberClass}.ShowMember_${ShowMemberDeco}(dkx_addref(mbr));
    ${MemberClass}.ShowMember_${ShowMemberDeco}(${MemberClass}.CreateMember_${CreateMemberDeco}(456));
    mbr = dkx_swapnoadd(mbr, ${MemberClass}.CreateMember_${CreateMemberDeco}(789));
    (void)dkx_release(${MemberClass}.CreateMember_${CreateMemberDeco}(101112));
    (void)dkx_release(mbr);
}
"
.Replace("${MemberClass}", Compiler.GetWbdkClassName("Test.Member"))
.Replace("${RunDeco}", Compiler.GetMethodDecoration(DataType.Void, DataType.EmptyArray))
.Replace("${ShowMemberDeco}", Compiler.GetMethodDecoration(DataType.Void, new DataType[] { memberDataType }))
.Replace("${CreateMemberDeco}", Compiler.GetMethodDecoration(memberDataType, new DataType[] { DataType.Int }))
);
        }

        [Test]
        public async Task ReturnInsideConstructor()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    class Member
    {
        private int _no;
        private string _name;

        public Member(int no, string name)
        {
            _no = no;
            if (name == """")
            {
                _name = ""Unnamed"";
                return;
            }
            _name = name;
        }
    }
}
");
            await RunCompileAsync(app);
            var memberDataType = new DataType(BaseType.Class, new string[] { "Test.Member" });
            await ValidateOutputAsync(app, "Test.Member", $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
unsigned int Member_${CtorDeco}(int no, char(255) name)
{
    unsigned int this;
    this = dkx_new(516);
    dkx_setint4(this, 0, no);
    if name == """"
    {
        dkx_setstr(this, 4, ""Unnamed"");
        return this;
    }
    dkx_setstr(this, 4, name);
    return this;
}
"
.Replace("${CtorDeco}", Compiler.GetMethodDecoration(memberDataType, new DataType[] { DataType.Int, DataType.String255 }))
);
        }

        [Test]
        public async Task MethodNameSameAsClass()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    class Member
    {
        public int Member()
        {
            return 0;
        }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.MemberNameCannotBeSameAsClassName);
        }

        [Test]
        public async Task PropertyNameSameAsClass()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    class Member
    {
        public int Member { get { return 0; } }
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.MemberNameCannotBeSameAsClassName);
        }

        [Test]
        public async Task MemberVariableNameSameAsClass()
        {
            var app = CreateApp();
            SetupCompile(app);
            app.LoadAppSettings();

            await SetCompileFileAsync(app, @"x:\src\Test.dkx", @"
using System;

namespace Test
{
    class Member
    {
        private int Member;
    }
}
");
            await RunCompileAsync(app, expectedErrorCode: ErrorCode.MemberNameCannotBeSameAsClassName);
        }

        // TODO: Cannot access private class from another namespace
    }
}
