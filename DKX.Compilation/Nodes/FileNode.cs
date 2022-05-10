using DK;
using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
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
        private Dictionary<string, Constant> _constants = new Dictionary<string, Constant>();

        public FileNode(string pathName, CodeParser code)
            : base(code)
        {
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
            _className = PathUtil.GetFileNameWithoutExtension(_pathName);
        }

        public string ClassName => _className;
        public bool HasErrors => _reportItems.Any(i => i.Severity == ErrorSeverity.Error);
        public IEnumerable<MethodNode> Methods => ChildNodes.Where(n => n is MethodNode).Cast<MethodNode>();
        public IEnumerable<ReportItem> ReportItems => _reportItems;

        public override string PathName => _pathName;

        public void Parse()
        {
            var className = null as string;

            while (true)
            {
                if (className == null && Code.ReadExactWholeWord("class"))
                {
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

                    InsideClass();
                }
                else if (Code.Read())
                {
                    ReportItem(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
                }
                else break;
            }
        }

        private void InsideClass()
        {
            DataType? dataType;

            while (true)
            {
                if (Code.ReadExact('}')) break; // End of class

                if (Code.ReadExactWholeWord("public")) AfterPrivacy(Privacy.Public);
                else if (Code.ReadExactWholeWord("protected")) AfterPrivacy(Privacy.Protected);
                else if (Code.ReadExactWholeWord("private")) AfterPrivacy(Privacy.Private);
                else if (Code.ReadExactWholeWord("const")) AfterConst(Privacy.Private, Code.Span);
                else if ((dataType = DataType.Parse(Code)) != null) AfterDataType(dataType.Value, Privacy.Private, isConst: false, CodeSpan.Empty);
                else if (Code.Read()) ReportItem(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
                else break;
            }
        }

        private void AfterPrivacy(Privacy privacy)
        {
            if (Code.ReadExactWholeWord("const"))
            {
                AfterConst(privacy, Code.Span);
                return;
            }

            var dataType = DataType.Parse(Code);
            if (dataType != null) AfterDataType(dataType.Value, privacy, isConst: false, CodeSpan.Empty);
            else if (Code.Read()) ReportItem(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
        }

        private void AfterConst(Privacy privacy, CodeSpan constSpan)
        {
            var dataType = DataType.Parse(Code);
            if (dataType != null) AfterDataType(dataType.Value, privacy, isConst: true, constSpan);
            else if (Code.Read()) ReportItem(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
        }

        private void AfterDataType(DataType dataType, Privacy privacy, bool isConst, CodeSpan constKeywordSpan)
        {
            if (Code.ReadWord())
            {
                var name = Code.Text;
                var nameSpan = Code.Span;

                if (Code.ReadExact('('))
                {
                    // This is a method declaration
                    if (isConst) ReportItem(constKeywordSpan, ErrorCode.MethodsCannotBeConst);

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

                            var argDataType = DataType.Parse(Code);
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

                            args.Add(new Variable(argName, argDataType.Value, argType, initializer: null));

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

                    var method = new MethodNode(this, name, dataType, args, privacy);
                    ReadCodeBody(method);
                }
                else if (Code.ReadExact('{'))
                {
                    // This is a property
                    if (isConst) ReportItem(constKeywordSpan, ErrorCode.PropertiesCannotBeConst);

                    if (CompileConstants.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                    else if (HasVariable(name) || HasConstant(name) || HasProperty(name)) ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);

                    var prop = new PropertyNode(this, name, dataType, privacy);
                    var gotGetter = false;
                    var gotSetter = false;
                    while (true)
                    {
                        if (Code.ReadExact('}')) break;
                        else if (!gotGetter && Code.ReadExactWholeWord("get"))
                        {
                            gotGetter = true;
                            if (Code.ReadExact('{'))
                            {
                                var getter = new PropertyAccessorNode(prop, PropertyAccessorType.Getter);
                                ReadCodeBody(getter);
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
                                var setter = new PropertyAccessorNode(prop, PropertyAccessorType.Setter);
                                ReadCodeBody(setter);
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
                        else break;
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
                    if (isConst)
                    {
                        // This is a constant
                        while (true)
                        {
                            Chain initializer = null;
                            if (Code.ReadExact('='))
                            {
                                var eqSpan = Code.Span;
                                initializer = ExpressionParser.ReadExpressionOrNull(Code);
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

                            if (CompileConstants.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
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
                        while (true)
                        {
                            if (privacy != Privacy.Private)
                            {
                                ReportItem(nameSpan, ErrorCode.MemberVariableMustBePrivate);
                            }

                            Chain initializer = null;
                            if (Code.ReadExact('='))
                            {
                                var eqSpan = Code.Span;
                                initializer = ExpressionParser.ReadExpressionOrNull(Code);
                                if (initializer == null)
                                {
                                    ReportItem(eqSpan, ErrorCode.ExpectedExpression);
                                }
                                else
                                {
                                    initializer.Report(this);
                                }
                            }

                            if (CompileConstants.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                            else if (HasVariable(name) || HasConstant(name) || HasProperty(name)) ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                            else AddVariable(new Variable(name, dataType, passType: null, initializer));

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

        private void ReadCodeBody(Node parentMethodOrProperty)
        {
            Code.SkipToAfterExit();

            // TODO
        }

        protected override void OnReportItem(ReportItem error)
        {
            _reportItems.Add(error);
        }

        #region Constants
        public bool HasConstant(string name) => _constants.ContainsKey(name);

        public void AddConstant(Constant constant)
        {
            if (_constants.ContainsKey(constant.Name)) throw new ArgumentException("Duplicate constant name.");
            _constants[constant.Name] = constant;
        }

        public IEnumerable<Constant> Constants => _constants.Values;
        #endregion

        #region Properties
        public bool HasProperty(string name)
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
    }
}
