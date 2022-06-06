using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Jobs;
using DKX.Compilation.Objects;
using DKX.Compilation.Project;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DKX.Compilation.Scopes
{
    class MethodScope : Scope, IReturnScope, IVariableScope, IVariableWbdkScope, IMethod, IObjectReferenceScope
    {
        private string _className;
        private string _name;
        private Span _nameSpan;
        private string _wbdkName;
        private DataType _returnDataType;
        private VariableStore _variableStore;
        private List<Variable> _wbdkVariables = new List<Variable>();
        private FileContext _fileContext;
        private Privacy _privacy;
        private ModifierFlags _flags;
        private string _signature;  // Initially null; lazy generated
        private Statement[] _statements;
        private FileTarget _fileTarget;

        private MethodScope(
            Scope parent,
            string className,
            string name,
            Span nameSpan,
            DataType returnDataType,
            Modifiers modifiers,
            CompilePhase phase,
            IResolver resolver,
            IProject project)
            : base(parent, phase, resolver, project)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _returnDataType = returnDataType;

            _variableStore = new VariableStore(parent.GetScope<IVariableScope>());
            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _flags = modifiers.Flags;

            var fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
            _fileTarget = new FileTarget(fileContext, parent.GetScope<ClassScope>().WbdkClassName + FileContextHelper.GetExtension(fileContext));

            Resolver = new MethodResolver(this, resolver);
        }

        public MethodAccessType AccessType => MethodAccessType.Normal;
        public IEnumerable<Variable> Arguments => _variableStore.GetVariables(includeParents: false).Where(v => v.PassType.HasValue);
        IArgument[] IMethod.Arguments => _variableStore.GetVariables(includeParents: false).Where(v => v.PassType.HasValue).Cast<IArgument>().ToArray();
        ClassScope Class => GetScope<ClassScope>();
        IClass IMethod.Class => GetScope<IClass>();
        public Span DefinitionSpan => _nameSpan;
        public FileContext FileContext { get => _fileContext; set => _fileContext = value; }
        public FileTarget FileTarget { get => _fileTarget; set => _fileTarget = value; }
        public ModifierFlags Flags { get => _flags; set => _flags = value; }
        public bool IsConstructor => _flags.IsConstructor();
        public string Name => _name;
        public Span NameSpan => _nameSpan;
        public Privacy Privacy => _privacy;
        public DataType ReturnDataType => _returnDataType;
        public DataType ScopeDataType => Parent.GetScope<IObjectReferenceScope>().ScopeDataType;
        public bool ScopeStatic => _flags.HasFlag(ModifierFlags.Static);
        public Statement[] Statements { get => _statements ?? Statement.EmptyArray; private set => _statements = value; }
        public IVariableStore VariableStore => _variableStore;
        public string WbdkName { get => _wbdkName; set => _wbdkName = value ?? throw new ArgumentNullException(); }

        public override string ToString() => $"MethodScope: {_returnDataType} {_name}({string.Join(", ", Arguments.Select(a => $"{a.DataType} {a.Name}"))})";

        public static MethodScope Parse(
            Scope parent,
            string className,
            string name,
            Span nameSpan,
            DataType returnDataType,
            DkxTokenCollection argumentTokens,
            Modifiers modifiers,
            DkxTokenCollection bodyTokens,
            CompilePhase phase,
            IResolver resolver,
            IProject project)
        {
            var methodScope = new MethodScope(parent, className, name, nameSpan, returnDataType, modifiers, phase, resolver, project);

            var args = methodScope.ProcessArguments(argumentTokens).ToList();
            methodScope._variableStore.AddVariables(args);
            methodScope._wbdkName = string.Concat(methodScope._name, "_", Compiler.GetMethodDecoration(returnDataType, args.Select(x => x.DataType)));

            if (bodyTokens != null)
            {
                methodScope.Statements = StatementParser.SplitTokensIntoStatements(methodScope, bodyTokens);
            }

            if (phase >= CompilePhase.MemberScan) modifiers.CheckForMethod(methodScope, methodScope.GetScope<ClassScope>(), methodScope, phase);

            return methodScope;
        }

        private IEnumerable<Variable> ProcessArguments(DkxTokenCollection tokens)
        {
            var unnamedIndex = 0;
            var reportedEmptyArgs = false;
            var args = new List<Variable>();

            if (tokens.Count > 0)
            {
                foreach (var argTokens in tokens.SplitByType(DkxTokenType.Delimiter))
                {
                    var stream = new DkxTokenStream(argTokens);

                    string name = null;
                    Span nameSpan;
                    ArgumentPassType passType = ArgumentPassType.ByValue;

                    if (stream.EndOfStream)
                    {
                        if (!reportedEmptyArgs) Report(_nameSpan, ErrorCode.MethodContainsEmptyArguments);
                        reportedEmptyArgs = true;
                        name = string.Format(DkxConst.Variables.UnnamedArgumentFormat, ++unnamedIndex);
                        nameSpan = _nameSpan;
                    }

                    var token = stream.Peek();
                    if (token.IsKeyword(DkxConst.Keywords.Ref))
                    {
                        passType = ArgumentPassType.ByReference;
                    }
                    else if (token.IsKeyword(DkxConst.Keywords.Out))
                    {
                        passType = ArgumentPassType.Out;
                    }

                    if (!ExpressionParser.TryReadDataType(this, stream, out var dataType, out var dataTypeSpan))
                    {
                        Report(argTokens.First().Span, ErrorCode.ExpectedDataType);
                        dataType = DataType.Int;
                    }
                    else
                    {
                        token = stream.Peek();
                        if (token.IsIdentifier())
                        {
                            name = token.Text;
                            nameSpan = token.Span;
                            if (args.Any(x => x.Name == name)) Report(token.Span, ErrorCode.DuplicateArgumentName);
                            stream.Position++;
                        }
                        else
                        {
                            Report(argTokens.First().Span, ErrorCode.ExpectedArgumentName);
                            name = string.Format(DkxConst.Variables.UnnamedArgumentFormat, ++unnamedIndex);
                            nameSpan = dataTypeSpan;
                        }

                        args.Add(new Variable(
                            class_: Class,
                            name: name,
                            wbdkName: name,
                            dataType: dataType,
                            fileContext: FileContext.NeutralClass,
                            passType: passType,
                            accessMethod: FieldAccessMethod.Variable,
                            flags: default,
                            local: true,
                            privacy: Privacy.Public,
                            initializer: null,
                            nameSpan));
                    }
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
                    if (_flags.HasFlag(ModifierFlags.Static))
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
                        if (arg.PassType == ArgumentPassType.ByReference)
                        {
                            sb.Append(DkxConst.Keywords.Ref);
                            sb.Append(' ');
                        }
                        else if (arg.PassType == ArgumentPassType.Out)
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

        internal void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            if (_fileTarget != context.FileTarget) return;

            cw.Write(_returnDataType.ToWbdkCode());
            cw.Write(' ');
            cw.Write(_wbdkName);
            cw.Write('(');
            var firstArg = true;
            if (!_flags.IsStatic() && !_flags.IsConstructor())
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
                if (arg.PassType == ArgumentPassType.ByReference || arg.PassType == ArgumentPassType.Out) cw.Write('&');
                cw.Write(arg.WbdkName);
            }
            cw.Write(')');
            cw.WriteLine();
            using (cw.Indent())
            {
                var flow = new FlowTrace();
                var methodContext = new CodeGenerationContext(context, this);

                // Variables
                if (_flags.IsConstructor())
                {
                    cw.Write(DataType.UInt.ToWbdkCode());
                    cw.WriteSpace();
                    cw.Write(DkxConst.Keywords.This);
                    cw.Write(DkxConst.StatementEndToken);
                    cw.WriteLine();
                }
                foreach (var variable in _wbdkVariables)
                {
                    cw.Write(variable.DataType.ToWbdkCode());
                    cw.WriteSpace();
                    cw.Write(variable.WbdkName);
                    cw.Write(DkxConst.StatementEndToken);
                    cw.WriteLine();
                }

                // Set arguments as initialized
                foreach (var arg in _variableStore.GetVariables(includeParents: false).Where(v => v.Local && v.PassType != null))
                {
                    switch (arg.PassType.Value)
                    {
                        case ArgumentPassType.ByValue:
                        case ArgumentPassType.ByReference:
                            flow.OnVariableAssigned(arg.WbdkName);
                            break;
                    }
                }

                // Statements
                if (_flags.IsConstructor())
                {
                    cw.Write(DkxConst.Keywords.This);
                    cw.WriteSpace();
                    cw.Write(DkxConst.Operators.AssignChar);
                    cw.WriteSpace();
                    cw.Write(ObjectAccess.GenerateNewObject(GetScope<ClassScope>(), _nameSpan));
                    cw.WriteStatementEnd();
                    cw.WriteLine();
                }
                foreach (var statement in _statements ?? Statement.EmptyArray)
                {
                    statement.GenerateWbdkCode(methodContext, cw, flow);
                }

                if (_flags.IsConstructor() && !flow.IsEnded)
                {
                    cw.Write(DkxConst.Keywords.Return);
                    cw.WriteSpace();
                    cw.Write(DkxConst.Keywords.This);
                    cw.WriteStatementEnd();
                    cw.WriteLine();
                }

                GenerateScopeEnding(methodContext, cw, flow, methodEnding: true, _nameSpan);
            }
        }

        public void AddWbdkVariable(Variable variable)
        {
            _wbdkVariables.Add(variable ?? throw new ArgumentNullException(nameof(variable)));
        }

        public bool HasWbdkVariable(string wbdkName)
        {
            foreach (var variable in _wbdkVariables)
            {
                if (variable.WbdkName == wbdkName) return true;
            }
            return false;
        }

        public IEnumerable<Variable> GetWbdkVariables() => _wbdkVariables;
    }
}
