using DK.Code;
using DKX.Compilation.ObjectFiles;
using DKX.Compilation.Scopes;
using DKX.Compilation.Tests.Files;
using DKX.Compilation.Tokens;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.Scopes
{
    [TestFixture]
    class ScopesTests : CompileTestClass
    {
        [Test]
        public async Task MemberClass()
        {
            var dkxCode = @"
namespace Test
{
    class Member
    {
        int _no;
        string _name;

        public int Number { get { return _no; } }
        public string Name { get { return _name; } set { _name = value; } }

        public void SetData(int no, string name)
        {
            _no = no;
            _name = name;
        }
    }
}
";
            var wbdkCode = @"
int Member__no;
char(255) Member__name;

int Member_GetNumber() { return Member__no; }
char(255) Member_GetName() { return Member__name; }
void Member_SetName(char(255) value) { Member__name = value; }

void Member_SetData(int no, char(255) name)
{
    Member__no = no;
    Member__name = name;
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
