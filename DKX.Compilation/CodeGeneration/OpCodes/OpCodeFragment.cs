using DK;
using DK.Code;
using DKX.Compilation.DataTypes;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.CodeGeneration.OpCodes
{
    class OpCodeFragment
    {
        private DataType? _dataType;
        private string _varName;
        private List<string> _opCodes;
        private List<string> _registersUsed;

        public static string NewLine = "\n";

        public OpCodeFragment(DataType? dataType, string varName)
        {
            _dataType = dataType;
            _varName = varName ?? throw new ArgumentNullException(nameof(varName));
        }

        public OpCodeFragment(DataType? dataType, string varName, string fragmentCode)
        {
            _dataType = dataType;
            _varName = varName ?? throw new ArgumentNullException(nameof(varName));

            _opCodes = new List<string>();
            _opCodes.Add(fragmentCode ?? throw new ArgumentNullException(nameof(fragmentCode)));
        }

        public OpCodeFragment(DataType? dataType, string varName, IEnumerable<string> fragments)
        {
            _dataType = dataType;
            _varName = varName ?? throw new ArgumentNullException(nameof(varName));

            _opCodes = new List<string>();
            _opCodes.AddRange(fragments ?? throw new ArgumentNullException(nameof(fragments)));
        }

        public OpCodeFragment(DataType? dataType, string varName, OpCodeFragment fragment)
        {
            _dataType = dataType;
            _varName = varName ?? throw new ArgumentNullException(nameof(varName));

            _opCodes = new List<string>();
            _opCodes.AddRange(fragment._opCodes);
        }

        public DataType? DataType => _dataType;
        public bool IsEmpty => (_opCodes?.Count ?? 0) == 0;
        public IEnumerable<string> OpCodes => (IEnumerable<string>)_opCodes ?? StringHelper.EmptyStringArray;
        public string VarName => _varName;

        public override string ToString() => string.Join("\n", (IEnumerable<string>)_opCodes ?? StringHelper.EmptyStringArray);

        public OpCodeFragment Append(string op)
        {
            if (_opCodes == null) _opCodes = new List<string>();
            _opCodes.Add(op ?? throw new ArgumentNullException(nameof(op)));
            return this;
        }

        public OpCodeFragment Append(OpCodeFragment frag)
        {
            if (_opCodes == null || _opCodes.Count == 0) return frag;

            if (frag._opCodes == null)
            {
                frag._opCodes = new List<string>();
                frag._opCodes.AddRange(_opCodes);
            }
            else
            {
                frag._opCodes.InsertRange(0, _opCodes);
            }

            return frag;
        }

        public OpCodeFragment UsedRegister(string regName)
        {
            if (_registersUsed == null) _registersUsed = new List<string>();
            _registersUsed.Add(regName);
            return this;
        }

        private static string Span(CodeSpan span) => $"[{span.Start}:{span.Length}]";

        public static OpCodeFragment Return(CodeSpan span) => new OpCodeFragment(DataTypes.DataType.Void, string.Empty, $"{Span(span)} ret");
        public static OpCodeFragment ReturnValue(CodeSpan span, DataType? dataType, string varName) => new OpCodeFragment(dataType, varName, $"{Span(span)} retv {varName}");

        public static OpCodeFragment SetVarToVar(CodeSpan span, DataType? dataType, string dstVarName, string srcVarName) => new OpCodeFragment(dataType, dstVarName, $"{Span(span)} setv {dstVarName} {srcVarName}");
        public static OpCodeFragment SetVarToIdentifier(CodeSpan span, DataType? dataType, string dstVarName, string identName) => new OpCodeFragment(dataType, dstVarName, $"{Span(span)} seti {dstVarName} {identName}");
        public static OpCodeFragment SetVarToNumber(CodeSpan span, DataType? dataType, string dstVarName, string numberText) => new OpCodeFragment(dataType, dstVarName, $"{Span(span)} setn {dstVarName} {numberText}");
        public static OpCodeFragment SetVarToString(CodeSpan span, DataType? dataType, string dstVarName, string rawText) => new OpCodeFragment(dataType, dstVarName, $"{Span(span)} sets {dstVarName} {CodeParser.StringToStringLiteral(rawText)}");

        public static OpCodeFragment Increment(CodeSpan span, DataType? dataType, string varName) => new OpCodeFragment(dataType, varName, $"{Span(span)} inc {varName}");
        public static OpCodeFragment Decrement(CodeSpan span, DataType? dataType, string varName) => new OpCodeFragment(dataType, varName, $"{Span(span)} dec {varName}");
    }
}
