using DK.Code;
using DKX.Compilation.CodeGeneration.OpCodes;

namespace DKX.Compilation.Nodes.Statements
{
    abstract class Statement : Node
    {
        private CodeSpan _span;

        /// <summary>
        /// Writes the op code instructions for the statement.
        /// </summary>
        /// <param name="code">Op code writer</param>
        /// <param name="parentOffset">Offset of the parent statement, for generating spans.</param>
        public abstract void ToCode(OpCodeGenerator code, int parentOffset);

        /// <summary>
        /// Returns true if the statement does not yield any op codes.
        /// </summary>
        public abstract bool IsEmptyCode { get; }

        public Statement(Node parent, CodeSpan span)
            : base(parent)
        {
            _span = span;
            if (Parent is Statement parentStmt) parentStmt.Span = parentStmt.Span.Envelope(_span);
        }

        public CodeSpan Span
        {
            get => _span;
            protected set
            {
                _span = value;
                if (Parent is Statement parentStmt) parentStmt.Span = parentStmt.Span.Envelope(_span);
            }
        }
    }
}
