using DK;
using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    class FileNode : Node, IVariableScopeNode
    {
        private string _pathName;
        private List<ReportItem> _reportItems = new List<ReportItem>();
        private string _className;
        private VariableStore _variableStore;

        public FileNode(DkAppContext app, string pathName, CodeParser code)
            : base(app, code)
        {
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
            _className = PathUtil.GetFileNameWithoutExtension(_pathName);
            _variableStore = new VariableStore(parent: null);
        }

        public string ClassName => _className;
        public override bool HasErrors => _reportItems.Any(i => i.Severity == ErrorSeverity.Error);
        public IEnumerable<MethodNode> Methods => ChildNodes.Where(n => n is MethodNode).Cast<MethodNode>();
        public override string PathName => _pathName;
        public IEnumerable<ReportItem> ReportItems => _reportItems;
        public IVariableStore VariableStore => _variableStore;

        public void Parse()
        {
            var className = null as string;

            while (true)
            {
                var modifiers = Modifiers.ReadModifiers(Code, this);

                if (className == null && Code.ReadExactWholeWord("class"))
                {
                    modifiers.CheckForClass(this, Code.Span);

                    if (!Code.ReadWord())
                    {
                        ReportItem(Code.Position, ErrorCode.ExpectedClassName);
                    }
                    else
                    {
                        className = Code.Text;
                        var fileName = PathUtil.GetFileNameWithoutExtension(_pathName);
                        if (!className.EqualsI(fileName))
                        {
                            ReportItem(Code.Position, ErrorCode.ClassNameDoesNotMatchFileName);
                        }
                        else
                        {
                            _className = className;
                        }
                    }

                    if (!Code.ReadExact('{'))
                    {
                        ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                        continue;
                    }

                    InsideClass(modifiers);
                }
                else if (Code.Read())
                {
                    ReportItem(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
                }
                else break;
            }
        }

        private void InsideClass(Modifiers classModifiers)
        {
            while (true)
            {
                if (Code.ReadExact('}')) break; // End of class

                var modifiers = Modifiers.ReadModifiers(Code, this);

                if (DataType.TryParse(Code, out var dataType)) AfterDataType(dataType, modifiers);
                else if (Code.Read()) ReportItem(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
                else break;
            }
        }

        private void AfterDataType(DataType dataType, Modifiers modifiers)
        {
            if (Code.ReadWord())
            {
                var name = Code.Text;
                var nameSpan = Code.Span;

                if (Code.ReadExact('('))
                {
                    // This is a method declaration
                    var methodPrivacy = modifiers.Privacy ?? Privacy.Public;
                    var methodFileContext = modifiers.FileContext ?? FileContext.NeutralClass;
                    modifiers.CheckForMethod(this);

                    // Read the arguments
                    var args = new List<Variable>();
                    if (!Code.ReadExact(')'))
                    {
                        var unnamedIndex = 0;
                        while (true)
                        {
                            var argType = ArgumentPassType.ByValue;
                            if (Code.ReadExactWholeWord("ref")) argType = ArgumentPassType.ByReference;
                            else if (Code.ReadExactWholeWord("out")) argType = ArgumentPassType.Out;

                            if (!DataType.TryParse(Code, out var argDataType))
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedArgumentDataType);
                                argDataType = DataType.Unsupported;
                            }

                            string argName;
                            if (!Code.ReadWord())
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedArgumentName);
                                argName = $"unnamed{++unnamedIndex}";
                            }
                            else argName = Code.Text;

                            args.Add(new Variable(
                                name: argName,
                                wbdkName: argName,
                                dataType: argDataType,
                                fileContext: methodFileContext,
                                passType: argType,
                                initializer: null));

                            if (Code.ReadExact(')')) break;
                            if (Code.ReadExact(',')) continue;

                            ReportItem(Code.Position, ErrorCode.ExpectedToken, ',');
                            Code.SkipToAfterExit();
                            break;
                        }
                    }

                    if (!Code.ReadExact('{'))
                    {
                        ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                    }

                    var bodyStartPos = Code.Span.End;

                    var method = new MethodNode(
                        parent: this,
                        name: name,
                        returnDataType: dataType,
                        args: args,
                        privacy: methodPrivacy,
                        fileContext: methodFileContext,
                        bodySpan: new CodeSpan(bodyStartPos, bodyStartPos));

                    var bodyContext = new NodeBodyContext(method);

                    method.ReadCodeBody(bodyContext, bodyStartPos);
                }
                else if (Code.ReadExact('{'))
                {
                    // This is a property
                    modifiers.CheckForProperty(this);

                    if (DkxConst.Keywords.AllKeywords.Contains(name))
                    {
                        ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                    }
                    else if (_variableStore.HasVariable(name, includeParents: true) || HasConstant(name) || HasProperty(name))
                    {
                        ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                    }

                    var prop = new PropertyNode(this, name, dataType);
                    var gotGetter = false;
                    var gotSetter = false;
                    while (true)
                    {
                        if (Code.ReadExact('}')) break;
                        
                        var accessorModifiers = Modifiers.ReadModifiers(Code, this);
                        accessorModifiers.CheckForPropertyAccessor(this, modifiers);
                        var accessorPrivacy = accessorModifiers.Privacy ?? modifiers.Privacy ?? Privacy.Public;
                        var accessorFileContext = accessorModifiers.FileContext ?? FileContext.NeutralClass;

                        if (!gotGetter && Code.ReadExactWholeWord("get"))
                        {
                            gotGetter = true;
                            if (Code.ReadExact('{'))
                            {
                                var getter = new PropertyAccessorNode(
                                    property: prop,
                                    accessorType: PropertyAccessorType.Getter,
                                    privacy: accessorPrivacy,
                                    fileContext: accessorFileContext,
                                    bodyStartPos: Code.Span.Start);

                                var bodyContext = new NodeBodyContext(getter);

                                getter.ReadCodeBody(bodyContext, Code.Span.End);
                            }
                            else
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                                break;
                            }
                        }
                        else if (!gotSetter && Code.ReadExactWholeWord("set"))
                        {
                            gotSetter = true;
                            if (Code.ReadExact('{'))
                            {
                                var setter = new PropertyAccessorNode(
                                    property: prop,
                                    accessorType: PropertyAccessorType.Setter,
                                    privacy: accessorPrivacy,
                                    fileContext: accessorFileContext,
                                    bodyStartPos: Code.Span.Start);

                                var bodyContext = new NodeBodyContext(setter);

                                setter.ReadCodeBody(bodyContext, Code.Span.End);
                            }
                            else
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedToken, '{');
                                break;
                            }
                        }
                        else if (Code.Read())
                        {
                            ReportItem(Code.Position, ErrorCode.UnexpectedToken, Code.Text);
                            Code.SkipToAfterExit();
                            break;
                        }
                        else
                        {
                            if (modifiers.IsEmpty) break;
                            ReportItem(Code.Position, ErrorCode.ExpectedToken, "get");
                        }
                    }

                    if (!gotGetter && !gotSetter)
                    {
                        ReportItem(nameSpan, ErrorCode.PropertyHasNoGetterOrSetter);
                    }
                    else if (!gotGetter)
                    {
                        ReportItem(nameSpan, ErrorCode.PropertyHasNoGetter);
                    }
                }
                else
                {
                    // This is a member variable or constant
                    if (modifiers.Const)
                    {
                        // This is a constant
                        modifiers.CheckForConstant(this);
                        while (true)
                        {
                            Chain initializer = null;
                            if (Code.ReadExact('='))
                            {
                                var eqSpan = Code.Span;
                                var bodyContext = new NodeBodyContext(this);
                                initializer = ExpressionParser.ReadExpressionOrNull(bodyContext);
                                if (initializer == null)
                                {
                                    ReportItem(eqSpan, ErrorCode.ExpectedExpression);
                                }
                                else
                                {
                                    initializer.Report(this);
                                }
                            }
                            else
                            {
                                ReportItem(nameSpan, ErrorCode.ConstantsMustHaveInitializer);
                            }

                            if (DkxConst.Keywords.AllKeywords.Contains(name))
                            {
                                ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                            }
                            else if (_variableStore.HasVariable(name, includeParents: true) || HasConstant(name) || HasProperty(name))
                            {
                                ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                            }
                            else
                            {
                                AddConstant(new Constant(name, dataType, initializer));
                            }

                            if (Code.ReadExact(';')) break;

                            if (!Code.ReadExact(','))
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedToken, ',');
                                break;
                            }

                            if (!Code.ReadWord())
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedVariableName);
                                break;
                            }
                            name = Code.Text;
                        }
                    }
                    else
                    {
                        // This is a member variable
                        modifiers.CheckForMemberVariable(this);

                        while (true)
                        {
                            Chain initializer = null;
                            if (Code.ReadExact('='))
                            {
                                var eqSpan = Code.Span;
                                var bodyContext = new NodeBodyContext(this);
                                initializer = ExpressionParser.ReadExpressionOrNull(bodyContext);
                                if (initializer == null)
                                {
                                    ReportItem(eqSpan, ErrorCode.ExpectedExpression);
                                }
                                else
                                {
                                    initializer.Report(this);
                                }
                            }

                            if (DkxConst.Keywords.AllKeywords.Contains(name))
                            {
                                ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                            }
                            else if (_variableStore.HasVariable(name, includeParents: true) || HasConstant(name) || HasProperty(name))
                            {
                                ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                            }
                            else
                            {
                                _variableStore.AddVariable(new Variable(
                                    name: name,
                                    wbdkName: name, // TODO: may need to decorate with the class name
                                    dataType: dataType,
                                    fileContext: modifiers.FileContext ?? FileContext.NeutralClass,
                                    passType: null, // Only arguments use passType
                                    initializer: initializer));
                            }

                            if (Code.ReadExact(';')) break;

                            if (!Code.ReadExact(','))
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedToken, ',');
                                break;
                            }

                            if (!Code.ReadWord())
                            {
                                ReportItem(Code.Position, ErrorCode.ExpectedVariableName);
                                break;
                            }
                            name = Code.Text;
                        }
                    }
                }
            }
            else if (Code.Read()) ReportItem(Code.Span, ErrorCode.ExpectedMethodName, Code.Text);
        }

        protected override void OnReportItem(ReportItem error)
        {
            _reportItems.Add(error);
        }

        #region Constants
        private Dictionary<string, Constant> _constants = new Dictionary<string, Constant>();

        public override bool HasConstant(string name) => _constants.ContainsKey(name);

        internal override Constant GetConstant(string name) => _constants.TryGetValue(name, out var constant) ? constant : default;

        public void AddConstant(Constant constant)
        {
            if (_constants.ContainsKey(constant.Name)) throw new ArgumentException("Duplicate constant name.");
            _constants[constant.Name] = constant;
        }

        public IEnumerable<Constant> Constants => _constants.Values;
        #endregion

        #region Properties
        public override bool HasProperty(string name)
        {
            foreach (var node in ChildNodes)
            {
                if (node is PropertyNode prop)
                {
                    if (prop.Name == name) return true;
                }
            }

            return false;
        }

        public IEnumerable<PropertyNode> Properties => ChildNodes.Where(n => n is PropertyNode).Cast<PropertyNode>();
        #endregion

        #region Typedefs
        private Dictionary<string, DataType?> _typedefs = new Dictionary<string, DataType?>();

        protected override DataType? GetTypedefDataType(string typedefName)
        {
            if (_typedefs.TryGetValue(typedefName, out var dataType)) return dataType;

            var typedef = App.Settings.Dict.GetTypedef(typedefName);
            if (typedef != null)
            {
                dataType = DkDataTypeParser.Parse(new CodeParser(typedef.DataType.Source.ToString()));
                _typedefs[typedefName] = dataType;
                return dataType;
            }

            _typedefs[typedefName] = null;
            return null;
        }
        #endregion
    }
}
