namespace DKX.Compilation.Nodes
{
    abstract class Statement : Node
    {
        public abstract string ToCode();

        public Statement(Node parent)
            : base(parent)
        {
        }
    }
}
