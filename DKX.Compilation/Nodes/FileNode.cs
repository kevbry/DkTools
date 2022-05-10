using DK;
using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.DataTypes;
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

        public FileNode(string pathName, CodeParser code)
            : base(code)
        {
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
            _className = PathUtil.GetFileNameWithoutExtension(_pathName);
        }

        public string ClassName => _className;
        public bool HasErrors => _reportItems.Any(i => i.Severity == ErrorSeverity.Error);
        public IEnumerable<MethodNode> Methods => ChildNodes.Where(n => n is MethodNode).Cast<MethodNode>();
        public IEnumerable<PropertyNode> Properties => ChildNodes.Where(n => n is PropertyNode).Cast<PropertyNode>();
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
                        ReportError(Code.Position, ErrorCode.ExpectedClassName);
                    }
                    else
                    {
                        className = Code.Text;
                        var fileName = PathUtil.GetFileNameWithoutExtension(_pathName);
                        if (!className.EqualsI(fileName))
                        {
                            ReportError(Code.Position, ErrorCode.ClassNameDoesNotMatchFileName);
                        }
                        else
                        {
                            _className = className;
                        }
                    }

                    if (!Code.ReadExact('{'))
                    {
                        ReportError(Code.Position, ErrorCode.ExpectedToken, '{');
                        continue;
                    }

                    InsideClass();
                }
                else if (Code.Read())
                {
                    ReportError(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
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
                else if ((dataType = DataType.Parse(Code)) != null) AfterDataType(dataType.Value, Privacy.Private);
                else if (Code.Read()) ReportError(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
                else break;
            }
        }

        private void AfterPrivacy(Privacy privacy)
        {
            var dataType = DataType.Parse(Code);
            if (dataType != null) AfterDataType(dataType.Value, privacy);
            else if (Code.Read()) ReportError(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
        }

        private void AfterDataType(DataType dataType, Privacy privacy)
        {
            if (Code.ReadWord())
            {
                var word = Code.Text;
                var wordSpan = Code.Span;
                if (Code.ReadExact('('))
                {
                    // This is a method declaration
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
                                ReportError(Code.Position, ErrorCode.ExpectedArgumentDataType);
                                argDataType = DataType.Unsupported;
                            }

                            string argName;
                            if (!Code.ReadWord())
                            {
                                ReportError(Code.Position, ErrorCode.ExpectedArgumentName);
                                argName = $"unnamed{++unnamedIndex}";
                            }
                            else argName = Code.Text;

                            args.Add(new Variable(argName, argDataType.Value, argType));

                            if (Code.ReadExact(')')) break;
                            if (Code.ReadExact(',')) continue;

                            ReportError(Code.Position, ErrorCode.ExpectedToken, ',');
                            Code.SkipToAfterExit();
                            break;
                        }
                    }

                    if (!Code.ReadExact('{'))
                    {
                        ReportError(Code.Position, ErrorCode.ExpectedToken, '{');
                    }

                    var method = new MethodNode(this, word, dataType, args, privacy);
                    ReadCodeBody(method);
                }
                else if (Code.ReadExact('{'))
                {
                    // This is a property
                    var prop = new PropertyNode(this, word, dataType, privacy);
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
                                ReportError(Code.Position, ErrorCode.ExpectedToken, '{');
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
                                ReportError(Code.Position, ErrorCode.ExpectedToken, '{');
                                break;
                            }
                        }
                        else if (Code.Read())
                        {
                            ReportError(Code.Position, ErrorCode.UnexpectedToken, Code.Text);
                            Code.SkipToAfterExit();
                            break;
                        }
                        else break;
                    }

                    if (!gotGetter && !gotSetter)
                    {
                        ReportError(wordSpan, ErrorCode.PropertyHasNoGetterOrSetter);
                    }
                    else if (!gotGetter)
                    {
                        ReportError(wordSpan, ErrorCode.PropertyHasNoGetter);
                    }
                }
                else
                {
                    // This is a member variable
                    while (true)
                    {
                        if (CompileConstants.AllKeywords.Contains(word)) ReportError(wordSpan, ErrorCode.InvalidVariableName, word);
                        else if (HasVariable(word)) ReportError(wordSpan, ErrorCode.DuplicateVariable, word);
                        else AddVariable(new Variable(word, dataType, passType: null));

                        if (privacy != Privacy.Private)
                        {
                            ReportError(wordSpan, ErrorCode.MemberVariableMustBePrivate);
                        }

                        if (Code.ReadExact(';')) break;

                        // TODO: In the future, add initializer statement

                        if (!Code.ReadExact(','))
                        {
                            ReportError(Code.Position, ErrorCode.ExpectedToken, ',');
                            break;
                        }

                        if (!Code.ReadWord())
                        {
                            ReportError(Code.Position, ErrorCode.ExpectedVariableName);
                            break;
                        }
                        word = Code.Text;
                    }
                }
            }
            else if (Code.Read()) ReportError(Code.Span, ErrorCode.ExpectedMethodName, Code.Text);
        }

        private void ReadCodeBody(Node parentMethodOrProperty)
        {
            Code.SkipToAfterExit();

            // TODO
        }

        protected override void OnError(ReportItem error)
        {
            _reportItems.Add(error);
        }
    }
}
