namespace DKX.Compilation.CodeGeneration.Constants
{
    abstract class ConstantValue
    {
        public abstract string ToCode();

        public override string ToString() => ToCode();
    }
}
