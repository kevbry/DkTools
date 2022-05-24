using DK.Code;
using DKX.Compilation.CodeGeneration;
using DKX.Compilation.Expressions;
using DKX.Compilation.Tokens;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Scopes
{
    public class ClassScope : Scope, IVariableScope
    {
        private string _name;
        private Modifiers _modifiers;
        private List<MethodScope> _methods = new List<MethodScope>();
        private List<PropertyScope> _properties = new List<PropertyScope>();
        private List<Constant> _constants = new List<Constant>();
        private VariableStore _variableStore;

        public ClassScope(Scope parent, string className, Modifiers modifiers)
            : base(parent)
        {
            _name = className ?? throw new ArgumentNullException(nameof(className));
            _modifiers = modifiers;
            _variableStore = new VariableStore(parent?.GetScope<IVariableScope>());
        }

        public IEnumerable<MethodScope> Methods => _methods;
        public string Name => _name;
        public IVariableStore VariableStore => _variableStore;

        public void ProcessTokens(DkxTokenCollection tokens, ProcessingDepth depth)
        {
            var used = new TokenUseTracker();

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

                        var modifiers = Modifiers.ReadModifiers(tokens, pos, used, this);
                        modifiers.CheckForMethod(this);

                        var method = new MethodScope(this, _name, nameToken.Text, nameToken.Span, dataTypeToken.DataType, argsToken.Tokens, modifiers,
                            depth == ProcessingDepth.Full ? scopeToken.Tokens : null);
                        _methods.Add(method);
                        pos += 4;
                    }
                    else
                    {
                        ReportItem(argsToken.Span, ErrorCode.ExpectedToken, '{');
                        pos += 3;
                    }
                }
                else if (tokens[pos + 2].IsScope)
                {
                    // This is a property
                    var scopeToken = tokens[pos + 2];
                    used.Use(dataTypeToken, nameToken, scopeToken);

                    var modifiers = Modifiers.ReadModifiers(tokens, pos, used, this);
                    modifiers.CheckForProperty(this);

                    var property = new PropertyScope(this, _name, nameToken.Text, nameToken.Span, dataTypeToken.DataType, modifiers, scopeToken.Tokens, depth);
                    _properties.Add(property);
                    pos += 3;
                }
                else
                {
                    // This is a constant or field
                    used.Use(dataTypeToken, nameToken);
                    var modifiers = Modifiers.ReadModifiers(tokens, pos, used, this);

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
                            var initializerChain = ExpressionParser.ReadExpressionOrNull(this, initializerStream);
                            if (initializerChain == null) ReportItem(initializerTokens.Span, ErrorCode.ExpectedExpression);
                            else if (!initializerStream.EndOfStream) ReportItem(initializerStream.Read().Span, ErrorCode.SyntaxError);

                            if (modifiers.Const)
                            {
                                var constant = new Constant(nameToken.Text, dataTypeToken.DataType, initializerChain);
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
                                    initializer: initializerChain);

                                _variableStore.AddVariable(variable);
                            }

                            pos = end + 1;
                        }
                        else
                        {
                            ReportItem(assignToken.Span, ErrorCode.ExpectedExpression);
                            pos += 2;
                        }
                    }
                    else // No '=' after name
                    {
                        if (modifiers.Const)
                        {
                            ReportItem(nameToken.Span, ErrorCode.ConstantsMustHaveInitializer);
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
                                    initializer: null);

                                _variableStore.AddVariable(variable);
                                pos += 3;
                            }
                            else
                            {
                                ReportItem(nameToken.Span, ErrorCode.ExpectedToken, ';');
                                pos += 2;
                            }
                        }
                    }
                }
            }

            foreach (var badToken in tokens.GetUnused(used)) ReportItem(badToken.Span, ErrorCode.SyntaxError);
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

        internal override void GenerateWbdkCode(CodeWriter cw)
        {
            var gotField = false;
            foreach (var field in _variableStore.GetVariables(includeParents: false))
            {
                gotField = true;
                cw.Write(field.DataType.ToWbdkCode());
                cw.Write(' ');
                cw.Write(field.WbdkName);
                cw.Write(';');
                cw.WriteLine();
            }
            if (gotField) cw.WriteLine();

            foreach (var property in _properties)
            {
                property.GenerateWbdkCode(cw);
                cw.WriteLine();
            }

            foreach (var method in _methods)
            {
                method.GenerateWbdkCode(cw);
                cw.WriteLine();
            }
        }
    }
}
