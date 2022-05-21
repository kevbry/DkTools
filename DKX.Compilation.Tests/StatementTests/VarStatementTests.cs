using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests.StatementTests
{
    [TestFixture]
    class VarStatementTests : CompileTestClass
    {
        [Test]
        public async Task Int()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\bin\.dkx\test.dkxx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        var x = 0;
    }
}
", new ObjectFileModel
            {
                SourcePathName = @"x:\src\test.dkx",
                ClassName = "Test",
                Methods = new ObjectMethod[]
                {
                    new ObjectMethod
                    {
                        Name = "TestMethod",
                        Privacy = Privacy.Public,
                        FileContext = DK.Code.FileContext.NeutralClass,
                        ReturnDataType = "void",
                        Body = new ObjectBody
                        {
                            StartPosition = 45,
                            Variables = new ObjectVariable[]
                            {
                                new ObjectVariable { Name = "x", DataType = "int" }
                            },
                            Code = "mov($x,0)"
                        }
                    }
                }
            }, @"
void TestMethod()
{
    int x;
    x = 0;
}
");
        }

        [Test]
        public async Task NoInitializer()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\bin\.dkx\test.dkxx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        var x;
    }
}
", null, null, ReportItem.FromOneBased(@"x:\src\test.dkx", 6, 13, 6, 14, ErrorCode.VariableInitializationRequired));
        }

        [Test]
        public async Task MultipleVariablesInOneStatement()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\bin\.dkx\test.dkxx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        var x = 0, y = 0;
    }
}
", null, null, ReportItem.FromOneBased(@"x:\src\test.dkx", 6, 18, ErrorCode.ExpectedToken, ';'));
        }

        [Test]
        public async Task VarNotAtTop()
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\bin\.dkx\test.dkxx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        var x = 0;
        x++;
        var y = x;
    }
}
", new ObjectFileModel
            {
                SourcePathName = @"x:\src\test.dkx",
                ClassName = "Test",
                Methods = new ObjectMethod[]
                {
                    new ObjectMethod
                    {
                        Name = "TestMethod",
                        Privacy = Privacy.Public,
                        FileContext = DK.Code.FileContext.NeutralClass,
                        ReturnDataType = "void",
                        Body = new ObjectBody
                        {
                            StartPosition = 45,
                            Variables = new ObjectVariable[]
                            {
                                new ObjectVariable { Name = "x", DataType = "int" },
                                new ObjectVariable { Name = "y", DataType = "int" }
                            },
                            Code = "mov($x,0),inc($x),mov($y,$x)"
                        }
                    }
                }
            }, @"
void TestMethod()
{
    int x;
    int y;
    x = 0;
    x += 1;
    y = x;
}
");
        }

        // TODO: vars in different scopes with the same name
        // TODO: duplicate name (neg)
        // TODO: bad data type

        // Numeric
        [TestCase("0", "int", "0", "int", "0")]
        [TestCase("-1", "int", "-1", "int", "-1")]
        [TestCase("999999999", "int", "999999999", "int", "999999999")]
        [TestCase("-999999999", "int", "-999999999", "int", "-999999999")]
        [TestCase("1234567890", "unsigned(10)", "1234567890", "numeric(10) unsigned", "1234567890")]
        [TestCase("-1234567890", "numeric(10)", "-1234567890", "numeric(10)", "-1234567890")]
        [TestCase("123.45", "unsigned(5, 2)", "123.45", "numeric(5, 2) unsigned", "123.45")]
        [TestCase("-123.45", "numeric(5, 2)", "-123.45", "numeric(5, 2)", "-123.45")]
        // Strings
        [TestCase("\"Hello\"", "string", "\"Hello\"", "char(255)", "\"Hello\"")]
        // Chars
        [TestCase("' '", "char", "' '", "char", "' '")]
        [TestCase("'w'", "char", "'w'", "char", "'w'")]
        [TestCase("'\\''", "char", "'\\''", "char", "'\\''")]
        [TestCase("'\\t'", "char", "'\\t'", "char", "'\\t'")]
        [TestCase("'\\r'", "char", "'\\r'", "char", "'\\r'")]
        [TestCase("'\\n'", "char", "'\\n'", "char", "'\\n'")]
        [TestCase("'\\w'", "char", "'w'", "char", "'w'")]
        [TestCase("'\\\\'", "char", "'\\\\'", "char", "'\\\\'")]
        [TestCase("'\\\''", "char", "'\\\''", "char", "'\\\''")]
        [TestCase("'\\\"'", "char", "'\\\"'", "char", "'\\\"'")]
        //// Dates
        //[TestCase("%0", "date", "%0", "date", "\"01Jan1900\"")]
        //[TestCase("%01Jan1900", "date", "%0", "date", "\"01Jan1900\"")]
        //[TestCase("%20May2022", "date", "%0", "date", "\"20May2022\"")]
        //[TestCase("%20220520", "date", "%0", "date", "\"20May2022\"")]
        //// Times
        //[TestCase(":0", "time", ":0", "time", "\"00:00:00\"")]
        //[TestCase(":000000", "time", ":0", "time", "\"00:00:00\"")]
        //[TestCase(":43000", "time", ":43000", "time", "\"04:30:00\"")]
        //[TestCase(":043000", "time", ":43000", "time", "\"04:30:00\"")]
        //[TestCase(":120000", "time", ":120000", "time", "\"21:00:00\"")]
        //[TestCase(":235958", "time", ":235958", "time", "\"23:59:58\"")]
        //[TestCase(":235959", "time", ":235959", "time", "\"23:59:58\"")]
        public async Task Literals(string literal, string expectedType, string opCodeLiteral, string wbdkType, string wbdkLiteral)
        {
            await SetupCompileSingle(@"x:\src\test.dkx", @"x:\bin\.dkx\test.dkxx", @"x:\src\__test.nc", @"
class Test
{
    void TestMethod()
    {
        var x = %1;
    }
}
".Replace("%1", literal), new ObjectFileModel
            {
                SourcePathName = @"x:\src\test.dkx",
                ClassName = "Test",
                Methods = new ObjectMethod[]
                {
                    new ObjectMethod
                    {
                        Name = "TestMethod",
                        Privacy = Privacy.Public,
                        FileContext = DK.Code.FileContext.NeutralClass,
                        ReturnDataType = "void",
                        Body = new ObjectBody
                        {
                            StartPosition = 45,
                            Variables = new ObjectVariable[]
                            {
                                new ObjectVariable { Name = "x", DataType = expectedType }
                            },
                            Code = "mov($x,%1)".Replace("%1", opCodeLiteral)
                        }
                    }
                }
            }, @"
void TestMethod()
{
    %2 x;
    x = %1;
}
".Replace("%1", wbdkLiteral).Replace("%2", wbdkType));
        }
    }
}
