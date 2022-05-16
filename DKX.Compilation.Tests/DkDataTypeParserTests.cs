using DK.Code;
using DKX.Compilation.DataTypes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    [TestFixture]
    class DkDataTypeParserTests
    {
        [TestCase("void", BaseType.Void, 0, 0)]
        [TestCase("Boolean_t", BaseType.Bool, 0, 0)]
        [TestCase("short", BaseType.Short, 0, 0)]
        [TestCase("unsigned short", BaseType.UShort, 0, 0)]
        [TestCase("short unsigned", BaseType.UShort, 0, 0)]
        [TestCase("int", BaseType.Int, 0, 0)]
        [TestCase("unsigned int", BaseType.UInt, 0, 0)]
        [TestCase("int unsigned", BaseType.UInt, 0, 0)]
        [TestCase("numeric(9)", BaseType.Numeric, 9, 0)]
        [TestCase("numeric(9) \"999-999-999\"", BaseType.Numeric, 9, 0)]
        [TestCase("numeric(9) unsigned", BaseType.UNumeric, 9, 0)]
        [TestCase("numeric(9) unsigned \"999-999-999\"", BaseType.UNumeric, 9, 0)]
        [TestCase("numeric(11, 2)", BaseType.Numeric, 11, 2)]
        [TestCase("numeric(11, 2) unsigned", BaseType.UNumeric, 11, 2)]
        [TestCase("numeric(11, 2) currency", BaseType.Numeric, 11, 2)]
        [TestCase("numeric(11, 2) unsigned currency", BaseType.UNumeric, 11, 2)]
        [TestCase("numeric(11, 2) currency unsigned", BaseType.UNumeric, 11, 2)]
        [TestCase("date", BaseType.Date, 0, 0)]
        [TestCase("date \"ddMMMyyyy\"", BaseType.Date, 0, 0)]
        [TestCase("time", BaseType.Time, 0, 0)]
        [TestCase("time 8", BaseType.Time, 0, 0)]
        [TestCase("table", BaseType.Table, 0, 0)]
        [TestCase("indrel", BaseType.Indrel, 0, 0)]
        [TestCase("variant", BaseType.Variant, 0, 0)]
        [TestCase("command", BaseType.Unsupported, 0, 0)]
        [TestCase("Section Level 5", BaseType.Unsupported, 0, 0)]
        [TestCase("scroll", BaseType.Unsupported, 0, 0)]
        [TestCase("graphic 100 100 100", BaseType.Unsupported, 0, 0)]
        [TestCase("interface NETString", BaseType.Unsupported, 0, 0)]
        [TestCase("oleobject", BaseType.Unsupported, 0, 0)]
        [TestCase("string 255", BaseType.String, 255, 0)]
        public void CodeParsing(string codeFragment, BaseType baseType, short width, short scale)
        {
            var code = new CodeParser(codeFragment);
            var dataType = DkDataTypeParser.Parse(code);
            Assert.IsNotNull(dataType);
            Assert.AreEqual(baseType, dataType.Value.BaseType);
            Assert.AreEqual(width, dataType.Value.Width);
            Assert.AreEqual(scale, dataType.Value.Scale);

            code.SkipWhiteSpace();
            Assert.IsTrue(code.EndOfFile);
        }

        [TestCase("enum { no, yes }", "no|yes")]
        [TestCase("enum { yes, no }", "yes|no")]
        [TestCase("enum { \" \", no, yes }", " |no|yes")]
        [TestCase("enum { \" \", \"no\", \"yes\" }", " |no|yes")]
        [TestCase("enum { \" \", bdoon, \"107ave\", wstgate }", " |bdoon|107ave|wstgate")]
        public void Enums(string codeFragment, string expectedOptionsString)
        {
            var code = new CodeParser(codeFragment);
            var dataType = DkDataTypeParser.Parse(code);
            Assert.IsNotNull(dataType);
            Assert.AreEqual(BaseType.Enum, dataType.Value.BaseType);
            Assert.AreEqual(0, dataType.Value.Width);
            Assert.AreEqual(0, dataType.Value.Scale);

            var expectedOptions = expectedOptionsString.Split('|');

            var options = dataType.Value.Options;
            Assert.IsNotNull(options);
            Assert.AreEqual(expectedOptions.Length, options.Length);
            Assert.AreEqual(expectedOptions, options);
        }
    }
}
