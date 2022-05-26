using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Resolving;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes
{
    class ClassScope : Scope, IClass, IVariableScope, IClassNamingScope, IObjectReferenceScope
    {
        private string _name;
        private string _fullName;
        private string[] _fullNameParts;
        private Privacy _privacy;
        private bool _static;
        private List<MethodScope> _methods = new List<MethodScope>();
        private List<PropertyScope> _properties = new List<PropertyScope>();
        private List<Constant> _constants = new List<Constant>();
        private VariableStore _variableStore;
        private uint _dataSize;
        private DataType _dataType;

        public ClassScope(Scope parent, string className, Modifiers modifiers)
            : base(parent)
        {
            _name = className ?? throw new ArgumentNullException(nameof(className));
            _variableStore = new VariableStore(parent?.GetScope<IVariableScope>());

            _privacy = modifiers.Privacy ?? Privacy.Public;
            _static = modifiers.Static;

            var parentNamingScope = parent.GetScope<IClassNamingScope>();
            if (parentNamingScope == null) throw new InvalidOperationException("Class has no parent naming scope.");

            _fullName = string.Concat(parentNamingScope.FullClassName, DkxConst.Operators.Dot, _name);
            _fullNameParts = parentNamingScope.FullClassNameParts.Concat(new string[] { _name }).ToArray();
            _dataType = new DataType(this);
        }

        public uint DataSize => _dataSize;
        public string FullClassName => _fullName;
        public IEnumerable<string> FullClassNameParts => _fullNameParts;
        public IEnumerable<MethodScope> Methods => _methods;
        public string Name => _name;
        public Privacy Privacy => _privacy;
        public DataType ScopeDataType => _dataType;
        public bool ScopeStatic => true;
        public bool Static => _static;
        public IVariableStore VariableStore => _variableStore;

        public async Task ProcessTokensAsync(DkxTokenCollection tokens, ProcessingDepth depth, IResolver resolver)
        {
            var used = new TokenUseTracker();
            var classResolver = new ClassResolver(this, resolver);

            var pos = 0;
            while (pos < tokens.Count)
            {
                if (!tokens[pos].IsDataType || !tokens[pos + 1].IsIdentifier)
                {
                    pos++;
                    continue;
                }

                var dataTypeToken = tokens[pos];
                var nameToken = tokens[pos + 1];

                if (tokens[pos + 2].IsBrackets)
                {
                    // This is a method
                    var argsToken = tokens[pos + 2];
                    used.Use(dataTypeToken, nameToken, argsToken);
                    if (tokens[pos + 3].IsScope)
                    {
                        var scopeToken = tokens[pos + 3];
                        used.Use(scopeToken);

                        var modifiers = await Modifiers.ReadModifiersAsync(tokens, pos, used, this);
                        await modifiers.CheckForMethodAsync(this);

                        var method = await MethodScope.ParseAsync(
                            parent: this,
                            className: _name,
                            name: nameToken.Text,
                            nameSpan: nameToken.Span,
                            returnDataType: dataTypeToken.DataType,
                            argumentTokens: argsToken.Tokens,
                            modifiers: modifiers,
                            bodyTokens: depth == ProcessingDepth.Full ? scopeToken.Tokens : null,
                            resolver: classResolver);

                        _methods.Add(method);
                        pos += 4;
                    }
                    else
                    {
                        await ReportAsync(argsToken.Span, ErrorCode.ExpectedToken, '{');
                        pos += 3;
                    }
                }
                else if (tokens[pos + 2].IsScope)
                {
                    // This is a property
                    var scopeToken = tokens[pos + 2];
                    used.Use(dataTypeToken, nameToken, scopeToken);

                    var modifiers = await Modifiers.ReadModifiersAsync(tokens, pos, used, this);
                    await modifiers.CheckForPropertyAsync(this);

                    var property = await PropertyScope.ParseAsync(
                        parent: this,
                        className: _name,
                        name: nameToken.Text,
                        nameSpan: nameToken.Span,
                        dataType: dataTypeToken.DataType,
                        modifiers: modifiers,
                        bodyTokens: scopeToken.Tokens,
                        depth: depth,
                        resolver: classResolver);

                    _properties.Add(property);
                    pos += 3;
                }
                else
                {
                    // This is a constant or member variable
                    used.Use(dataTypeToken, nameToken);
                    var modifiers = await Modifiers.ReadModifiersAsync(tokens, pos, used, this);

                    if (tokens[pos + 2].IsOperator(Operator.Assign))
                    {
                        var assignToken = tokens[pos + 2];
                        used.Use(assignToken);

                        var end = tokens.FindIndex(t => t.IsStatementEnd, pos + 4);
                        if (end > 0)
                        {
                            var initializerTokens = tokens.GetRange(pos + 4, end - (pos + 4));
                            used.Use(initializerTokens);
                            used.Use(tokens[end]);

                            var initializerStream = initializerTokens.ToStream();
                            var initializerChain = await ExpressionParser.TryReadExpressionAsync(this, initializerStream, resolver);
                            if (initializerChain == null) await ReportAsync(initializerTokens.Span, ErrorCode.ExpectedExpression);
                            else if (!initializerStream.EndOfStream) await ReportAsync(initializerStream.Read().Span, ErrorCode.SyntaxError);

                            if (modifiers.Const)
                            {
                                var constValue = await initializerChain.GetConstantOrNullAsync(this);
                                if (constValue == null)
                                {
                                    await ReportAsync(initializerChain.Span, ErrorCode.ExpressionNotConstant);
                                    constValue = new NullConstantValue(initializerChain.Span);
                                }
                                var constant = new Constant(nameToken.Text, dataTypeToken.DataType, constValue, modifiers.Privacy ?? Privacy.Private);
                                _constants.Add(constant);
                            }
                            else
                            {
                                var variable = new Variable(
                                    name: nameToken.Text,
                                    wbdkName: string.Concat(_name, "_", nameToken.Text),
                                    dataType: dataTypeToken.DataType,
                                    fileContext: FileContext.NeutralClass,
                                    passType: null,
                                    static_: modifiers.Static,
                                    local: false,
                                    privacy: modifiers.Privacy ?? Privacy.Private,
                                    initializer: initializerChain);

                                _variableStore.AddVariable(variable);
                            }

                            pos = end + 1;
                        }
                        else
                        {
                            await ReportAsync(assignToken.Span, ErrorCode.ExpectedExpression);
                            pos += 2;
                        }
                    }
                    else // No '=' after name
                    {
                        if (modifiers.Const)
                        {
                            await ReportAsync(nameToken.Span, ErrorCode.ConstantsMustHaveInitializer);
                            pos += 2;
                        }
                        else
                        {
                            if (tokens[pos + 2].IsStatementEnd)
                            {
                                used.Use(tokens[pos + 2]);

                                var variable = new Variable(
                                    name: nameToken.Text,
                                    wbdkName: string.Concat(_name, "_", nameToken.Text),
                                    dataType: dataTypeToken.DataType,
                                    fileContext: FileContext.NeutralClass,
                                    passType: null,
                                    static_: modifiers.Static,
                                    local: false,
                                    privacy: modifiers.Privacy ?? Privacy.Private,
                                    initializer: null);

                                _variableStore.AddVariable(variable);
                                pos += 3;
                            }
                            else
                            {
                                await ReportAsync(nameToken.Span, ErrorCode.ExpectedToken, ';');
                                pos += 2;
                            }
                        }
                    }
                }
            }

            foreach (var badToken in tokens.GetUnused(used)) await ReportAsync(badToken.Span, ErrorCode.SyntaxError);

            CalculateLayout();
        }

        public IEnumerable<FileContext> GetFileContexts()
        {
            var fileContexts = new List<FileContext>();
            foreach (var method in _methods)
            {
                var fc = method.FileContext;
                if (!fileContexts.Contains(fc)) fileContexts.Add(fc);
            }

            // TODO: include properties

            return fileContexts;
        }

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            // Generate global variables for any static member variables
            var gotMemberVariable = false;
            foreach (var memberVariable in _variableStore.GetVariables(includeParents: false))
            {
                if (!memberVariable.Static) continue;

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
                await property.GenerateWbdkCodeAsync(cw);
                cw.WriteLine();
            }

            foreach (var method in _methods)
            {
                await method.GenerateWbdkCodeAsync(cw);
                cw.WriteLine();
            }
        }

        private void CalculateLayout()
        {
            _dataSize = 0U;

            foreach (var memberVariable in _variableStore.GetVariables(includeParents: false).Where(v => !v.Static))
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

        Task<IEnumerable<IMethod>> IClass.GetMethods(string name) => Task.FromResult<IEnumerable<IMethod>>(_methods);

        Task<IEnumerable<IField>> IClass.GetFields(string name)
        {
            return Task.FromResult
            (
                _variableStore.GetVariables(includeParents: false)
                .Where(x => x.ArgumentType == null && x.Name == name)
                .Cast<IField>()
                .Concat(_constants.Where(x => x.Name == name).Cast<IField>())
                .Concat(_properties.Where(x => x.Name == name).Cast<IField>())
            );
        }
    }
}
