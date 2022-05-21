using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using NUnit.Framework;

namespace DKX.Compilation.Tests.Code
{
    [TestFixture]
    class CodeParsingTests
    {
        [Test]
        public void Class()
        {
            var cp = new DkxCodeParser("class TestClass { }");
            var root = cp.ReadAll();

            Assert.IsFalse(root.IsNone);
            Assert.IsTrue(root.IsGroup);
            Assert.AreEqual(3, root.Tokens.Count);

            var rootTokens = root.Tokens;

            Assert.AreEqual(DkxTokenType.Keyword, rootTokens[0].Type);
            Assert.AreEqual("class", rootTokens[0].Text);

            Assert.AreEqual(DkxTokenType.Identifier, rootTokens[1].Type);
            Assert.AreEqual("TestClass", rootTokens[1].Text);

            Assert.AreEqual(DkxTokenType.Scope, rootTokens[2].Type);
            Assert.AreEqual(0, rootTokens[2].Tokens.Count);
            Assert.IsTrue(rootTokens[2].Closed);
        }

        [Test]
        public void UnclosedClass()
        {
            var cp = new DkxCodeParser("class TestClass {");
            var root = cp.ReadAll();

            Assert.IsFalse(root.IsNone);
            Assert.IsTrue(root.IsGroup);
            Assert.AreEqual(3, root.Tokens.Count);

            var rootTokens = root.Tokens;

            Assert.AreEqual(DkxTokenType.Keyword, rootTokens[0].Type);
            Assert.AreEqual("class", rootTokens[0].Text);

            Assert.AreEqual(DkxTokenType.Identifier, rootTokens[1].Type);
            Assert.AreEqual("TestClass", rootTokens[1].Text);

            Assert.AreEqual(DkxTokenType.Scope, rootTokens[2].Type);
            Assert.AreEqual(0, rootTokens[2].Tokens.Count);
            Assert.IsFalse(rootTokens[2].Closed);
        }

