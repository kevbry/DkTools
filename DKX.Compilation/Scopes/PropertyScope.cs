using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Resolving;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Scopes
{
    public class PropertyScope : Scope, IField, IObjectReferenceScope
    {
        private string _className;
        private string _name;
        private CodeSpan _nameSpan;
        private DataType _dataType;
        private bool _static;
        private Privacy _privacy;
        private FileContext _fileContext;
        private PropertyAccessorScope _getter;
        private PropertyAccessorScope _setter;

        private PropertyScope(
            Scope parent,
            string className,
            string name,
            CodeSpan nameSpan,
            DataType dataType,
            Modifiers modifiers)
            : base(parent)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _dataType = dataType;

            _static = modifiers.Static;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;
        }

        public string ClassName => _className;
        public DataType DataType => _dataType;
        public bool IsConstant => false;
        public string Name => _name;
        public bool ReadOnly => _setter == null;
        public Privacy ReadPrivacy => _getter?.Privacy ?? Privacy.Private;
        public DataType ScopeDataType => Parent.GetScope<IObjectReferenceScope>().ScopeDataType;
        public bool ScopeStatic => _static;
        public bool Static => _static;
        public Privacy WritePrivacy => _setter?.Privacy ?? Privacy.Private;

        public static async Task<PropertyScope> ParseAsync(
            Scope parent,
            string className,
            string name,
            CodeSpan nameSpan,
            DataType dataType,
            Modifiers modifiers,
            DkxTokenCollection bodyTokens,
            ProcessingDepth depth,
            IResolver resolver)
        {
            var propertyScope = new PropertyScope(parent, className, name, nameSpan, dataType, modifiers);

            await ParseAccessorsAsync(propertyScope, bodyTokens, depth, resolver, nameSpan);

            return propertyScope;
        }

        private static async Task ParseAccessorsAsync(PropertyScope propertyScope, DkxTokenCollection tokens, ProcessingDepth depth, IResolver resolver, CodeSpan nameSpan)
        {
            if (!tokens.Any()) await propertyScope.ReportAsync(nameSpan, ErrorCode.PropertyHasNoGetterOrSetter);

            var used = new TokenUseTracker();

            foreach (var index in tokens.FindIndices(t => t.IsKeyword(DkxConst.Keywords.Get) || t.IsKeyword(DkxConst.Keywords.Set)))
            {
                var keywordToken = tokens[index];
                used.Use(keywordToken);

                if (tokens[index + 1].IsScope)
                {
                    var bodyToken = tokens[index + 1];
                    used.Use(bodyToken);

                    var modifiers = await Modifiers.ReadModifiersAsync(tokens, index, used, propertyScope);
                    await modifiers.CheckForPropertyAccessorAsync(propertyScope, modifiers);

                    if (keywordToken.IsKeyword(DkxConst.Keywords.Get))
                    {
                        if (propertyScope._getter != null) await propertyScope.ReportAsync(keywordToken.Span, ErrorCode.DuplicatePropertyGetter);
                        else propertyScope._getter = await PropertyAccessorScope.ParseAsync(
                            property: propertyScope,
                            accessorType: PropertyAccessorType.Getter,
                            privacy: modifiers.Privacy ?? Privacy.Private,
                            bodyTokens: depth == ProcessingDepth.Full ? bodyToken.Tokens : null,
                            resolver: resolver);
                    }
                    else
                    {
                        if (propertyScope._setter != null) await propertyScope.ReportAsync(keywordToken.Span, ErrorCode.DuplicatePropertySetter);
                        else propertyScope._setter = await PropertyAccessorScope.ParseAsync(
                            property: propertyScope,
                            accessorType: PropertyAccessorType.Setter,
                            privacy: modifiers.Privacy ?? Privacy.Private,
                            bodyTokens: depth == ProcessingDepth.Full ? bodyToken.Tokens : null,
                            resolver: resolver);
                    }
                }
            }

            await propertyScope.ReportUnusedTokensAsync(tokens, used);
        }

        internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
        {
            if (_getter == null) throw new InvalidOperationException("Property has no getter.");

            await _getter.GenerateWbdkCodeAsync(cw);
            await (_setter?.GenerateWbdkCodeAsync(cw) ?? Task.CompletedTask);
        }

        public async Task<CodeFragment> ToWbdkCode_ReadAsync(CodeFragment parentFragment, CodeSpan fieldSpan, ISourceCodeReporter report)
        {
            if (_getter == null) throw new InvalidOperationException("Property has no getter.");

            return await _getter.ToWbdkCode_ReadAsync(parentFragment, fieldSpan, report);
        }

        public async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment parentFragment, CodeSpan fieldSpan, CodeFragment valueFragment, ISourceCodeReporter report)
        {
            if (_setter == null) throw new CodeException(fieldSpan, ErrorCode.PropertyIsReadOnly, _name);

            return await _setter.ToWbdkCode_WriteAsync(parentFragment, fieldSpan, valueFragment, report);
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
                        name: DkxConst.Properties.SetterArgumentName,
                        wbdkName: DkxConst.Properties.SetterArgumentName,
                        dataType: _property.DataType,
                        fileContext: FileContext.NeutralClass,
                        passType: ArgumentPassType.ByValue,
                        static_: false,
                        local: true,
                        privacy: Privacy.Public,
                        initializer: null));
                }
            }

            public Privacy Privacy => _privacy;
            public DataType ReturnDataType => _property.DataType;
            public IVariableStore VariableStore => _variableStore;

            public static async Task<PropertyAccessorScope> ParseAsync(
                PropertyScope property,
                PropertyAccessorType accessorType,
                Privacy privacy,
                DkxTokenCollection bodyTokens,
                IResolver resolver)
            {
                var accessorScope = new PropertyAccessorScope(property, accessorType, privacy, bodyTokens, resolver);

                if (bodyTokens != null)
                {
                    accessorScope._statements = await StatementParser.SplitTokensIntoStatementsAsync(accessorScope, bodyTokens, resolver);
                }

                return accessorScope;
            }

            internal override async Task GenerateWbdkCodeAsync(CodeWriter cw)
            {
                if (_accessorType == PropertyAccessorType.Getter)
                {
                    cw.Write(_property.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(_property.ClassName);
                    cw.Write('_');
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
                    cw.Write(_property.ClassName);
                    cw.Write('_');
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
                            await statement.GenerateWbdkCodeAsync(cw);
                        }
                    }
                }
            }

            public Task<CodeFragment> ToWbdkCode_ReadAsync(CodeFragment parentFragment, CodeSpan fieldSpan, ISourceCodeReporter report)
            {
                var sb = new StringBuilder();

                var cls = GetScope<ClassScope>();
                var nameParts = cls.FullClassNameParts.ToArray();
                if (nameParts.Length < 2) throw new InvalidOperationException("Class did not return enough name parts.");
                for (int i = 0, ii = nameParts.Length - 1; i <= ii; i++)
                {
                    sb.Append(nameParts[i]);
                    if (i == 0) sb.Append('.');
                    else sb.Append('_');
                }
                sb.Append(DkxConst.Properties.GetterPrefix);
                sb.Append(_property.Name);
                sb.Append('(');
                if (!_property.Static)
                {
                    // Pass the 'this' pointer.
                    sb.Append(parentFragment);
                }
                sb.Append(')');

                return Task.FromResult(new CodeFragment(sb.ToString(), _property.DataType, Expressions.OpPrec.None, fieldSpan, readOnly: true));
            }

            public async Task<CodeFragment> ToWbdkCode_WriteAsync(CodeFragment parentFragment, CodeSpan fieldSpan, CodeFragment valueFragment, ISourceCodeReporter report)
            {
                await Conversions.ConversionValidator.CheckConversionAsync(_property.DataType, valueFragment, report);

                var sb = new StringBuilder();

                var cls = GetScope<ClassScope>();
                var nameParts = cls.FullClassNameParts.ToArray();
                if (nameParts.Length < 2) throw new InvalidOperationException("Class did not return enough name parts.");
                for (int i = 0, ii = nameParts.Length - 1; i <= ii; i++)
                {
                    sb.Append(nameParts[i]);
                    if (i == 0) sb.Append('.');
                    else sb.Append('_');
                }
                sb.Append(DkxConst.Properties.SetterPrefix);
                sb.Append(_property.Name);
                sb.Append('(');
                if (!_property.Static)
                {
                    // Pass the 'this' pointer.
                    sb.Append(parentFragment);
                    sb.Append(", ");
                }
                sb.Append(valueFragment);
                sb.Append(')');

                return new CodeFragment(sb.ToString(), _property.DataType, Expressions.OpPrec.None, fieldSpan, readOnly: true);
            }
        }

        enum PropertyAccessorType
        {
            Getter,
            Setter
        }
    }
}
