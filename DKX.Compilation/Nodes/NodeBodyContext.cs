using DK.Code;
using DKX.Compilation.Variables;
using System;

namespace DKX.Compilation.Nodes
{
    class NodeBodyContext
    {
        private Node _body;

        public NodeBodyContext(Node body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public Node Body => _body;
        public CodeParser Code => _body.Code;

        public void PublishVariable(Variable variable)
        {
            var scope = _body.GetContainerOrNull<IVariableScopeNode>();
            if (scope == null) throw new InvalidOperationException("No variable scope is available.");
            scope.VariableStore.AddVariable(variable);

            var declarationScope = _body.GetContainerOrNull<IVariableDeclarationNode>();
            if (declarationScope == null) throw new InvalidOperationException("No variable declaration scope is available.");
            declarationScope.AddVariableDeclaration(new VariableDeclaration(variable.Name, variable.WbdkName, variable.DataType.ToCode()));
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
