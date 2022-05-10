using DK.Code;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Nodes
{
    public abstract class Node
    {
        private Node _parent;
        private CodeParser _code;
        private Dictionary<string, Variable> _vars;
        private List<Node> _childNodes;

        public static readonly Node[] EmptyArray = new Node[0];

        public Node(Node parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _code = _parent._code;

            _parent.AddChildNode(this);
        }

        protected Node(CodeParser code)
        {
            _parent = null;
            _code = code;
        }

        public CodeParser Code => _code;

        public virtual string PathName => _parent.PathName;

        protected void ReportError(int pos, ErrorCode code, params object[] args)
        {
            if (!_code.GetLineNumberAndOffset(pos, out var lineNumber, out var lineOffset))
            {
                lineNumber = -1;
                lineOffset = -1;
            }
            OnError(new ReportItem(PathName, lineNumber, lineOffset, -1, -1, code, args));
        }

        protected void ReportError(CodeSpan span, ErrorCode code, params object[] args)
        {
            int startLine, startOff, endLine, endOff;
            if (!_code.GetLineNumberAndOffset(span.Start, out startLine, out startOff))
            {
                startLine = -1;
                startOff = -1;
                endLine = -1;
                endOff = -1;
            }
            else if (!_code.GetLineNumberAndOffset(span.End, out endLine, out endOff))
            {
                endLine = -1;
                endOff = -1;
            }

            OnError(new ReportItem(PathName, startLine, startOff, endLine, endOff, code, args));
        }

        protected virtual void OnError(ReportItem error)
        {
            _parent.OnError(error);
        }

        #region Variables
        public bool HasVariable(string name)
        {
            if (_vars != null && _vars.ContainsKey(name)) return true;
            return _parent?.HasVariable(name) ?? false;
        }

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
        #endregion
    }
}
