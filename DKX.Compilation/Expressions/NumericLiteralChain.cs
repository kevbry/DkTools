using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Scopes;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;

namespace DKX.Compilation.Expressions
{
    class NumericLiteralChain : Chain
    {
        private decimal _value;
        private DataType _dataType;

        public NumericLiteralChain(decimal value, DataType dataType, Span span)
            : base(span)
        {
            _value = value;
            _dataType = dataType;
        }

        public override DataType DataType => _dataType;
        public override bool IsEmptyCode => false;

        public override DataType InferredDataType
        {
            get
            {
                if (_dataType.Scale == 0 && _dataType.Width <= DkxConst.Numeric.MaxInt4Digits) return DataType.Int;
                return _dataType;
            }
        }

        public static decimal NumberTextToValue(string text, out DataType dataTypeOut)
        {
            var signed = false;
            var gotDot = false;
            byte width = 0;
            byte scale = 0;

            foreach (var ch in text ?? throw new ArgumentNullException(nameof(text)))
            {
                if (signed == false && ch == '-') signed = true;
                else if (gotDot == false && ch == '.') gotDot = true;
                else if (ch >= '0' && ch <= '9')
                {
                    width++;
                    if (gotDot) scale++;
                }
                else
                {
                    throw new ArgumentException($"'{text}' is not a valid number.");
                }
            }

            if (width == 0) throw new ArgumentException($"'{text}' is not a valid number.");

            dataTypeOut = new DataType(signed ? BaseType.Numeric : BaseType.UNumeric, width: width, scale: scale);
            return decimal.Parse(text);
        }

        public override CodeFragment ToWbdkCode_Read(CodeGenerationContext context, FlowTrace flow)
        {
            return new CodeFragment(_value.ToString(), _dataType, OpPrec.None, Span, reportable: true);
        }

        public override CodeFragment ToWbdkCode_Write(CodeGenerationContext context, CodeFragment valueFragment, FlowTrace flow)
        {
            throw new CodeException(Span, ErrorCode.ExpressionCannotBeWrittenTo);
        }

        public override ConstTerm ToConstTermOrNull(IReportItemCollector report)
        {
            return new ConstValueTerm(new NumberConstValue(_value, _dataType, Span), Span);
        }
    }
}
