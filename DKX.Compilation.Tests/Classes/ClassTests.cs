using DK.Code;
using DKX.Compilation.ObjectFiles;
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
            var dkxCode = @"
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
            //Member.SetData(123, ""John"");
        }
    }
}
";
            var wbdkCode = @"
char(255) Member__inst;

int Member_GetNumber(unsigned int this) { return dkx_getint4(this, 0); }
char(255) Member_GetName(unsigned int this) { return dkx_getstr(this, 4); }
void Member_SetName(unsigned int this, char(255) value) { dkx_setstr(this, 4, value); }
char(255) Member_GetInst() { return Member__inst; }
void Member_SetInst(char(255) value) { Member__inst = value; }

void Member_SetData(unsigned int this, int no, char(255) name)
{
    dkx_setint4(this, 0, no);
    dkx_setstr(this, 4, name);
}

void Member_SetInstitution(char(255) inst)
{
    Member__inst = inst;
}

void User_DoTest()
{
    int no;
    unsigned int mbr;
    mbr = dkx_new(516);
    no = Test.Member_GetNumber(mbr);
}
";
            var objectModel = new ObjectFileModel
            {
                FileContexts = new ObjectFileContext[]
                {
                    new ObjectFileContext { Context = FileContext.NeutralClass }
                }
            };

            await SetupCompileSingle(@"x:\src\Test.dkx", @"x:\gen\.dkx\Test.nc", @"x:\bin\.dkx\Test.dkxx", dkxCode, wbdkCode, objectModel);
        }
    }
}
