using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Jobs;
using DKX.Compilation.Project;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
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
        private Privacy _privacy;
        private ModifierFlags _flags;
        private List<MethodScope> _methods = new List<MethodScope>();
        private List<PropertyScope> _properties = new List<PropertyScope>();
        private VariableStore _variableStore;
        private ConstantStore _constantStore;
        private uint _dataSize;
        private DataType _dataType;

        public ClassScope(Scope parent, string namespaceName, string className, string dkxPathName, Modifiers modifiers)
            : base(parent, parent.Phase, parent.Resolver, parent.Project)
        {
            _name = className ?? throw new ArgumentNullException(nameof(className));
            _namespaceName = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
            _dkxPathName = dkxPathName;
            _variableStore = new VariableStore(parent?.GetScope<IVariableScope>());
            _constantStore = new ConstantStore(parent?.GetScope<IConstantScope>());
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _flags = modifiers.Flags;

            _fullName = string.Concat(_namespaceName, DkxConst.Operators.Dot, _name);
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
        public Privacy Privacy => _privacy;
        public DataType ScopeDataType => _dataType;
        public bool ScopeStatic => _flags.HasFlag(ModifierFlags.Static);
        public bool Static => _flags.HasFlag(ModifierFlags.Static);
        public IVariableStore VariableStore => _variableStore;
        public string WbdkClassName => _wbdkClassName;

        public void ProcessTokens(DkxTokenCollection tokens)
        {
            var used = new TokenUseTracker();

            var pos = 0;
            while (pos < tokens.Count)
            {
                // TODO: A data type could span multiple tokens (e.g. System.Console)
                // Use ExpressionParser.TryReadDataType() instead
                if (!ExpressionParser.TokenIsDataType(tokens[pos], Resolver, out var dataType, out var dataTypeInvalid))
                {
                    pos++;
                    continue;
                }
                var dataTypeIndex = pos;
                var dataTypeToken = tokens[pos];
                if (dataTypeInvalid && Phase == CompilePhase.FullCompilation) Report(dataTypeToken.Span, ErrorCode.InvalidDataType);

                var nameIndex = dataTypeIndex + 1;
                var nameToken = tokens[nameIndex];

                if (tokens[nameIndex + 1].IsBrackets)
                {
                    // This is a method
                    var argsIndex = nameIndex + 1;
                    used.Use(dataTypeToken, nameToken);
                    if (tokens[argsIndex + 1].IsScope)
                    {
                        var scopeIndex = argsIndex + 1;
                        ProcessMethod(tokens, dataTypeIndex, dataTypeToken, nameToken, tokens[argsIndex], tokens[scopeIndex], used);
                        pos = scopeIndex + 1;
                    }
                    else
                    {
                        Report(tokens[argsIndex].Span, ErrorCode.ExpectedToken, '{');
                        pos = argsIndex + 1;
                    }
                }
                else if (tokens[nameIndex + 1].IsScope)
                {
                    // This is a property
                    var scopeIndex = nameIndex + 1;
                    ProcessProperty(tokens, dataTypeIndex, dataTypeToken, nameToken, tokens[scopeIndex], used);
                    pos = scopeIndex + 1;
                }
                else
                {
                    // This is a constant or member variable
                    ProcessMemberVariableOrConstant(tokens, ref pos, dataTypeIndex, dataTypeToken, nameIndex, nameToken, used);
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

        private void ProcessMethod(DkxTokenCollection tokens, int dataTypeIndex, DkxToken dataTypeToken,
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

            used.Use(dataTypeToken, nameToken, argsToken, scopeToken);

            var modifiers = Modifiers.ReadModifiers(this, tokens, dataTypeIndex, used);

            var method = MethodScope.Parse(
                parent: this,
                className: _name,
                name: nameToken.Text,
                nameSpan: nameToken.Span,
                returnDataType: dataTypeToken.DataType,
                argumentTokens: argsToken.Tokens,
                modifiers: modifiers,
                bodyTokens: Phase == CompilePhase.FullCompilation ? scopeToken.Tokens : null,
                phase: Phase,
                resolver: Resolver,
                project: Project);

            _methods.Add(method);
        }

        private void ProcessProperty(DkxTokenCollection tokens, int dataTypeIndex, DkxToken dataTypeToken, DkxToken nameToken, DkxToken scopeToken, TokenUseTracker used)
        {
            switch (Phase)
            {
                case CompilePhase.MemberScan:
                case CompilePhase.FullCompilation:
                    break;
                default:
                    return;
            }

            var modifiers = Modifiers.ReadModifiers(this, tokens, dataTypeIndex, used);
            if (Phase == CompilePhase.FullCompilation) modifiers.CheckForProperty(this);

            used.Use(dataTypeToken, nameToken, scopeToken);

            var property = PropertyScope.Parse(
                class_: this,
                name: nameToken.Text,
                nameSpan: nameToken.Span,
                dataType: dataTypeToken.DataType,
                modifiers: modifiers,
                bodyTokens: scopeToken.Tokens,
                phase: Phase,
                resolver: Resolver);

            _properties.Add(property);
        }

        private void ProcessMemberVariableOrConstant(DkxTokenCollection tokens, ref int pos,
            int dataTypeIndex, DkxToken dataTypeToken,
            int nameIndex, DkxToken nameToken,
            TokenUseTracker used)
        {
            used.Use(dataTypeToken, nameToken);
            var modifiers = Modifiers.ReadModifiers(this, tokens, pos, used);

            if (tokens[nameIndex + 1].IsOperator(Operator.Assign))
            {
                var assignIndex = nameIndex + 1;
                var assignToken = tokens[assignIndex];
                used.Use(assignToken);

                var end = tokens.FindStatementEnd(assignIndex + 1);
                if (end > 0)
                {
                    var initializerTokens = tokens.GetRange(assignIndex + 1, end - (assignIndex + 1));
                    used.Use(initializerTokens);
                    used.Use(tokens[end]);

                    Chain initializerChain = null;
                    if (Phase >= CompilePhase.ConstantResolution)
                    {
                        var initializerStream = initializerTokens.ToStream();
                        initializerChain = ExpressionParser.ReadExpressionOrNull(this, initializerStream);
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
                                dataType: dataTypeToken.DataType,
                                constTerm: constTerm,
                                constValueOrNull: null,
                                privacy: modifiers.Privacy ?? Privacy.Private,
                                span: nameToken.Span);

                            _constantStore.Add(constant);
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
                                dataType: dataTypeToken.DataType,
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
                    }
                    pos = end + 1;
                }
                else
                {
                    Report(assignToken.Span, ErrorCode.ExpectedExpression);
                    pos = assignIndex + 1;
                }
            }
            else // No '=' after name
            {
                if (modifiers.Const)
                {
                    if (Phase == CompilePhase.MemberScan)
                    {
                        Report(nameToken.Span, ErrorCode.ConstantsMustHaveInitializer);
                        pos = nameIndex + 1;
                    }
                }
                else
                {
                    if (tokens[nameIndex + 1].IsStatementEnd)
                    {
                        var statementEndIndex = nameIndex + 1;
                        used.Use(tokens[statementEndIndex]);

                        if (Phase < CompilePhase.ConstantResolution)
                        {
                            var variable = new Variable(
                                class_: this,
                                name: nameToken.Text,
                                wbdkName: nameToken.Text,
                                dataType: dataTypeToken.DataType,
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
                        pos = statementEndIndex + 1;
                    }
                    else
                    {
                        Report(nameToken.Span, ErrorCode.ExpectedToken, ';');
                        pos = nameIndex + 1;
                    }
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

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            // Put the original class name at the top of the file, for troubleshooting if needed.
            cw.WriteLine($"// {_fullName}");
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
                .Where(x => x.ArgumentType == null && x.Name == name)
                .Cast<IField>()
                .Concat(_constantStore.GetConstants(name).Cast<IField>())
                .Concat(_properties.Where(x => x.Name == name).Cast<IField>());
        }

        IEnumerable<IField> IClass.Fields
        {
            get
            {
                return _variableStore.GetVariables(includeParents: false)
                    .Where(x => x.ArgumentType == null)
                    .Cast<IField>()
                    .Concat(_constantStore.Constants.Cast<IField>())
                    .Concat(_properties.Cast<IField>());
            }
        }
    }
}
