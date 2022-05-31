namespace DKX.Compilation.Scopes.Statements
{
    abstract class Statement : Scope
    {
        public abstract bool IsEmpty { get; }

        private Span _span;

        public static readonly Statement[] EmptyArray = new Statement[0];

        public Statement(Scope parent, Span span)
            : base(parent)
        {
            _span = span;

            if (parent is Statement stmt) stmt.Span = stmt.Span.Envelope(span);
        }

        public Span Span
        {
            get => _span;
            protected set
            {
                _span = value;
                if (!value.IsEmpty && Parent is Statement stmt) stmt.Span = stmt.Span.Envelope(value);
            }
        }
    }
}
