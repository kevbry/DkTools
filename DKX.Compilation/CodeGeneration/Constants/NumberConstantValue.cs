namespace DKX.Compilation.CodeGeneration.Constants
{
    class NumberConstantValue : ConstantValue
    {
        private decimal _value;

        public NumberConstantValue(decimal value)
        {
            _value = value;
        }

        public override string ToCode() => _value.ToString();
    }
}
