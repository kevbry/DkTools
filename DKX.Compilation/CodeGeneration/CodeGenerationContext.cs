using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Expressions;
using DKX.Compilation.Files;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DKX.Compilation.CodeGeneration
{
    class CodeGenerationContext
    {
        private DkAppContext _app;
        private Dictionary<string, ObjectVariable> _variables;
        private Dictionary<string, ObjectConstant> _constants;

        public CodeGenerationContext(DkAppContext app, IEnumerable<ObjectVariable> variables, IEnumerable<ObjectConstant> constants)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));

            if (variables != null && variables.Any())
            {
                _variables = new Dictionary<string, ObjectVariable>();
                foreach (var variable in variables) _variables[variable.Name] = variable;
            }

            if (constants != null && constants.Any())
            {
                _constants = new Dictionary<string, ObjectConstant>();
                foreach (var constant in constants) _constants[constant.Name] = constant;
            }
        }

        public DataType ResolveDataType(string code)
        {
            var dataType = DataType.Parse(code) ?? DataType.Unsupported;
            if (dataType.IsUnresolved) return ResolveDataType(dataType);
            return dataType;
        }

        public DataType ResolveDataType(DataType dataType, DataType[] stack = null)
        {
            if (stack != null && stack.Contains(dataType)) return DataType.Unsupported;

            if (!dataType.IsUnresolved) return dataType;

            switch (dataType.BaseType)
            {
                case BaseType.Like1:
                    var name = dataType.Options[0];
                    if (_variables != null && _variables.TryGetValue(name, out var variable))
                    {
                        var variableDataType = DataType.Parse(variable.DataType) ?? DataType.Unsupported;
                        if (variableDataType.IsUnresolved) return ResolveDataType(variableDataType, (stack ?? DataType.EmptyArray).Concat(new DataType[] { dataType }).ToArray());
                        return variableDataType;
                    }

                    if (_constants != null && _constants.TryGetValue(name, out var constant))
                    {
                        var constDataType = DataType.Parse(constant.DataType) ?? DataType.Unsupported;
                        if (constDataType.IsUnresolved) return ResolveDataType(constDataType, (stack ?? DataType.EmptyArray).Concat(new DataType[] { dataType }).ToArray());
                        return constDataType;
                    }
                    return DataType.Unsupported;

                case BaseType.Like2:
                    var options = dataType.Options;
                    var parentName = options[0];
                    var childName = options[1];

                    var table = _app.Settings.Dict.GetTable(parentName);
                    if (table != null)
                    {
                        var col = table.GetColumn(childName);
                        if (col != null)
                        {
                            var colDataType = DataType.Parse(col.DataType.Source.ToString()) ?? DataType.Unsupported;
                            if (colDataType.IsUnresolved) return ResolveDataType(colDataType, (stack ?? DataType.EmptyArray).Concat(new DataType[] { dataType }).ToArray());
                            return colDataType;
                        }
                    }
                    return DataType.Unsupported;

                default:
                    throw new InvalidBaseTypeException(dataType.BaseType);
            }
        }

        public CodeFragment ResolveIdentifier(string identName, CodeSpan identSpan)
        {
            if (_variables != null && _variables.TryGetValue(identName, out var variable))
            {
                var variableDataType = DataType.Parse(variable.DataType) ?? DataType.Unsupported;
                if (variableDataType.IsUnresolved) variableDataType = ResolveDataType(variableDataType);

                return new CodeFragment(variable.Name, variableDataType, OpPrec.None, terminated: false, identSpan, readOnly: false);
            }

            if (_constants != null && _constants.TryGetValue(identName, out var constant))
            {
                var constDataType = DataType.Parse(constant.DataType) ?? DataType.Unsupported;
                if (constDataType.IsUnresolved) constDataType = ResolveDataType(constDataType);
            }

            return CodeFragment.Empty;
        }

        public bool TryGetVariable(string name, out DataType dataTypeOut)
        {
            if (_variables.TryGetValue(name, out var obj))
            {
                var varDataType = DataType.Parse(obj.DataType);
                if (varDataType != null)
                {
                    if (varDataType.Value.IsUnresolved) varDataType = ResolveDataType(varDataType.Value);

                    dataTypeOut = varDataType.Value;
                    return true;
                }
            }

            dataTypeOut = default;
            return false;
        }
    }
}
