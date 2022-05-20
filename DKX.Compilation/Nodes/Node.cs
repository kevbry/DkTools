using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
using DKX.Compilation.Nodes.Statements;
using DKX.Compilation.ReportItems;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.Nodes
{
    public abstract class Node : ISourceCodeReporter
    {
        private Node _parent;
        private DkAppContext _app;
        private CodeParser _code;
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
        public Node Parent => _parent;

        public virtual string PathName => _parent.PathName;

        #region Report Items
        public void ReportItem(int pos, ErrorCode code, params object[] args)
        {
            OnReportItem(new ReportItem(PathName, _code.Source, pos, code, args));
        }

        public void ReportItem(CodeSpan span, ErrorCode code, params object[] args)
        {
            OnReportItem(new ReportItem(PathName, _code.Source, span, code, args));
        }

        protected virtual void OnReportItem(ReportItem error)
        {
            _parent.OnReportItem(error);
        }

        public virtual bool HasErrors => _parent.HasErrors;
        #endregion

        #region Properties
        public virtual bool HasProperty(string name) => _parent.HasProperty(name);
        #endregion

        #region Constants
        public virtual bool HasConstant(string name) => _parent.HasConstant(name);

        internal virtual Constant GetConstant(string name) => _parent.GetConstant(name);
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
        internal IEnumerable<Statement> Statements => _childNodes.Where(x => x is Statement).Cast<Statement>();

        /// <summary>
        /// Reads code between braces { }.
        /// This method will read the closing '}'.
        /// </summary>
        /// <param name="bodyContext">The method/property context.</param>
        /// <param name="bodyStartPos">Starting position of the body. This should be immediately following the opening '{'.
        internal bool ReadCodeBody(NodeBodyContext bodyContext, int bodyStartPos)
        {
            if (!(this is IBodyNode bodyNode)) throw new InvalidOperationException("This method may only be called by IBodyNode implementations.");

            while (true)
            {
                if (Code.ReadExact('}'))
                {
                    bodyNode.BodySpan = new CodeSpan(bodyStartPos, Code.Span.Start);
                    return true;
                }

                if (!ReadStatement(bodyContext, out _))
                {
                    Code.SkipToAfterExit(out var bodyEndPos);
                    bodyNode.BodySpan = new CodeSpan(bodyStartPos, bodyEndPos);
                    return false;
                }
            }
        }

        internal bool ReadStatement(
            NodeBodyContext bodyContext,
            out CodeSpan stmtSpanOut,
            bool allowControlStatements = true,
            bool allowVariableDeclarations = true,
            bool tryReadStatementEndToken = true)
        {
            var word = Code.PeekWordR();

            if (allowControlStatements)
            {
                Statement stmt;
                switch (word)
                {
                    case "for":
                        stmt = new ForStatement(this, Code.MovePeekedSpan(), bodyContext);
                        stmtSpanOut = stmt.Span;
                        return true;
                    case "if":
                        stmt = new IfStatement(this, Code.MovePeekedSpan(), bodyContext);
                        stmtSpanOut = stmt.Span;
                        return true;
                    case "return":
                        stmt = new ReturnStatement(this, Code.MovePeekedSpan(), bodyContext);
                        stmtSpanOut = stmt.Span;
                        return true;
                    case "while":
                        stmt = new WhileStatement(this, Code.MovePeekedSpan(), bodyContext);
                        stmtSpanOut = stmt.Span;
                        return true;
                }
            }

            if (allowVariableDeclarations)
            {
                if (TryReadDataType(out var dataType, out var dataTypeSpan))
                {
                    ReadSpecificVariableDeclaration(
                        bodyContext: bodyContext,
                        dataType: dataType,
                        dataTypeSpan: dataTypeSpan,
                        requireAllVariablesToBeInitialized: false,
                        stmtSpanOut: out var stmtSpan);

                    stmtSpanOut = stmtSpan;
                    return true;
                }
            }

            var exp = ExpressionParser.ReadExpressionOrNull(bodyContext);
            if (exp != null)
            {
                exp.Report(this);
                new ExpressionStatement(this, exp);
                if (tryReadStatementEndToken)
                {
                    if (!Code.ReadExact(';'))
                    {
                        ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
                    }
                    else
                    {
                        stmtSpanOut = exp.Span.Envelope(Code.Span);
                        return true;
                    }
                }
                else
                {
                    stmtSpanOut = exp.Span;
                    return true;
                }
            }
            else
            {
                if (tryReadStatementEndToken)
                {
                    if (Code.ReadExact(';'))
                    {
                        new EmptyStatement(this, Code.Span);
                        stmtSpanOut = Code.Span;
                        return true;
                    }
                }
            }

            ReportItem(Code.Position, ErrorCode.ExpectedStatement);
            stmtSpanOut = default;
            return false;
        }

        protected bool TryReadDataType(out DataType dataTypeOut, out CodeSpan spanOut)
        {
            if (DataType.TryParse(Code, out var dataType, out var dataTypeSpan))
            {
                dataTypeOut = dataType;
                spanOut = dataTypeSpan;
                return true;
            }

            DataType? typedefDataType;
            if (Code.PeekWord() && (typedefDataType = GetTypedefDataType(Code.Text)) != null)
            {
                dataTypeOut = typedefDataType.Value;
                spanOut = Code.MovePeekedSpan();
                return true;
            }

            dataTypeOut = default;
            spanOut = default;
            return false;
        }

        protected virtual DataType? GetTypedefDataType(string typedefName) => _parent.GetTypedefDataType(typedefName);

        protected ObjectBody GenerateObjectBody(int parentOffset)
        {
            if (!(this is IBodyNode bodyNode)) throw new InvalidOperationException("This method may only be called for an IBodyNode implementation.");

            ObjectVariable[] variables = null;
            if (this is IVariableDeclarationNode varDeclNode) variables = varDeclNode.GetVariableDeclarations().Select(x => x.ToObjectVariable()).ToArray();
            if (variables.Length == 0) variables = null;

            var generator = new OpCodeGenerator();
            GenerateStatementsCode(generator, parentOffset);

            return new ObjectBody
            {
                Variables = variables,
                Code = generator.ToString(),
                StartPosition = bodyNode.BodySpan.Start
            };
        }

        protected void GenerateStatementsCode(OpCodeGenerator code, int parentOffset)
        {
            var first = true;
            foreach (var stmt in ChildNodes.Where(n => n is Statement).Cast<Statement>().Where(x => !x.IsEmptyCode))
            {
                if (first) first = false;
                else code.WriteDelim();
                stmt.ToCode(code, parentOffset);
            }
        }

        internal void ReadSpecificVariableDeclaration(
            NodeBodyContext bodyContext,
            DataType dataType,
            CodeSpan dataTypeSpan,
            bool requireAllVariablesToBeInitialized,
            out CodeSpan stmtSpanOut)
        {
            var stmtSpan = dataTypeSpan;

            while (true)
            {
                if (!Code.ReadWord())
                {
                    ReportItem(dataTypeSpan, ErrorCode.ExpectedVariableName);
                    stmtSpanOut = stmtSpan;
                    return;
                }
                var name = Code.Text;
                var nameSpan = Code.Span;
                stmtSpan = stmtSpan.Envelope(nameSpan);
                Chain initializer = null;
                var initializerSpan = nameSpan;

                if (Code.ReadExact('='))
                {
                    var eqSpan = Code.Span;
                    initializer = ExpressionParser.ReadExpressionOrNull(bodyContext);
                    if (initializer == null) ReportItem(eqSpan, ErrorCode.ExpectedExpression);
                    initializerSpan = nameSpan.Envelope(initializer.Span);
                    stmtSpan = stmtSpan.Envelope(initializerSpan);
                }
                else if (requireAllVariablesToBeInitialized)
                {
                    ReportItem(nameSpan, ErrorCode.VariableInitializationRequired);
                }

                if (DkxConst.AllKeywords.Contains(name))
                {
                    ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);
                }
                else if (GetContainerOrNull<IVariableScopeNode>()?.VariableStore.HasVariable(name, includeParents: true) == true ||
                    HasConstant(name) || HasProperty(name))
                {
                    ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
                }
                else
                {
                    var variable = new Variable(
                        name: name,
                        wbdkName: name,
                        dataType: dataType,
                        fileContext: FileContext.NeutralClass,  // Variables don't use a file context as that's up to the parent method / property accessor.
                        passType: null,                         // Variables don't use a pass type; that's only for arguments.
                        initializer: null);                     // Variables don't use an initializer; they add the initialization statement into the code directly where they are declared.

                    bodyContext.PublishVariable(variable);
                    if (initializer != null)
                    {
                        new VariableInitializationStatement(this, variable, initializer, initializerSpan);
                        initializer.Report(this);
                    }
                }

                if (Code.ReadExact(';'))
                {
                    stmtSpanOut = stmtSpan.Envelope(Code.Span);
                    return;
                }
                if (Code.ReadExact(',')) continue;

                ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
                stmtSpanOut = stmtSpan;
                return;
            }
        }
        #endregion
    }
}
