using DK.Code;
using DKX.Compilation.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DKX.Compilation.Nodes
{
    class NodeBodyContext
    {
        private Node _body;

        public NodeBodyContext(Node body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public CodeParser Code => _body.Code;

        public bool TryGetVariable(string name, out Variable variableOut)
        {
            var variable = _body.GetVariable(name);
            if (variable != null)
            {
                variableOut = variable;
                return true;
            }

            variableOut = default;
            return false;
        }

        public bool TryGetConstant(string name, out Constant constantOut)
        {
            var constant = _body.GetConstant(name);
            if (constant != null)
            {
                constantOut = constant;
                return true;
            }

            constantOut = default;
            return false;
        }
    }
}
