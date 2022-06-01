using DK.Code;
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
            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
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
            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.User")}.nc", @"
// Test.User

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
            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.Member")}.nc", @"
// Test.Member

char(255) _inst;

void SetInstitution_%1(char(255) inst)
{
    _inst = inst;
}
".Replace("%1", Compiler.ComputeHash("void, string").ToString("X"))
);
            await ValidateOutput(app, $"x:\\gen\\.dkx\\{Compiler.GetWbdkClassName("Test.User")}.nc", @"
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

        // TODO: using the 'this' keyword to get at a member variable or method
        // TODO: Can't call static method using an object reference.
        // TODO: Can't call non-static members with a static class name.
        // TODO: A property that is named the same as its data type should still work fine.
    }
}
