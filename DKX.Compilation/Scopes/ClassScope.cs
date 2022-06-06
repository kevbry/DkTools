using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Jobs;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Scopes
{
    class ClassScope : Scope, IClass, IVariableScope, IConstantScope, IObjectReferenceScope
    {
        private string _name;
        private string _namespaceName;
        private string _fullName;
        private string _wbdkClassName;
        private string _dkxPathName;
        private Span _nameSpan;
        private Privacy _privacy;
        private ModifierFlags _flags;
        private List<MethodScope> _methods = new List<MethodScope>();
        private List<PropertyScope> _properties = new List<PropertyScope>();
        private VariableStore _variableStore;
        private ConstantStore _constantStore;
        private uint _dataSize;
        private DataType _dataType;

        public ClassScope(Scope parent, string namespaceName, string className, string fullClassName, string dkxPathName, Span nameSpan, Modifiers modifiers)
            : base(parent, parent.Phase, parent.Resolver, parent.Project)
        {
            _name = className ?? throw new ArgumentNullException(nameof(className));
            _namespaceName = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
            _fullName = fullClassName ?? throw new ArgumentNullException(nameof(fullClassName));
            _dkxPathName = dkxPathName;
            _nameSpan = nameSpan;
            _variableStore = new VariableStore(parent?.GetScope<IVariableScope>());
            _constantStore = new ConstantStore(parent?.GetScope<IConstantScope>());
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _flags = modifiers.Flags;

            _wbdkClassName = Compiler.GetWbdkClassName(_fullName);
            _dataType = new DataType(this);

            Resolver = new ClassResolver(this, parent.Resolver);

            if (Phase >= CompilePhase.ConstantResolution)
            {
                var projectClass = Project.GetClassByFullNameOrNull(_fullName);
                if (projectClass == null) throw new InvalidOperationException($"Class '{_fullName}' not found in project.");

                var projectConstants = projectClass.Fields.Where(x => x.AccessMethod == FieldAccessMethod.Constant).ToList();
                foreach (var projectConstant in projectConstants) _constantStore.Add(new Constant(projectConstant));

                var projectVariables = projectClass.Fields.Where(x => x.AccessMethod != FieldAccessMethod.Constant).ToList();
                foreach (var projectVariable in projectVariables) _variableStore.AddVariable(new Variable(projectVariable));
            }
        }

        public uint DataSize => _dataSize;
        public string ClassName => _name;
        public IConstantStore ConstantStore => _constantStore;
        public string DkxPathName => _dkxPathName;
        public ModifierFlags Flags => _flags;
        public string FullClassName => _fullName;
        public IEnumerable<MethodScope> Methods => _methods;
        IEnumerable<IMethod> IClass.Methods => _methods;
        public string Name => _name;
        public string NamespaceName => _namespaceName;
        public Span NameSpan => _nameSpan;
        public Privacy Privacy => _privacy;
        public DataType ScopeDataType => _dataType;
        public bool ScopeStatic => _flags.HasFlag(ModifierFlags.Static);
        public bool Static => _flags.HasFlag(ModifierFlags.Static);
        public IVariableStore VariableStore => _variableStore;
        public string WbdkClassName => _wbdkClassName;

        public override string ToString() => $"ClassScope: {_fullName}";

        public void ProcessTokens(DkxTokenCollection tokens)
        {
            DkxToken nameToken;

            try
            {
                var used = new TokenUseTracker();

                var stream = new DkxTokenStream(tokens);
                while (!stream.EndOfStream)
                {
                    if (stream.Peek().IsIdentifier(_name) && stream.Peek(1).IsBrackets && stream.Peek(2).IsScope)
                    {
                        // Constructor
                        var nameIndex = stream.Position;
                        nameToken = stream.Read();
                        var argsToken = stream.Read();
                        var scopeToken = stream.Read();
                        used.Use(nameToken, argsToken, scopeToken);
                        ProcessConstructor(tokens, nameIndex, nameToken, argsToken, scopeToken, used);
                        continue;
                    }

                    var dataTypePos = stream.Position;
                    if (!ExpressionParser.TryReadDataType(this, stream, out var dataType, out var dataTypeSpan))
                    {
                        stream.Position++;
                        continue;
                    }

                    nameToken = stream.Read();
                    if (!nameToken.IsIdentifier()) continue;
                    used.Use(stream.GetRange(dataTypePos, stream.Position - dataTypePos));

                    if (stream.Peek().IsBrackets)
                    {
                        // This is a method
                        var argsToken = stream.Read();
                        used.Use(argsToken);
                        if (stream.Peek().IsScope)
                        {
                            var scopeToken = stream.Read();
                            used.Use(scopeToken);
                            ProcessMethod(tokens, dataType, dataTypePos, nameToken, argsToken, scopeToken, used);
                        }
                        else throw new CodeException(argsToken.Span, ErrorCode.ExpectedScope);
                    }
                    else if (stream.Peek().IsScope)
                    {
                        // This is a property
                        var scopeToken = stream.Read();
                        used.Use(scopeToken);
                        ProcessProperty(tokens, dataType, dataTypePos, nameToken, scopeToken, used);
                    }
                    else
                    {
                        // This is a member variable or constant
                        ProcessMemberVariableOrConstant(stream, dataType, dataTypePos, nameToken, used);
                    }
                }

                if (Phase >= CompilePhase.MemberScan)
                {
                    CalculateLayout();
                }

                if (Phase == CompilePhase.FullCompilation)
                {
                    ReportUnusedTokens(tokens, used);
                }
            }
            catch (CodeException ex)
            {
                AddReportItem(ex.ToReportItem());
            }
        }

        private void ProcessConstructor(DkxTokenCollection tokens, int nameIndex, DkxToken nameToken, DkxToken argsToken, DkxToken scopeToken, TokenUseTracker used)
        {
            switch (Phase)
            {
                case CompilePhase.MemberScan:
                case CompilePhase.ConstantResolution:
                case CompilePhase.FullCompilation:
                    break;
                default:
                    return;
            }

            var modifiers = Modifiers.ReadModifiers(this, tokens, nameIndex, used);
            modifiers.Flags |= ModifierFlags.NotCallable | ModifierFlags.Constructor;

            var method = MethodScope.Parse(
                parent: this,
                className: _name,
                name: _name,
                nameSpan: nameToken.Span,
                returnDataType: new DataType(this),
                argumentTokens: argsToken.Tokens,
                modifiers: modifiers,
                bodyTokens: Phase == CompilePhase.FullCompilation ? scopeToken.Tokens : null,
                phase: Phase,
                resolver: Resolver,
                project: Project);

            if (_methods.Any(x => x.WbdkName == method.WbdkName)) Report(nameToken.Span, ErrorCode.DuplicateMethod, nameToken.Text);

            _methods.Add(method);
        }

        private void ProcessMethod(DkxTokenCollection tokens, DataType dataType, int dataTypeIndex,
            DkxToken nameToken, DkxToken argsToken, DkxToken scopeToken, TokenUseTracker used)
        {
            switch (Phase)
            {
                case CompilePhase.MemberScan:
                case CompilePhase.ConstantResolution:
                case CompilePhase.FullCompilation:
                    break;
                default:
                    return;
            }

            if (nameToken.Text == _name) Report(nameToken.Span, ErrorCode.MemberNameCannotBeSameAsClassName);

            var modifiers = Modifiers.ReadModifiers(this, tokens, dataTypeIndex, used);

            var method = MethodScope.Parse(
                parent: this,
                className: _name,
                name: nameToken.Text,
                nameSpan: nameToken.Span,
                returnDataType: dataType,
                argumentTokens: argsToken.Tokens,
                modifiers: modifiers,
                bodyTokens: Phase == CompilePhase.FullCompilation ? scopeToken.Tokens : null,
                phase: Phase,
                resolver: Resolver,
                project: Project);

            if (_methods.Any(x => x.WbdkName == method.WbdkName)) Report(nameToken.Span, ErrorCode.DuplicateMethod, nameToken.Text);

            _methods.Add(method);
        }

        private void ProcessProperty(DkxTokenCollection tokens, DataType dataType, int dataTypeIndex, DkxToken nameToken, DkxToken scopeToken, TokenUseTracker used)
        {
            switch (Phase)
            {
                case CompilePhase.MemberScan:
                case CompilePhase.FullCompilation:
                    break;
                default:
                    return;
            }

            if (nameToken.Text == _name) Report(nameToken.Span, ErrorCode.MemberNameCannotBeSameAsClassName);

            var modifiers = Modifiers.ReadModifiers(this, tokens, dataTypeIndex, used);
            if (Phase == CompilePhase.FullCompilation) modifiers.CheckForProperty(this, GetScope<ClassScope>(), nameToken.Span);

            var property = PropertyScope.Parse(
                class_: this,
                name: nameToken.Text,
                nameSpan: nameToken.Span,
                dataType: dataType,
                modifiers: modifiers,
                bodyTokens: scopeToken.Tokens,
                phase: Phase,
                resolver: Resolver);

            _properties.Add(property);
        }

        private void ProcessMemberVariableOrConstant(DkxTokenStream stream, DataType dataType, int dataTypeIndex, DkxToken nameToken, TokenUseTracker used)
        {
            if (nameToken.Text == _name) Report(nameToken.Span, ErrorCode.MemberNameCannotBeSameAsClassName);

            var modifiers = Modifiers.ReadModifiers(this, stream.Tokens, dataTypeIndex, used);

            if (stream.Peek().IsOperator(Operator.Assign))
            {
                var assignToken = stream.Read();
                used.Use(assignToken);

                var end = stream.Tokens.FindStatementEnd(stream.Position);
                if (end < 0) throw new CodeException(assignToken.Span, ErrorCode.ExpectedExpression);

                var initializerTokens = stream.GetRange(stream.Position, end - stream.Position);
                used.Use(initializerTokens);
                used.Use(stream.Tokens[end]);
                stream.Position = end + 1;

                Chain initializerChain = null;
                if (Phase >= CompilePhase.ConstantResolution)
                {
                    var initializerStream = initializerTokens.ToStream();
                    initializerChain = ExpressionParser.ReadExpressionOrNull(this, initializerStream, dataType);
                    if (initializerChain == null) Report(initializerTokens.Span, ErrorCode.ExpectedExpression);
                    else if (!initializerStream.EndOfStream) Report(initializerStream.Read().Span, ErrorCode.SyntaxError);
                }

                if (modifiers.Const)
                {
                    ConstTerm constTerm = null;
                    if (Phase == CompilePhase.ConstantResolution)
                    {
                        constTerm = initializerChain.ToConstTermOrNull(this);
                        if (constTerm == null)
                        {
                            Report(initializerChain.Span, ErrorCode.ExpressionNotConstant);
                            constTerm = new ConstErrorTerm(initializerChain.Span);
                        }
                    }

                    // Constants will be read from the project when in full compilation mode
                    if (Phase == CompilePhase.MemberScan || Phase == CompilePhase.ConstantResolution)
                    {
                        var constant = new Constant(
                            class_: this,
                            name: nameToken.Text,
                            dataType: dataType,
                            constTerm: constTerm,
                            constValueOrNull: null,
                            privacy: modifiers.Privacy ?? Privacy.Private,
                            span: nameToken.Span);

                        _constantStore.Add(constant);
                    }

                    if (Phase == CompilePhase.FullCompilation)
                    {
                        modifiers.CheckForConstant(this, nameToken.Span);
                    }
                }
                else
                {
                    if (Phase < CompilePhase.ConstantResolution)
                    {
                        var variable = new Variable(
                            class_: this,
                            name: nameToken.Text,
                            wbdkName: nameToken.Text,
                            dataType: dataType,
                            fileContext: FileContext.NeutralClass,
                            passType: null,
                            accessMethod: modifiers.Flags.HasFlag(ModifierFlags.Static) ? FieldAccessMethod.Variable : FieldAccessMethod.Object,
                            flags: modifiers.Flags,
                            local: false,
                            privacy: modifiers.Privacy ?? Privacy.Private,
                            initializer: initializerChain,
                            span: nameToken.Span);

                        _variableStore.AddVariable(variable);
                    }

                    if (Phase == CompilePhase.FullCompilation)
                    {
                        modifiers.CheckForMemberVariable(this, this, nameToken.Span);
                    }
                }
            }
            else // No '=' after name
            {
                if (modifiers.Const)
                {
                    throw new CodeException(nameToken.Span, ErrorCode.ConstantsMustHaveInitializer);
                }
                else
                {
                    if (stream.Peek().IsStatementEnd)
                    {
                        var statementEndToken = stream.Read();
                        used.Use(statementEndToken);

                        if (Phase < CompilePhase.ConstantResolution)
                        {
                            var variable = new Variable(
                                class_: this,
                                name: nameToken.Text,
                                wbdkName: nameToken.Text,
                                dataType: dataType,
                                fileContext: FileContext.NeutralClass,
                                passType: null,
                                accessMethod: modifiers.Flags.HasFlag(ModifierFlags.Static) ? FieldAccessMethod.Variable : FieldAccessMethod.Object,
                                flags: modifiers.Flags,
                                local: false,
                                privacy: modifiers.Privacy ?? Privacy.Private,
                                initializer: null,
                                span: nameToken.Span);

                            _variableStore.AddVariable(variable);
                        }

                        if (Phase == CompilePhase.FullCompilation)
                        {
                            modifiers.CheckForMemberVariable(this, this, nameToken.Span);
                        }
                    }
                    else throw new CodeException(nameToken.Span, ErrorCode.ExpectedStatementEndToken);
                }
            }
        }

        public IEnumerable<FileTarget> GetFileTargets()
        {
            var fileTargets = new List<FileTarget>();

            foreach (var method in _methods)
            {
                var ft = method.FileTarget;
                if (!fileTargets.Contains(ft)) fileTargets.Add(ft);
            }

            foreach (var prop in _properties)
            {
                var ft = prop.FileTarget;
                if (!fileTargets.Contains(ft)) fileTargets.Add(ft);
            }

            return fileTargets;
        }

        internal void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            // Put the original class name at the top of the file, for troubleshooting if needed.
            cw.WriteLine($"// {_fullName}");
            cw.WriteLine("#define _LINK dkx.lib");
            cw.WriteLine("#include <dkx.i>");
            cw.WriteLine("#warndel 108");
            cw.WriteLine();

            // Generate global variables for any static member variables
            var gotMemberVariable = false;
            foreach (var memberVariable in _variableStore.GetVariables(includeParents: false))
            {
                if (!memberVariable.Flags.HasFlag(ModifierFlags.Static)) continue;
                if (memberVariable.AccessMethod == FieldAccessMethod.Property) continue;

                gotMemberVariable = true;
                cw.Write(memberVariable.DataType.ToWbdkCode());
                cw.Write(' ');
                cw.Write(memberVariable.WbdkName);
                cw.Write(';');
                cw.WriteLine();
            }
            if (gotMemberVariable) cw.WriteLine();

            foreach (var property in _properties)
            {
                property.GenerateWbdkCode(context, cw);
                cw.WriteLine();
            }

            foreach (var method in _methods)
            {
                method.GenerateWbdkCode(context, cw);
                cw.WriteLine();
            }
        }

        private void CalculateLayout()
        {
            _dataSize = 0U;

            foreach (var memberVariable in _variableStore.GetVariables(includeParents: false).Where(v => v.AccessMethod == FieldAccessMethod.Object))
            {
                var dataSize = memberVariable.DataType.DataSize;
                if (dataSize > 1 && dataSize < 4) PadSize(ref _dataSize, 2);
                else if (dataSize >= 4) PadSize(ref _dataSize, 4);

                memberVariable.Offset = _dataSize;
                _dataSize += dataSize;
            }
            PadSize(ref _dataSize, 4);
        }

        private static void PadSize(ref uint size, uint alignment)
        {
            if (size % alignment != 0)
            {
                size += alignment - (size % alignment);
            }
        }

        IEnumerable<IMethod> IClass.GetMethods(string name) => _methods;

        IEnumerable<IField> IClass.GetFields(string name)
        {
            return _variableStore.GetVariables(includeParents: false)
                .Where(x => x.PassType == null && x.Name == name)
                .Cast<IField>()
                .Concat(_constantStore.GetConstants(name).Cast<IField>())
                .Concat(_properties.Where(x => x.Name == name).Cast<IField>());
        }

        IEnumerable<IField> IClass.Fields
        {
            get
            {
                return _variableStore.GetVariables(includeParents: false)
                    .Where(x => x.PassType == null)
                    .Cast<IField>()
                    .Concat(_constantStore.Constants.Cast<IField>())
                    .Concat(_properties.Cast<IField>());
            }
        }
    }
}
