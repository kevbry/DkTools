using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Scopes.Statements;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Linq;

namespace DKX.Compilation.Scopes
{
    public class PropertyScope : Scope
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

        public PropertyScope(Scope parent, string className, string name, CodeSpan nameSpan, DataType dataType, Modifiers modifiers, DkxTokenCollection bodyTokens, ProcessingDepth depth)
            : base(parent)
        {
            _className = className ?? throw new ArgumentNullException(nameof(className));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _nameSpan = nameSpan;
            _dataType = dataType;

            _static = modifiers.Static;
            _privacy = modifiers.Privacy ?? Privacy.Public;
            _fileContext = modifiers.FileContext ?? FileContext.NeutralClass;

            ParseAccessors(bodyTokens, depth);
        }

        public string ClassName => _className;
        public DataType DataType => _dataType;
        public string Name => _name;

        private void ParseAccessors(DkxTokenCollection tokens, ProcessingDepth depth)
        {
            if (!tokens.Any()) ReportItem(_nameSpan, ErrorCode.PropertyHasNoGetterOrSetter);

            var used = new TokenUseTracker();

            foreach (var index in tokens.FindIndices(t => t.IsKeyword(DkxConst.Keywords.Get) || t.IsKeyword(DkxConst.Keywords.Set)))
            {
                var keywordToken = tokens[index];
                used.Use(keywordToken);

                if (tokens[index + 1].IsScope)
                {
                    var bodyToken = tokens[index + 1];
                    used.Use(bodyToken);

                    var modifiers = Modifiers.ReadModifiers(tokens, index, used, this);
                    modifiers.CheckForPropertyAccessor(this, modifiers);

                    if (keywordToken.IsKeyword(DkxConst.Keywords.Get))
                    {
                        if (_getter != null) ReportItem(keywordToken.Span, ErrorCode.DuplicatePropertyGetter);
                        else _getter = new PropertyAccessorScope(this, PropertyAccessorType.Getter, depth == ProcessingDepth.Full ? bodyToken.Tokens : null);
                    }
                    else
                    {
                        if (_setter != null) ReportItem(keywordToken.Span, ErrorCode.DuplicatePropertySetter);
                        else _setter = new PropertyAccessorScope(this, PropertyAccessorType.Setter, depth == ProcessingDepth.Full ? bodyToken.Tokens : null);
                    }
                }
            }

            ReportUnusedTokens(tokens, used);
        }

        internal override void GenerateWbdkCode(CodeWriter cw)
        {
            _getter?.GenerateWbdkCode(cw);
            _setter?.GenerateWbdkCode(cw);
        }

        class PropertyAccessorScope : Scope, IReturnScope, IVariableScope
        {
            private PropertyScope _property;
            private PropertyAccessorType _accessorType;
            private Statement[] _statements;
            private VariableStore _variableStore;

            public PropertyAccessorScope(PropertyScope property, PropertyAccessorType accessorType, DkxTokenCollection bodyTokens)
                : base(property)
            {
                _property = property ?? throw new ArgumentNullException(nameof(property));
                _accessorType = accessorType;
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
                        initializer: null));
                }

                if (bodyTokens != null) _statements = StatementParser.SplitTokensIntoStatements(this, bodyTokens);
            }

            public DataType ReturnDataType => _property.DataType;
            public IVariableStore VariableStore => _variableStore;

            internal override void GenerateWbdkCode(CodeWriter cw)
            {
                if (_accessorType == PropertyAccessorType.Getter)
                {
                    cw.Write(_property.DataType.ToWbdkCode());
                    cw.Write(' ');
                    cw.Write(_property.ClassName);
                    cw.Write('_');
                    cw.Write(DkxConst.Properties.GetterPrefix);
                    cw.Write(_property.Name);
                    cw.Write("()");
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
                            statement.GenerateWbdkCode(cw);
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
