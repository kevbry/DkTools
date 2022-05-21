using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;
using DKX.Compilation.Expressions;
using DKX.Compilation.Variables;

namespace DKX.Compilation.Nodes.Statements
{
    class VarStatement : Statement
    {
        private VariableInitializationStatement _initializer;

        public VarStatement(Node parent, CodeSpan keywordSpan, NodeBodyContext bodyContext)
            : base(parent, keywordSpan)
        {
            if (!Code.ReadWord()) ReportItem(Code.Position, ErrorCode.ExpectedVariableName);
            var name = Code.Text;
            var nameSpan = Code.Span;

            if (DkxConst.Keywords.AllKeywords.Contains(name)) ReportItem(nameSpan, ErrorCode.InvalidVariableName, name);

            var variableStore = GetContainerOrNull<IVariableScopeNode>().VariableStore;
            if (variableStore.HasVariable(name, includeParents: true) ||
                HasConstant(name) ||
                HasProperty(name))
            {
                ReportItem(nameSpan, ErrorCode.DuplicateVariable, name);
            }

            if (!Code.ReadExact('='))
            {
                ReportItem(nameSpan, ErrorCode.VariableInitializationRequired, '=');
                return;
            }

            var initializerExp = ExpressionParser.ReadExpressionOrNull(bodyContext);
            if (initializerExp == null)
            {
                ReportItem(nameSpan, ErrorCode.VariableInitializationRequired);
                return;
            }
            initializerExp.Report(this);

            var dataType = initializerExp.InferredDataType;
            if (!dataType.IsSuitableForVariable) ReportItem(initializerExp.Span, ErrorCode.InvalidVariableDataType);

            var variable = new Variable(
                name: name,
                wbdkName: name,
                dataType: dataType,
                fileContext: FileContext.NeutralClass,
                passType: null,
                initializer: null);

            _initializer = new VariableInitializationStatement(this, variable, initializerExp, initializerExp.Span);

            bodyContext.PublishVariable(variable);

            if (!Code.ReadExact(';'))
            {
                ReportItem(Code.Position, ErrorCode.ExpectedToken, ';');
                return;
            }
        }

        public override void ToCode(OpCodeGenerator code, int parentOffset) => _initializer?.ToCode(code, parentOffset);

        public override bool IsEmptyCode => _initializer?.IsEmptyCode ?? true;
    }
}
