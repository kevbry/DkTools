using DK.Code;
using System;

namespace DKX.Compilation.CodeGeneration.Constants
{
    class StringConstantValue : ConstantValue
    {
        private string _rawText;

        public StringConstantValue(string rawText)
        {
            _rawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
        }

        public override string ToCode() => CodeParser.StringToStringLiteral(_rawText);
    }
}
