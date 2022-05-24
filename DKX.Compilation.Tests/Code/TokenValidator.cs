using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using NUnit.Framework;
using System;

namespace DKX.Compilation.Tests.Code
{
    class TokenValidator
    {
        private DkxTokenCollection _tokens;
        private int _index;
        private int _pos;

        public TokenValidator(DkxToken groupToken, int startPosition)
        {
            _tokens = groupToken.Tokens;
            _index = 0;
            _pos = startPosition;
        }

        private DkxToken GetToken()
        {
            Assert.IsTrue(_index < _tokens.Count);
            return _tokens[_index++];
        }

        private void CheckSpan(DkxToken token, int spanLength, int spacer)
        {
            Assert.AreEqual(new CodeSpan(_pos, _pos + spanLength), token.Span);
            _pos += spanLength + spacer;
        }

        private void CheckSpan(DkxToken token, int startPos, int endPos, int spacer)
        {
            Assert.AreEqual(new CodeSpan(startPos, endPos), token.Span);
            _pos = endPos + spacer;
        }

        public void Keyword(string keyword, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.Keyword, token.Type);
            Assert.AreEqual(keyword, token.Text);
            CheckSpan(token, keyword.Length, spacer);
        }

        public void DataType(DataType dataType, int length, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.DataType, token.Type);
            Assert.AreEqual(dataType, token.DataType);
            CheckSpan(token, length, spacer);
        }

        public void Identifier(string name, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.Identifier, token.Type);
            Assert.AreEqual(name, token.Text);
            CheckSpan(token, name.Length, spacer);
        }

        public void Operator(Operator op, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.Operator, token.Type);
            Assert.AreEqual(op, token.Operator);
            CheckSpan(token, op.GetText().Length, spacer);
        }

        public void String(string rawText, int width, bool hasError, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.String, token.Type);
            Assert.AreEqual(rawText, token.Text);
            Assert.AreEqual(hasError, token.HasError);
            Assert.AreEqual(DataTypes.DataType.String255, token.DataType);
            CheckSpan(token, width, spacer);
        }

        public void Char(char ch, int width, bool hasError, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.Char, token.Type);
            Assert.AreEqual(ch, token.Char);
            Assert.AreEqual(hasError, token.HasError);
            Assert.AreEqual(DataTypes.DataType.Char, token.DataType);
            CheckSpan(token, width, spacer);
        }

        public void Number(decimal value, DataType dataType, int width, int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.Number, token.Type);
            Assert.AreEqual(value, token.Number);
            Assert.AreEqual(dataType, token.DataType);
            CheckSpan(token, width, spacer);
        }

        public void StatementEnd(int spacer)
        {
            var token = GetToken();
            Assert.AreEqual(DkxTokenType.StatementEnd, token.Type);
            CheckSpan(token, 1, spacer);
        }

        private void Group(DkxTokenType type, int spacerIntoBody, Action<TokenValidator> callback, int spacerAfterBody)
        {
            var token = GetToken();
            Assert.AreEqual(type, token.Type);
            Assert.IsTrue(token.Closed);

            var startPos = _pos;
            _pos++; // For the opening token
            _pos += spacerIntoBody;

            var v = new TokenValidator(token, _pos);
            if (callback != null) callback(v);
            Assert.AreEqual(v._index, v._tokens.Count);

            _pos = v._pos + 1;  // +1 for the closing token
            CheckSpan(token, startPos, _pos, spacerAfterBody);
        }

        public void Brackets(int spacerIntoBody, Action<TokenValidator> callback, int spacerAfterBody) =>
            Group(DkxTokenType.Brackets, spacerIntoBody, callback, spacerAfterBody);

        public void Array(int spacerIntoBody, Action<TokenValidator> callback, int spacerAfterBody) =>
            Group(DkxTokenType.Array, spacerIntoBody, callback, spacerAfterBody);

        public void Scope(int spacerIntoBody, Action<TokenValidator> callback, int spacerAfterBody) =>
            Group(DkxTokenType.Scope, spacerIntoBody, callback, spacerAfterBody);
    }
}
