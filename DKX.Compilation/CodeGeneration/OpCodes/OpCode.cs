namespace DKX.Compilation.CodeGeneration.OpCodes
{
    static class OpCode
    {
        // Statements
        public const string If = "if";
        public const string Return = "ret";
        public const string While = "while";
        public const string For = "for";

        // Operators
        public const string Assign = "mov";

        public const string CompareEQ = "eq";
        public const string CompareNE = "ne";
        public const string CompareLT = "lt";
        public const string CompareLE = "le";
        public const string CompareGT = "gt";
        public const string CompareGE = "ge";

        public const string Not = "not";
        public const string And = "and";
        public const string Or = "or";

        public const string Increment = "inc";
        public const string Decrement = "dec";
        public const string Negate = "neg";

        public const string Add = "add";
        public const string Subtract = "sub";
        public const string Multiply = "mul";
        public const string Divide = "div";
        public const string Modulus = "mod";

        public const string AssignAdd = "addto";
        public const string AssignSubtract = "subto";
        public const string AssignMultiply = "multo";
        public const string AssignDivide = "divto";
        public const string AssignModulus = "modto";

        public const string Dot = "dot";    // For accessing children

        public const string Ternary = "trn";
    }
}
