using System;

namespace DKX.Compilation.Variables
{
    class VariableDeclaration
    {
        public string Name { get; private set; }
        public string WbdkName { get; private set; }
        public string DataType { get; private set; }

        public static readonly VariableDeclaration[] EmptyArray = new VariableDeclaration[0];

        public VariableDeclaration(string name, string wbdkName, string dataType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            WbdkName = wbdkName ?? throw new ArgumentNullException(nameof(wbdkName));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        }
    }
}
