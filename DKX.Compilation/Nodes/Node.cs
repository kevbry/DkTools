using DK.Code;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Nodes
{
    public abstract class Node
    {
        private Node _parent;
        private CodeParser _code;
        private Dictionary<string, Variable> _vars;
        private List<Node> _childNodes;

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
            OnError(new ReportItem(PathName, new CodeSpan(pos, pos), code, args));
        }

        protected void ReportError(CodeSpan span, ErrorCode code, params object[] args)
        {
            OnError(new ReportItem(PathName, span, code, args));
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
        #endregion

        #region Children
        protected void AddChildNode(Node node)
        {
            if (_childNodes == null) _childNodes = new List<Node>();
            _childNodes.Add(node ?? throw new ArgumentNullException(nameof(node)));
        }
        #endregion

        #region Code Parsing
        protected void SkipToAfterExit()
        {
            while (Code.ReadNestable())
            {
                if (Code.Type == CodeType.Operator && (Code.Text == "}" || Code.Text == ")" || Code.Text == "]")) break;
            }
        }
        #endregion
    }
}
