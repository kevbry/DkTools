using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Nodes
{
    class FileNode : Node
    {
        private string _pathName;
        private List<ReportItem> _reportItems = new List<ReportItem>();

        public FileNode(string pathName, CodeParser code)
            : base(code)
        {
            _pathName = pathName ?? throw new ArgumentNullException(nameof(pathName));
        }

        public void Parse()
        {
            while (!Code.EndOfFile)
            {
                var dataType = DataType.Parse(Code);
                if (dataType != null)
                {
                    AfterDataType(dataType.Value);
                }
                else if (Code.Read())
                {
                    ReportError(Code.Span, ErrorCode.UnexpectedToken, Code.Text);
                }
                else break;
            }
        }

        private void AfterDataType(DataType dataType)
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
                            var argType = ArgumentType.ByValue;
                            if (Code.ReadExactWholeWord("ref")) argType = ArgumentType.ByReference;
                            else if (Code.ReadExactWholeWord("out")) argType = ArgumentType.Out;

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
                            SkipToAfterExit();
                            break;
                        }
                    }

                    if (!Code.ReadExact('{'))
                    {
                        ReportError(Code.Position, ErrorCode.ExpectedToken, '{');
                    }

                    // TODO: Read the statements
                    SkipToAfterExit();

                    var method = new MethodNode(this, word, dataType, args);
                }
                else
                {
                    // This is a global variable
                    while (true)
                    {
                        if (CompileConstants.AllKeywords.Contains(word)) ReportError(wordSpan, ErrorCode.InvalidVariableName, word);
                        else if (HasVariable(word)) ReportError(wordSpan, ErrorCode.DuplicateVariable, word);
                        else AddVariable(new Variable(word, dataType, argType: null));

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
        }

        protected override void OnError(ReportItem error)
        {
            _reportItems.Add(error);
        }

        public override string PathName => _pathName;
    }
}
