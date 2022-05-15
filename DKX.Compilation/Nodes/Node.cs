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
using System.Text;

namespace DKX.Compilation.Nodes
{
    public abstract class Node : ISourceCodeReporter
    {
        private Node _parent;
        private DkAppContext _app;
        private CodeParser _code;
        private Dictionary<string, Variable> _vars;
        private List<Node> _childNodes;

        public static readonly Node[] EmptyArray = new Node[0];

        public Node(Node parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _app = _parent._app;
            _code = _parent._code;

            _parent.AddChildNode(this);
        }

        protected Node(DkAppContext app, CodeParser code)
        {
            _parent = null;
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _code = code ?? throw new ArgumentNullException(nameof(code));
        }

        public DkAppContext App => _app;
        public CodeParser Code => _code;

        public virtual string PathName => _parent.PathName;

        #region Report Items
        public void ReportItem(int pos, ErrorCode code, params object[] args)
        {
            OnReportItem(new ReportItem(PathName, _code.DocumentText, pos, code, args));
        }

        public void ReportItem(CodeSpan span, ErrorCode code, params object[] args)
        {
            OnReportItem(new ReportItem(PathName, _code.DocumentText, span, code, args));
        }

        protected virtual void OnReportItem(ReportItem error)
        {
            _parent.OnReportItem(error);
        }
        #endregion

        #region Variables
        public bool HasVariable(string name)
        {
            if (_vars != null && _vars.ContainsKey(name)) return true;
            return _parent?.HasVariable(name) ?? false;
        }

        public virtual bool HasConstant(string name) => _parent.HasConstant(name);

        public virtual bool HasProperty(string name) => _parent.HasProperty(name);

        public Variable GetVariable(string name)
        {
            if (_vars != null)
            {
                if (_vars.TryGetValue(name, out var variable)) return variable;
            }

            return _parent?.GetVariable(name);
        }

        public void AddVariable(Variable variable)
        {
            if (_vars == null) _vars = new Dictionary<string, Variable>();
            _vars[variable.Name] = variable;
        }

        public IEnumerable<Variable> Variables
        {
            get
            {
                if (_parent != null)
                {
                    foreach (var v in _parent.Variables) yield return v;
                }

                if (_vars != null)
                {
                    foreach (var v in _vars.Values) yield return v;
                }
            }
        }
        #endregion

        #region Children
        protected void AddChildNode(Node node)
        {
            if (_childNodes == null) _childNodes = new List<Node>();
            _childNodes.Add(node ?? throw new ArgumentNullException(nameof(node)));
        }

        public IEnumerable<Node> ChildNodes => (IEnumerable<Node>)_childNodes ?? EmptyArray;

        public T GetContainerOrNull<T>() where T : class
        {
            if (this is T me) return me;
            if (_parent != null) return _parent.GetContainerOrNull<T>();
            return null;
        }
        #endregion

        #region Statements
        internal void ReadCodeBody(int bodyStartPos)
        {
            if (!(this is IBodyNode bodyNode)) throw new InvalidOperationException("This method may only be called by IBodyNode implementations.");

            while (true)
            {
                if (Code.ReadExact('}'))
                {
                    bodyNode.BodySpan = new CodeSpan(bodyStartPos, Code.Span.Start);
                    return;
                }
                if (!ParseStatement())
                {
                    Code.SkipToAfterExit(out var bodyEndPos);
                    bodyNode.BodySpan = new CodeSpan(bodyStartPos, bodyEndPos);
                    return;
                }
            }
        }

        internal bool ParseStatement()
        {
            if (Code.ReadExact("return"))
            {
                new ReturnStatement(this, Code.Span);
                return true;
            }

            // Try for variable declaration
            var dataType = ReadDataTypeOrNull(out var dataTypeSpan);
            if (dataType != null)
            {
                while (true)
                {
                    if (!Code.ReadWord())
                    {
                        ReportItem(dataTypeSpan, ErrorCode.ExpectedVariableName);
                        return true;
                    }
                    var name = Code.Text;
                    var nameSpan = Code.Span;
                    Chain initializer = null;
                    var initializerSpan = nameSpan;

                    if (Code.ReadExact('='))
                    {
                        var eqSpan = Code.Span;
                        initializer = ExpressionParser.ReadExpressionOrNull(Code);
                        if (initializer == null) ReportItem(eqSpan, ErrorCode.ExpectedExpression);
                        initializerSpan = nameSpan.Envelope(initializer.Span);
                    }

                    if (CompileConstants.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                    else if (HasVariable(name) || HasConstant(name) || HasProperty(name)) ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                    else
                    {
                        var variable = new Variable(
                            name: name,
                            dataType: dataType.Value,
                            fileContext: FileContext.NeutralClass,  // Variables don't use a file context as that's up to the parent method / property accessor.
                            passType: null,                         // Variables don't use a pass type; that's only for arguments.
                            initializer: null);                     // Variables don't use an initializer; they add the initialization statement into the code directly where they are declared.

                        AddVariable(variable);
                        if (initializer != null) new VariableInitializationStatement(this, variable, initializer, initializerSpan);
                    }

                    if (Code.ReadExact(';')) return true;
                    if (Code.ReadExact(',')) continue;

                    ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
                    return true;
                }
            }

            var exp = ExpressionParser.ReadExpressionOrNull(Code);
            if (exp != null)
            {
                new ExpressionStatement(this, exp);
                if (!Code.ReadExact(';')) ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
                return true;
            }

            ReportItem(Code.Position, ErrorCode.ExpectedStatement);
            return false;
        }

        protected DataType? ReadDataTypeOrNull(out CodeSpan spanOut)
        {
            var dataType = DataType.Parse(Code, out var dataTypeSpan);
            if (dataType != null)
            {
                spanOut = dataTypeSpan;
                return dataType;
            }

            if (Code.ReadWord())
            {
                dataType = GetTypedefDataType(Code.Text);
                if (dataType == null) Code.Position = Code.Span.Start;
                spanOut = Code.Span;
                return dataType;
            }

            spanOut = CodeSpan.Empty;
            return null;
        }

        protected virtual DataType? GetTypedefDataType(string typedefName) => _parent.GetTypedefDataType(typedefName);

        protected ObjectBody GenerateObjectBody(int parentOffset)
        {
            if (!(this is IBodyNode bodyNode)) throw new InvalidOperationException("This method may only be called for an IBodyNode implementation.");

            var variables = Variables.Where(v => v.IsArgument == false).Select(v => v.ToObjectVariable()).ToArray();
            if (variables.Length == 0) variables = null;

            var bodyCode = new StringBuilder();
            foreach (var stmt in ChildNodes.Where(n => n is Statement).Cast<Statement>())
            {
                var stmtCode = stmt.ToCode(parentOffset);
                if (string.IsNullOrEmpty(stmtCode)) continue;

                if (bodyCode.Length > 0) bodyCode.Append(',');
                bodyCode.Append(stmtCode);
            }

            return new ObjectBody
            {
                Variables = variables,
                Code = bodyCode.ToString(),
                StartPosition = bodyNode.BodySpan.Start
            };
        }
        #endregion
    }
}
