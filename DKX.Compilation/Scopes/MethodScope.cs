using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Scopes
{
    public class MethodScope : Scope, IReturnScope, IVariableScope
    {
        private string _className;
        private string _name;
        private CodeSpan _nameSpan;
        private string _wbdkName;
        private DataType _returnDataType;
        private VariableStore _variableStore;
        private FileContext _fileContext;
        private Privacy _privacy;
        private bool _static;
        private string _signature;  // Initially null; lazy generated
        private Statement[] _statements;

        public MethodScope(Scope parent, string className, string name, CodeSpan nameSpan, DataType returnDataType, DkxTokenCollection argumentTokens, Modifiers modifiers, DkxTokenCollection bodyTokens)
            : base(parent)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _returnDataType = returnDataType;
            _variableStore = new VariableStore(parent.GetScope<IVariableScope>());
            _variableStore.AddVariables(ProcessArguments(argumentTokens).ToList());

            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _static = modifiers.Static;

            _wbdkName = string.Concat(_className, "_", _name);

            if (bodyTokens != null)
            {
                _statements = StatementParser.SplitTokensIntoStatements(this, bodyTokens);
            }
        }

        public IEnumerable<Variable> Arguments => _variableStore.GetVariables(includeParents: false).Where(v => v.ArgumentType.HasValue);
        public FileContext FileContext => _fileContext;
        public DataType ReturnDataType => _returnDataType;
        public IVariableStore VariableStore => _variableStore;

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
                    sb.Append(_privacy.ToKeyword());
                    sb.Append(' ');
                    if (_fileContext.IsClientSide())
                    {
                        sb.Append(DkxConst.Keywords.Client);
                        sb.Append(' ');
                    }
                    else if (_fileContext.IsServerSide())
                    {
                        sb.Append(DkxConst.Keywords.Server);
                        sb.Append(' ');
                    }
                    if (_static)
                    {
                        sb.Append(DkxConst.Keywords.Static);
                        sb.Append(' ');
                    }
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

        internal override void GenerateWbdkCode(CodeWriter cw)
        {
            cw.Write(_returnDataType.ToWbdkCode());
            cw.Write(' ');
            cw.Write(_wbdkName);
            cw.Write('(');
            var firstArg = true;
            foreach (var arg in Arguments)
            {
                if (firstArg) firstArg = false;
                else cw.Write(", ");
                cw.Write(arg.DataType.ToWbdkCode());
                cw.Write(' ');
                if (arg.ArgumentType == ArgumentPassType.ByReference || arg.ArgumentType == ArgumentPassType.Out) cw.Write('&');
                cw.Write(arg.WbdkName);
            }
            cw.Write(')');
            cw.WriteLine();
            using (cw.Indent())
            {
                foreach (var statement in _statements ?? Statement.EmptyArray)
                {
                    statement.GenerateWbdkCode(cw);
                }
            }
        }
    }
}
