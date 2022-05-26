using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes
{
    class MethodScope : Scope, IReturnScope, IVariableScope, IMethod, IObjectReferenceScope
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

        private MethodScope(
            Scope parent,
            string className,
            string name,
            CodeSpan nameSpan,
            DataType returnDataType,
            VariableStore variableStore,
            Modifiers modifiers)
            : base(parent)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _returnDataType = returnDataType;
            _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));

            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _static = modifiers.Static;

            _wbdkName = string.Concat(_className, "_", _name);
        }

        public IEnumerable<Variable> Arguments => _variableStore.GetVariables(includeParents: false).Where(v => v.ArgumentType.HasValue);
        IArgument[] IMethod.Arguments => _variableStore.GetVariables(includeParents: false).Where(v => v.ArgumentType.HasValue).Cast<IArgument>().ToArray();
        public FileContext FileContext => _fileContext;
        public string Name => _name;
        public Privacy Privacy => _privacy;
        public DataType ReturnDataType => _returnDataType;
        public DataType ScopeDataType => Parent.GetScope<IObjectReferenceScope>().ScopeDataType;
        public bool ScopeStatic => _static;
        public Statement[] Statements { get => _statements ?? Statement.EmptyArray; private set => _statements = value; }
        public bool Static => _static;
        public IVariableStore VariableStore => _variableStore;

        public static async Task<MethodScope> ParseAsync(
            Scope parent,
            string className,
            string name,
            CodeSpan nameSpan,
            DataType returnDataType,
            DkxTokenCollection argumentTokens,
            Modifiers modifiers,
            DkxTokenCollection bodyTokens,
            IResolver resolver)
        {
            var variableStore = new VariableStore(parent.GetScope<IVariableScope>());
            var methodScope = new MethodScope(parent, className, name, nameSpan, returnDataType, variableStore, modifiers);

            variableStore.AddVariables(await methodScope.ProcessArgumentsAsync(argumentTokens));

            if (bodyTokens != null)
            {
                var methodResolver = new MethodResolver(methodScope, resolver);
                methodScope.Statements = (await StatementParser.SplitTokensIntoStatementsAsync(methodScope, bodyTokens, methodResolver)).ToArray();
            }

            return methodScope;
        }

        private async Task<IEnumerable<Variable>> ProcessArgumentsAsync(DkxTokenCollection tokens)
        {
            var unnamedIndex = 0;
            var reportedEmptyArgs = false;
            var args = new List<Variable>();

            if (tokens.Count > 0)
            {
                foreach (var argTokens in tokens.SplitByType(DkxTokenType.Delimiter))
                {
                    DataType dataType = DataType.Int;
                    string name = null;
                    ArgumentPassType passType = ArgumentPassType.ByValue;

                    if (tokens.Count == 0)
                    {
                        if (!reportedEmptyArgs) await ReportAsync(_nameSpan, ErrorCode.MethodContainsEmptyArguments);
                        reportedEmptyArgs = true;
                        name = string.Format(DkxConst.Variables.UnnamedArgumentFormat, ++unnamedIndex);
                    }

                    var pos = 0;
                    var len = argTokens.Count;
                    if (pos < len && argTokens[pos].Type == DkxTokenType.Keyword)
                    {
                        if (argTokens[pos].Text == DkxConst.Keywords.Ref) passType = ArgumentPassType.ByReference;
                        else if (argTokens[pos].Text == DkxConst.Keywords.Out) passType = ArgumentPassType.Out;
                        else await ReportAsync(argTokens[pos].Span, ErrorCode.SyntaxError);
                        pos++;
                    }

                    if (pos < len && argTokens[pos].Type == DkxTokenType.DataType)
                    {
                        dataType = argTokens[pos].DataType;
                        pos++;
                    }
                    else
                    {
                        await ReportAsync(argTokens.First().Span, ErrorCode.ExpectedDataType);
                    }

                    if (pos < len && argTokens[pos].Type == DkxTokenType.Identifier)
                    {
                        name = argTokens[pos].Text;
                        if (args.Any(x => x.Name == name))
                        {
                            await ReportAsync(argTokens[pos].Span, ErrorCode.DuplicateArgumentName);
                        }
                        pos++;
                    }
                    else
                    {
                        await ReportAsync(argTokens.First().Span, ErrorCode.ExpectedArgumentName);
                        name = string.Format(DkxConst.Variables.UnnamedArgumentFormat, ++unnamedIndex);
                    }

                    args.Add(new Variable(
                        name: name,
                        wbdkName: name,
                        dataType: dataType,
                        fileContext: FileContext.NeutralClass,
                        passType: passType,
                        static_: false,
                        local: true,
                        privacy: Privacy.Public,
                        initializer: null));
                }
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

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            cw.Write(_returnDataType.ToWbdkCode());
            cw.Write(' ');
            cw.Write(_wbdkName);
            cw.Write('(');
            var firstArg = true;
            if (!_static)
            {
                cw.Write("unsigned int this");
                firstArg = false;
            }
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
                // Variables
                foreach (var variable in _variableStore.GetVariables(includeParents: false).Where(v => v.ArgumentType == null))
                {
                    cw.Write(variable.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(variable.WbdkName);
                    cw.Write(';');
                    cw.WriteLine();
                }

                // Statements
                foreach (var statement in _statements ?? Statement.EmptyArray)
                {
                    await statement.GenerateWbdkCodeAsync(cw);
                }
            }
        }

        public Task<CodeFragment> ToWbdkCode_MethodCallAsync(CodeFragment parentFragment, IEnumerable<CodeFragment> arguments, CodeSpan span)
        {
            var sb = new StringBuilder();

            var cls = GetScope<ClassScope>();
            var nameParts = cls.FullClassNameParts.ToArray();
            if (nameParts.Length < 2) throw new InvalidOperationException("Class did not return enough name parts.");
            sb.Append(nameParts[0]);
            sb.Append('.');
            sb.Append(string.Join("_", nameParts.Skip(1)));
            sb.Append('(');

            var firstArg = true;
            if (!_static)
            {
                // First argument is the 'this' pointer.
                sb.Append(parentFragment);
                firstArg = false;
            }
            foreach (var arg in arguments)
            {
                if (firstArg) firstArg = false;
                else sb.Append(", ");
                sb.Append(arg);
            }

            sb.Append(')');

            return Task.FromResult(new CodeFragment(sb.ToString(), _returnDataType, OpPrec.None, span, readOnly: true));
        }
    }
}