        [Test]
        public void NamespaceAndClasses()
        {
            var cp = new DkxCodeParser(@"
namespace Util
{
    public class StringHelper
    {
        public string Trim(string str) { str[0] = """"; }
    }

    public class CharHelper
    {
        public char ToLower(char ch) { return ch >= 'A' && ch <= 'Z' ? ch + 32 : ch; }
        public char ToUpper(char ch) { return ch >= 'a' && ch <= 'z' ? ch - 32 : ch; }
    }
}
");
            var root = cp.ReadAll();

            Assert.IsFalse(root.IsNone);
            Assert.IsTrue(root.IsGroup);
            Assert.AreEqual(3, root.Tokens.Count);

            var v = new TokenValidator(root, 2);
            v.Keyword("namespace", 1);
            v.Identifier("Util", 2);
            v.Scope(6, n =>
            {
                // Util namespace
                n.Keyword("public", 1);
                n.Keyword("class", 1);
                n.Identifier("StringHelper", 6);
                n.Scope(10, c =>
                {
                    // StringHelper class
                    c.Keyword("public", 1);
                    c.DataType(DataType.String255, 6, 1);
                    c.Identifier("Trim", 0);
                    c.Arguments(0, a =>
                    {
                        a.DataType(DataType.String255, 6, 1);
                        a.Identifier("str", 0);
                    }, 1);
                    c.Scope(1, f =>
                    {
                        f.Identifier("str", 0);
                        f.Array(0, a =>
                        {
                            a.Number(0, DataType.Int, 1, 0);
                        }, 1);
                        f.Operator(Operator.Assign, 1);
                        f.String("", 2, false, 0);
                        f.StatementEnd(1);
                    }, 6);
                }, 8);
                n.Keyword("public", 1);
                n.Keyword("class", 1);
                n.Identifier("CharHelper", 6);
                n.Scope(10, c =>
                {
                    // CharHelper class

                    // CharHelper.ToLower()
                    c.Keyword("public", 1);
                    c.DataType(DataType.Char, 4, 1);
                    c.Identifier("ToLower", 0);
                    c.Arguments(0, a =>
                    {
                        a.DataType(DataType.Char, 4, 1);
                        a.Identifier("ch", 0);
                    }, 1);
                    c.Scope(1, f =>
                    {
                        f.Keyword("return", 1);
                        f.Identifier("ch", 1);
                        f.Operator(Operator.GreaterEqual, 1);
                        f.Char('A', 3, false, 1);
                        f.Operator(Operator.And, 1);
                        f.Identifier("ch", 1);
                        f.Operator(Operator.LessEqual, 1);
                        f.Char('Z', 3, false, 1);
                        f.Operator(Operator.Ternary1, 1);
                        f.Identifier("ch", 1);
                        f.Operator(Operator.Add, 1);
                        f.Number(32, DataType.Int, 2, 1);
                        f.Operator(Operator.Ternary2, 1);
                        f.Identifier("ch", 0);
                        f.StatementEnd(1);
                    }, 10);

                    // CharHelper.ToUpper()
                    c.Keyword("public", 1);
                    c.DataType(DataType.Char, 4, 1);
                    c.Identifier("ToUpper", 0);
                    c.Arguments(0, a =>
                    {
                        a.DataType(DataType.Char, 4, 1);
                        a.Identifier("ch", 0);
                    }, 1);
                    c.Scope(1, f =>
                    {
                        f.Keyword("return", 1);
                        f.Identifier("ch", 1);
                        f.Operator(Operator.GreaterEqual, 1);
                        f.Char('a', 3, false, 1);
                        f.Operator(Operator.And, 1);
                        f.Identifier("ch", 1);
                        f.Operator(Operator.LessEqual, 1);
                        f.Char('z', 3, false, 1);
                        f.Operator(Operator.Ternary1, 1);
                        f.Identifier("ch", 1);
                        f.Operator(Operator.Subtract, 1);
                        f.Number(32, DataType.Int, 2, 1);
                        f.Operator(Operator.Ternary2, 1);
                        f.Identifier("ch", 0);
                        f.StatementEnd(1);
                    }, 6);
                }, 2);
            }, 0);
        }

        [Test]
        public void VerbatimString()
        {
            var cp = new DkxCodeParser(@"
namespace Test
{
    public class TestClass
    {
        public void TestMethod()
        {
            var str = @""
This is a """"verbatim string""""
That wraps multiple lines.
Oh yeah.
\ / ' """" `
The end.
"";
        }
    }
}
");
            var root = new TokenValidator(cp.ReadAll(), 2);
            root.Keyword("namespace", 1);
            root.Identifier("Test", 2);
            root.Scope(6, ns =>
            {
                ns.Keyword("public", 1);
                ns.Keyword("class", 1);
                ns.Identifier("TestClass", 6);
                ns.Scope(10, cls =>
                {
                    cls.Keyword("public", 1);
                    cls.DataType(DataType.Void, 4, 1);
                    cls.Identifier("TestMethod", 0);
                    cls.Arguments(0, null, 10);
                    cls.Scope(14, meth =>
                    {
                        meth.Keyword("var", 1);
                        meth.Identifier("str", 1);
                        meth.Operator(Operator.Assign, 1);
                        meth.String("\r\nThis is a \"verbatim string\"\r\nThat wraps multiple lines.\r\nOh yeah.\r\n\\ / ' \" `\r\nThe end.\r\n", 96, false, 0);
                        meth.StatementEnd(10);
                    }, 6);
                }, 2);
            }, 2);
        }

        [TestCase("int", BaseType.Int, 0, 0)]
        [TestCase("uint", BaseType.UInt, 0, 0)]
        [TestCase("short", BaseType.Short, 0, 0)]
        [TestCase("ushort", BaseType.UShort, 0, 0)]
        [TestCase("numeric9", BaseType.Numeric, 9, 0)]
        [TestCase("numeric11.2", BaseType.Numeric, 11, 2)]
        [TestCase("unsigned5", BaseType.UNumeric, 5, 0)]
        [TestCase("unsigned19", BaseType.UNumeric, 19, 0)]
        [TestCase("unsigned13.2", BaseType.UNumeric, 13, 2)]
        public void DataTypes(string dataTypeText, BaseType baseType, byte width, byte scale)
        {
            var cp = new DkxCodeParser(dataTypeText);
            var tokens = cp.ReadAll().Tokens;

            Assert.AreEqual(1, tokens.Count);

            var token = tokens[0];
            Assert.AreEqual(baseType, token.DataType.BaseType);
            Assert.AreEqual(width, token.DataType.Width);
            Assert.AreEqual(scale, token.DataType.Scale);
        }
    }
}
