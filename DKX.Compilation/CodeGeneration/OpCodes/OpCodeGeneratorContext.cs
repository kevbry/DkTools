using DKX.Compilation.DataTypes;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.CodeGeneration.OpCodes
{
    public class OpCodeGeneratorContext
    {
        public OpCodeGeneratorContext(DataType returnDataType)
        {
            _returnDataType = returnDataType;
        }

        #region Return DataType
        private DataType _returnDataType;

        public DataType ReturnDataType => _returnDataType;
        #endregion

        #region Registers
        private List<Register> _registers;
        private int _registerCounter;

        public string GetRegister(DataType? dataType)
        {
            if (_registers == null) _registers = new List<Register>();

            foreach (var reg in _registers)
            {
                if (reg.Used) continue;
                if (dataType != null && reg.DataType != null && reg.DataType == dataType)
                {
                    reg.Used = true;
                    return reg.VarName;
                }
            }

            var newReg = new Register($"__{_registerCounter++}", dataType);
            _registers.Add(newReg);
            return newReg.VarName;
        }

        public void FreeRegister(string regName)
        {
            if (_registers == null) return;

            foreach (var reg in _registers)
            {
                if (reg.VarName == regName)
                {
                    reg.Used = false;
                }
            }
        }

        private class Register
        {
            public string VarName { get; private set; }
            public DataType? DataType { get; private set; }
            public bool Used { get; set; }

            public Register(string varName, DataType? dataType)
            {
                VarName = varName ?? throw new ArgumentNullException(nameof(varName));
                DataType = dataType;
                Used = false;
            }
        }
        #endregion
    }
}
