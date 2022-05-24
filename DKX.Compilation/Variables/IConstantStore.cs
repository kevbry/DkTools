namespace DKX.Compilation.Variables
{
    interface IConstantStore
    {
        bool TryGetConstant(string name, out Constant constant);
    }
}
