using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Nodes;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Scopes
{
    public class MethodScope : Scope
    {
        private string _name;
        private CodeSpan _nameSpan;
        private DataType _returnDataType;
        private Modifiers _modifiers;
        private List<Variable> _variables;
        private DkxTokenCollection _bodyTokens;

        public MethodScope(Scope parent, string name, CodeSpan nameSpan, DataType returnDataType, DkxTokenCollection argumentTokens, Modifiers modifiers, DkxTokenCollection bodyTokens)
            : base(parent)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _returnDataType = returnDataType;
            _modifiers = modifiers;
            _bodyTokens = bodyTokens;
            _variables = ProcessArguments(argumentTokens).ToList();
        }

        public IEnumerable<Variable> Arguments => _variables.Where(v => v.ArgumentType.HasValue);

        private IEnumerable<Variable> ProcessArguments(DkxTokenCollection tokens)
        {
            var unnamedIndex = 0;
            var reportedEmptyArgs = false;
            var args = new List<Variable>();

            foreach (var argTokens in tokens.SplitByType(DkxTokenType.Delimiter))
            {
                DataType dataType = DataType.Int;
                string name = null;
                ArgumentPassType passType = ArgumentPassType.ByValue;

                if (tokens.Count == 0)
                {
                    if (!reportedEmptyArgs) ReportItem(_nameSpan, ErrorCode.MethodContainsEmptyArguments);
                    reportedEmptyArgs = true;
                    name = string.Format(DkxConst.Variables.UnnamedArgumentFormat, ++unnamedIndex);
                }

                var pos = 0;
                var len = argTokens.Count;
                if (pos < len && argTokens[pos].Type == DkxTokenType.Keyword)
                {
                    if (argTokens[pos].Text == DkxConst.Keywords.Ref) passType = ArgumentPassType.ByReference;
                    else if (argTokens[pos].Text == DkxConst.Keywords.Out) passType = ArgumentPassType.Out;
                    else ReportItem(argTokens[pos].Span, ErrorCode.SyntaxError);
                    pos++;
                }

                if (pos < len && argTokens[pos].Type == DkxTokenType.DataType)
                {
                    dataType = argTokens[pos].DataType;
                    pos++;
                }
                else
                {
                    ReportItem(argTokens.First().Span, ErrorCode.ExpectedArgumentDataType);
                }

                if (pos < len && argTokens[pos].Type == DkxTokenType.Identifier)
                {
                    name = argTokens[pos].Text;
                    if (args.Any(x => x.Name == name))
                    {
                        ReportItem(argTokens[pos].Span, ErrorCode.DuplicateArgumentName);
                    }
                    pos++;
                }
                else
                {
                    ReportItem(argTokens.First().Span, ErrorCode.ExpectedArgumentName);
                    name = string.Format(DkxConst.Variables.UnnamedArgumentFormat, ++unnamedIndex);
                }

                args.Add(new Variable(
                    name: name,
                    wbdkName: name,
                    dataType: dataType,
                    fileContext: FileContext.NeutralClass,
                    passType: passType,
                    initializer: null));
            }

            return args;
        }

        public string Signature
        {
            get
            {
                if (_signature == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(_modifiers.ToSignature());

                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(_returnDataType.ToCode());
                    sb.Append(' ');
                    sb.Append(_name);
                    sb.Append('(');
                    foreach (var arg in Arguments)
                    {
                        if (arg.ArgumentType == ArgumentPassType.ByReference)
                        {
                            sb.Append(DkxConst.Keywords.Ref);
                            sb.Append(' ');
                        }
                        else if (arg.ArgumentType == ArgumentPassType.Out)
                        {
                            sb.Append(DkxConst.Keywords.Out);
                            sb.Append(' ');
                        }
                        sb.Append(arg.DataType.ToCode());
                        sb.Append(' ');
                        sb.Append(arg.Name);
                    }
                    sb.Append(')');
                    _signature = sb.ToString();
                }
                return _signature;
            }
        }
        private string _signature;

    }
}
