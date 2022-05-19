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
    class FileNode : Node
    {
        private string _pathName;
        private List<ReportItem> _reportItems = new List<ReportItem>();
        private string _className;

        public FileNode(DkAppContext app, string pathName, CodeParser code)
            : base(app, code)
        {
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
            _className = PathUtil.GetFileNameWithoutExtension(_pathName);
        }

        public string ClassName => _className;
        public override bool HasErrors => _reportItems.Any(i => i.Severity == ErrorSeverity.Error);
        public IEnumerable<MethodNode> Methods => ChildNodes.Where(n => n is MethodNode).Cast<MethodNode>();
        public IEnumerable<ReportItem> ReportItems => _reportItems;

        public override string PathName => _pathName;

        public void Parse()
        {
            var className = null as string;

            while (true)
            {
                var modifiers = ReadModifiers();

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
            DataType? dataType;

            while (true)
            {
                if (Code.ReadExact('}')) break; // End of class

                var modifiers = ReadModifiers();

                if ((dataType = DataType.Parse(Code, out _)) != null) AfterDataType(dataType.Value, modifiers);
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

                            var argDataType = DataType.Parse(Code, out _);
                            if (argDataType == null)
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
                                dataType: argDataType.Value,
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

                    if (DkxConst.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                    else if (HasVariable(name) || HasConstant(name) || HasProperty(name)) ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);

                    var prop = new PropertyNode(this, name, dataType);
                    var gotGetter = false;
                    var gotSetter = false;
                    while (true)
                    {
                        if (Code.ReadExact('}')) break;
                        
                        var accessorModifiers = ReadModifiers();
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

                            if (DkxConst.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                            else if (HasVariable(name) || HasConstant(name) || HasProperty(name)) ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                            else AddConstant(new Constant(name, dataType, initializer));

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

                            if (DkxConst.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                            else if (HasVariable(name) || HasConstant(name) || HasProperty(name)) ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                            else AddVariable(new Variable(
                                name: name,
                                wbdkName: name, // TODO: may need to decorate with the class name
                                dataType: dataType,
                                fileContext: modifiers.FileContext ?? FileContext.NeutralClass,
                                passType: null, // Only arguments use passType
                                initializer: initializer));

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

        private Modifiers ReadModifiers()
        {
            var privacy = null as Privacy?;
            var privacySpan = CodeSpan.Empty;
            var fileContext = null as FileContext?;
            var fileContextSpan = CodeSpan.Empty;
            var const_ = false;
            var constSpan = CodeSpan.Empty;
            var static_ = false;
            var staticSpan = CodeSpan.Empty;

            while (!Code.EndOfFile)
            {
                switch (Code.PeekWordR())
                {
                    case "public":
                        var wordSpan = Code.MovePeekedSpan();
                        if (privacy != null) ReportItem(wordSpan, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Privacy.Public;
                        privacySpan = wordSpan;
                        continue;
                    case "protected":
                        wordSpan = Code.MovePeekedSpan();
                        if (privacy != null) ReportItem(wordSpan, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Privacy.Protected;
                        privacySpan = wordSpan;
                        continue;
                    case "private":
                        wordSpan = Code.MovePeekedSpan();
                        if (privacy != null) ReportItem(wordSpan, ErrorCode.DuplicatePrivacyModifier);
                        privacy = Privacy.Private;
                        privacySpan = wordSpan;
                        continue;
                    case "neutral":
                        wordSpan = Code.MovePeekedSpan();
                        if (fileContext != null) ReportItem(wordSpan, ErrorCode.DuplicateFileContextModifier);
                        fileContext = FileContext.NeutralClass;
                        fileContextSpan = wordSpan;
                        continue;
                    case "client":
                        wordSpan = Code.MovePeekedSpan();
                        if (fileContext != null) ReportItem(wordSpan, ErrorCode.DuplicateFileContextModifier);
                        if (Code.ReadExactWholeWord("trigger"))
                        {
                            fileContext = FileContext.ClientTrigger;
                            fileContextSpan = wordSpan.Envelope(Code.Span);
                        }
                        else if (Code.ReadExactWholeWord("class"))
                        {
                            fileContext = FileContext.ClientClass;
                            fileContextSpan = wordSpan.Envelope(Code.Span);
                        }
                        else if (Code.ReadExactWholeWord("program"))
                        {
                            fileContext = FileContext.GatewayProgram;
                            fileContextSpan = wordSpan.Envelope(Code.Span);
                        }
                        else
                        {
                            fileContext = FileContext.ClientClass;
                            fileContextSpan = wordSpan;
                        }
                        continue;
                    case "server":
                        wordSpan = Code.MovePeekedSpan();
                        if (fileContext != null) ReportItem(wordSpan, ErrorCode.DuplicateFileContextModifier);
                        if (Code.ReadExactWholeWord("trigger"))
                        {
                            fileContext = FileContext.ServerTrigger;
                            fileContextSpan = wordSpan.Envelope(Code.Span);
                        }
                        else if (Code.ReadExactWholeWord("class"))
                        {
                            fileContext = FileContext.ServerClass;
                            fileContextSpan = wordSpan.Envelope(Code.Span);
                        }
                        else if (Code.ReadExactWholeWord("program"))
                        {
                            fileContext = FileContext.ServerProgram;
                            fileContextSpan = wordSpan.Envelope(Code.Span);
                        }
                        else
                        {
                            fileContext = FileContext.ServerClass;
                            fileContextSpan = wordSpan;
                        }
                        continue;
                    case "const":
                        wordSpan = Code.MovePeekedSpan();
                        if (const_) ReportItem(wordSpan, ErrorCode.DuplicateConstModifier);
                        const_ = true;
                        constSpan = wordSpan;
                        break;
                    case "static":
                        wordSpan = Code.MovePeekedSpan();
                        if (static_) ReportItem(wordSpan, ErrorCode.DuplicateStaticModifier);
                        static_ = true;
                        staticSpan = wordSpan;
                        break;

                }
                break;
            }

            return new Modifiers(privacy, privacySpan, fileContext, fileContextSpan, const_, constSpan, static_, staticSpan);
        }

        private struct Modifiers
        {
            public Privacy? Privacy { get; private set; }
            public CodeSpan PrivacySpan { get; private set; }
            public FileContext? FileContext { get; private set; }
            public CodeSpan FileContextSpan { get; private set; }
            public bool Const { get; private set; }
            public CodeSpan ConstSpan { get; private set; }
            public bool Static { get; private set; }
            public CodeSpan StaticSpan { get; private set; }

            public Modifiers(
                Privacy? privacy, CodeSpan privacySpan,
                FileContext? fileContext, CodeSpan fileContextSpan,
                bool const_, CodeSpan constSpan,
                bool static_, CodeSpan staticSpan)
            {
                Privacy = privacy;
                PrivacySpan = privacySpan;
                FileContext = fileContext;
                FileContextSpan = fileContextSpan;
                Const = const_;
                ConstSpan = constSpan;
                Static = static_;
                StaticSpan = staticSpan;
            }

            public bool IsEmpty => Privacy == null && FileContext == null && Const == false;

            public void CheckForClass(ISourceCodeReporter report, CodeSpan classKeywordSpan)
            {
            }

            public void CheckForMethod(ISourceCodeReporter report)
            {
                if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);
            }

            public void CheckForProperty(ISourceCodeReporter report)
            {
                if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);
            }

            public void CheckForPropertyAccessor(ISourceCodeReporter report, Modifiers propertyModifiers)
            {
                if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);

                if (FileContext != null)
                {
                    switch (FileContext)
                    {
                        case DK.Code.FileContext.ClientClass:
                        case DK.Code.FileContext.NeutralClass:
                        case DK.Code.FileContext.ServerClass:
                            break;
                        default:
                            report.ReportItem(FileContextSpan, ErrorCode.InvalidFileContext);
                            break;
                    }
                }

                if (Privacy != null && propertyModifiers.Privacy != null)
                {
                    if (Privacy.Value < propertyModifiers.Privacy) report.ReportItem(PrivacySpan, ErrorCode.PropertyAccessorMoreAccessibleThanProperty);
                }
            }

            public void CheckForMemberVariable(ISourceCodeReporter report)
            {
                if (Const) report.ReportItem(ConstSpan, ErrorCode.InvalidConst);

                if (Privacy.HasValue && Privacy != Files.Privacy.Private) report.ReportItem(PrivacySpan, ErrorCode.MemberVariableMustBePrivate);
            }

            public void CheckForConstant(ISourceCodeReporter report)
            {
                if (FileContext != null) report.ReportItem(FileContextSpan, ErrorCode.InvalidFileContext);
            }
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
