using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Jobs;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using DKX.Compilation.Variables.ConstantValues;
using DKX.Compilation.Variables.ConstTerms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Scopes
{
    class PropertyScope : Scope, IField, IObjectReferenceScope
    {
        private ClassScope _class;
        private string _name;
        private Span _nameSpan;
        private DataType _dataType;
        private ModifierFlags _flags;
        private Privacy _privacy;
        private FileContext _fileContext;
        private PropertyAccessorScope _getter;
        private PropertyAccessorScope _setter;
        private FileTarget _fileTarget;

        private PropertyScope(
            ClassScope class_,
            string name,
            Span nameSpan,
            DataType dataType,
            Modifiers modifiers)
            : base(class_, class_.Phase, class_.Resolver, class_.Project)
        {
            _class = class_;
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _dataType = dataType;

            _flags = modifiers.Flags;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
            _fileTarget = new FileTarget(_fileContext, _class.WbdkClassName + FileContextHelper.GetExtension(_fileContext));
        }

        public FieldAccessMethod AccessMethod => FieldAccessMethod.Property;
        public ClassScope Class => _class;
        IClass IField.Class => _class;
        public string ClassName => _class.Name;
        public ConstTerm ConstantExpression => null;
        public ConstValue ConstantValue => null;
        public DataType DataType => _dataType;
        public Span DefinitionSpan => _nameSpan;
        public FileContext FileContext => _fileContext;
        public FileTarget FileTarget => _fileTarget;
        public ModifierFlags Flags => _flags;
        public bool IsConstant => false;
        public string Name => _name;
        public uint Offset => default;
        public Privacy ReadPrivacy => _getter?.Privacy ?? Privacy.Private;
        public DataType ScopeDataType => Parent.GetScope<IObjectReferenceScope>().ScopeDataType;
        public bool ScopeStatic => _flags.HasFlag(ModifierFlags.Static);
        public Privacy WritePrivacy => _setter?.Privacy ?? Privacy.Private;

        public static PropertyScope Parse(
            ClassScope class_,
            string name,
            Span nameSpan,
            DataType dataType,
            Modifiers modifiers,
            DkxTokenCollection bodyTokens,
            CompilePhase phase,
            IResolver resolver)
        {
            var propertyScope = new PropertyScope(class_, name, nameSpan, dataType, modifiers);

            try
            {
                ParseAccessors(propertyScope, bodyTokens, phase, resolver, nameSpan, modifiers);
            }
            catch (CodeException ex)
            {
                propertyScope.AddReportItem(ex.ToReportItem());
            }

            return propertyScope;
        }

        private static void ParseAccessors(PropertyScope propertyScope, DkxTokenCollection tokens, CompilePhase phase, IResolver resolver, Span nameSpan, Modifiers propertyModifiers)
        {
            if (!tokens.Any()) throw new CodeException(nameSpan, ErrorCode.PropertyHasNoGetterOrSetter);

            var used = new TokenUseTracker();

            foreach (var index in tokens.FindIndices(t => t.IsKeyword(DkxConst.Keywords.Get) || t.IsKeyword(DkxConst.Keywords.Set)))
            {
                var keywordToken = tokens[index];
                used.Use(keywordToken);

                if (tokens[index + 1].IsScope)
                {
                    var bodyToken = tokens[index + 1];
                    used.Use(bodyToken);

                    var modifiers = Modifiers.ReadModifiers(propertyScope, tokens, index, used);
                    modifiers.CheckForPropertyAccessor(propertyScope, propertyModifiers, keywordToken.Span);

                    if (keywordToken.IsKeyword(DkxConst.Keywords.Get))
                    {
                        if (propertyScope._getter != null) throw new CodeException(keywordToken.Span, ErrorCode.DuplicatePropertyGetter);
                        else propertyScope._getter = PropertyAccessorScope.Parse(
                            property: propertyScope,
                            accessorType: PropertyAccessorType.Getter,
                            privacy: modifiers.Privacy ?? propertyScope._privacy,
                            bodyTokens: phase == CompilePhase.FullCompilation ? bodyToken.Tokens : null);
                    }
                    else
                    {
                        if (propertyScope._setter != null) throw new CodeException(keywordToken.Span, ErrorCode.DuplicatePropertySetter);
                        else propertyScope._setter = PropertyAccessorScope.Parse(
                            property: propertyScope,
                            accessorType: PropertyAccessorType.Setter,
                            privacy: modifiers.Privacy ?? propertyScope._privacy,
                            bodyTokens: phase == CompilePhase.FullCompilation ? bodyToken.Tokens : null);
                    }
                }
            }

            if (propertyScope._getter == null) throw new CodeException(nameSpan, ErrorCode.PropertyHasNoGetter);
            if (propertyScope._setter == null) propertyScope._flags |= ModifierFlags.ReadOnly;

            propertyScope.ReportUnusedTokens(tokens, used);
        }

        internal void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
        {
            if (_fileTarget != context.FileTarget) return;
            if (_getter == null) throw new InvalidOperationException("Property has no getter.");

            _getter.GenerateWbdkCode(context, cw);
            _setter?.GenerateWbdkCode(context, cw);
        }

        public override string ToString() => $"PropertyScope: {_dataType} {_name}";

        class PropertyAccessorScope : Scope, IReturnScope, IVariableScope, IVariableWbdkScope
        {
            private PropertyScope _property;
            private PropertyAccessorType _accessorType;
            private Privacy _privacy;
            private Statement[] _statements;
            private VariableStore _variableStore;
            private List<Variable> _wbdkVariables = new List<Variable>();

            private PropertyAccessorScope(PropertyScope property, PropertyAccessorType accessorType, Privacy privacy)
                : base(property, property.Phase, property.Resolver, property.Project)
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
                        accessMethod: FieldAccessMethod.Variable,
                        flags: default,
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
                DkxTokenCollection bodyTokens)
            {
                var accessorScope = new PropertyAccessorScope(property, accessorType, privacy);

                if (bodyTokens != null)
                {
                    accessorScope._statements = StatementParser.SplitTokensIntoStatements(accessorScope, bodyTokens);
                }

                return accessorScope;
            }

            internal void GenerateWbdkCode(CodeGenerationContext context, CodeWriter cw)
            {
                if (_accessorType == PropertyAccessorType.Getter)
                {
                    cw.Write(_property.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(DkxConst.Properties.GetterPrefix);
                    cw.Write(_property.Name);
                    cw.Write('(');
                    if (!_property.Flags.HasFlag(ModifierFlags.Static)) cw.Write("unsigned int this");
                    cw.Write(')');
                }
                else
                {
                    cw.Write(DkxConst.Keywords.Void);
                    cw.Write(' ');
                    cw.Write(DkxConst.Properties.SetterPrefix);
                    cw.Write(_property.Name);
                    cw.Write('(');
                    if (!_property.Flags.HasFlag(ModifierFlags.Static)) cw.Write("unsigned int this, ");
                    cw.Write(_property.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(DkxConst.Properties.SetterArgumentName);
                    cw.Write(')');
                }

                cw.WriteLine();
                using (cw.Indent())
                {
                    var flow = new FlowTrace();
                    var propertyContext = new CodeGenerationContext(context, _property);

                    // Variables
                    foreach (var variable in _wbdkVariables)
                    {
                        cw.Write(variable.DataType.ToWbdkCode());
                        cw.Write(' ');
                        cw.Write(variable.WbdkName);
                        cw.Write(';');
                        cw.WriteLine();
                    }

                    // For a setter, set the 'value' argument as initialized
                    if (_accessorType == PropertyAccessorType.Setter)
                    {
                        flow.OnVariableAssigned(DkxConst.Properties.SetterArgumentName);
                    }

                    // Statements
                    if (_statements != null)
                    {
                        foreach (var statement in _statements)
                        {
                            statement.GenerateWbdkCode(propertyContext, cw, flow);
                        }
                    }

                    GenerateScopeEnding(propertyContext, cw, flow, methodEnding: true, _property._nameSpan);
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

            public override string ToString() => $"{(_accessorType == PropertyAccessorType.Getter ? "Get" : "Set")} {_property}";
        }

        enum PropertyAccessorType
        {
            Getter,
            Setter
        }
    }
}
