using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes
{
    class PropertyScope : Scope, IField, IObjectReferenceScope
    {
        private ClassScope _class;
        private string _name;
        private Span _nameSpan;
        private DataType _dataType;
        private bool _static;
        private Privacy _privacy;
        private FileContext _fileContext;
        private PropertyAccessorScope _getter;
        private PropertyAccessorScope _setter;

        private PropertyScope(
            ClassScope class_,
            string className,
            string name,
            Span nameSpan,
            DataType dataType,
            Modifiers modifiers)
            : base(class_)
        {
            _class = class_;
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _dataType = dataType;

            _static = modifiers.Static;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
        }

        public FieldAccessMethod AccessMethod => FieldAccessMethod.Property;
        public ClassScope Class => _class;
        IClass IField.Class => _class;
        public string ClassName => _class.Name;
        public ConstTerm ConstantExpression => null;
        public ConstValue ConstantValue => null;
        public DataType DataType => _dataType;
        public Span DefinitionSpan => _nameSpan;
        public bool IsConstant => false;
        public string Name => _name;
        public uint Offset => default;
        public bool ReadOnly => _setter == null;
        public Privacy ReadPrivacy => _getter?.Privacy ?? Privacy.Private;
        public DataType ScopeDataType => Parent.GetScope<IObjectReferenceScope>().ScopeDataType;
        public bool ScopeStatic => _static;
        public bool Static => _static;
        public Privacy WritePrivacy => _setter?.Privacy ?? Privacy.Private;

        public static PropertyScope Parse(
            ClassScope class_,
            string className,
            string name,
            Span nameSpan,
            DataType dataType,
            Modifiers modifiers,
            DkxTokenCollection bodyTokens,
            ProcessingDepth depth,
            IResolver resolver)
        {
            var propertyScope = new PropertyScope(class_, className, name, nameSpan, dataType, modifiers);

            ParseAccessors(propertyScope, bodyTokens, depth, resolver, nameSpan);

            return propertyScope;
        }

        private static void ParseAccessors(PropertyScope propertyScope, DkxTokenCollection tokens, ProcessingDepth depth, IResolver resolver, Span nameSpan)
        {
            if (!tokens.Any()) propertyScope.Report(nameSpan, ErrorCode.PropertyHasNoGetterOrSetter);

            var used = new TokenUseTracker();

            foreach (var index in tokens.FindIndices(t => t.IsKeyword(DkxConst.Keywords.Get) || t.IsKeyword(DkxConst.Keywords.Set)))
            {
                var keywordToken = tokens[index];
                used.Use(keywordToken);

                if (tokens[index + 1].IsScope)
                {
                    var bodyToken = tokens[index + 1];
                    used.Use(bodyToken);

                    var modifiers = Modifiers.ReadModifiers(tokens, index, used, propertyScope);
                    modifiers.CheckForPropertyAccessor(propertyScope, modifiers);

                    if (keywordToken.IsKeyword(DkxConst.Keywords.Get))
                    {
                        if (propertyScope._getter != null) propertyScope.Report(keywordToken.Span, ErrorCode.DuplicatePropertyGetter);
                        else propertyScope._getter = PropertyAccessorScope.Parse(
                            property: propertyScope,
                            accessorType: PropertyAccessorType.Getter,
                            privacy: modifiers.Privacy ?? Privacy.Private,
                            bodyTokens: depth == ProcessingDepth.Full ? bodyToken.Tokens : null,
                            resolver: resolver);
                    }
                    else
                    {
                        if (propertyScope._setter != null) propertyScope.Report(keywordToken.Span, ErrorCode.DuplicatePropertySetter);
                        else propertyScope._setter = PropertyAccessorScope.Parse(
                            property: propertyScope,
                            accessorType: PropertyAccessorType.Setter,
                            privacy: modifiers.Privacy ?? Privacy.Private,
                            bodyTokens: depth == ProcessingDepth.Full ? bodyToken.Tokens : null,
                            resolver: resolver);
                    }
                }
            }

            propertyScope.ReportUnusedTokens(tokens, used);
        }

        internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            if (_getter == null) throw new InvalidOperationException("Property has no getter.");

            _getter.GenerateWbdkCode(context, cw);
            _setter?.GenerateWbdkCode(context, cw);
        }

        class PropertyAccessorScope : Scope, IReturnScope, IVariableScope
        {
            private PropertyScope _property;
            private PropertyAccessorType _accessorType;
            private Privacy _privacy;
            private Statement[] _statements;
            private VariableStore _variableStore;

            private PropertyAccessorScope(PropertyScope property, PropertyAccessorType accessorType, Privacy privacy, DkxTokenCollection bodyTokens, IResolver resolver)
                : base(property)
            {
                _property = property ?? throw new ArgumentNullException(nameof(property));
                _accessorType = accessorType;
                _privacy = privacy;
                _variableStore = new VariableStore(property.GetScope<IVariableScope>());

                if (_accessorType == PropertyAccessorType.Setter)
                {
                    // Add the implicit 'value' argument
                    _variableStore.AddVariable(new Variable(
                        class_: _property._class,
                        name: DkxConst.Properties.SetterArgumentName,
                        wbdkName: DkxConst.Properties.SetterArgumentName,
                        dataType: _property.DataType,
                        fileContext: FileContext.NeutralClass,
                        passType: ArgumentPassType.ByValue,
                        static_: false,
                        local: true,
                        privacy: Privacy.Public,
                        initializer: null,
                        span: _property._nameSpan));
                }
            }

            public Privacy Privacy => _privacy;
            public DataType ReturnDataType => _property.DataType;
            public IVariableStore VariableStore => _variableStore;

            public static PropertyAccessorScope Parse(
                PropertyScope property,
                PropertyAccessorType accessorType,
                Privacy privacy,
                DkxTokenCollection bodyTokens,
                IResolver resolver)
            {
                var accessorScope = new PropertyAccessorScope(property, accessorType, privacy, bodyTokens, resolver);

                if (bodyTokens != null)
                {
                    accessorScope._statements = StatementParser.SplitTokensIntoStatements(accessorScope, bodyTokens, resolver);
                }

                return accessorScope;
            }

            internal override void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
            {
                if (_accessorType == PropertyAccessorType.Getter)
                {
                    cw.Write(_property.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(DkxConst.Properties.GetterPrefix);
                    cw.Write(_property.Name);
                    cw.Write('(');
                    if (!_property.Static) cw.Write("unsigned int this");
                    cw.Write(')');
                }
                else
                {
                    cw.Write(DkxConst.Keywords.Void);
                    cw.Write(' ');
                    cw.Write(DkxConst.Properties.SetterPrefix);
                    cw.Write(_property.Name);
                    cw.Write('(');
                    if (!_property.Static) cw.Write("unsigned int this, ");
                    cw.Write(_property.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(DkxConst.Properties.SetterArgumentName);
                    cw.Write(')');
                }

                cw.WriteLine();
                using (cw.Indent())
                {
                    if (_statements != null)
                    {
                        foreach (var statement in _statements)
                        {
                            statement.GenerateWbdkCode(context, cw);
                        }
                    }
                }
            }
        }

        enum PropertyAccessorType
        {
            Getter,
            Setter
        }
    }
}
