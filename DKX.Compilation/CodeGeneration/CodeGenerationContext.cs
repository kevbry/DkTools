using DK.AppEnvironment;
using DK.Code;
using DKX.Compilation.DataTypes;
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

        public CodeFragment ResolveIdentifier(string identName, CodeSpan identSpan)
        {
            if (_variables != null && _variables.TryGetValue(identName, out var variable))
            {
                var variableDataType = DataType.Parse(variable.DataType) ?? DataType.Unsupported;
                return new CodeFragment(variable.Name, variableDataType, OpPrec.None, terminated: false, identSpan, readOnly: false);
            }

            if (_constants != null && _constants.TryGetValue(identName, out var constant))
            {
                var constDataType = DataType.Parse(constant.DataType) ?? DataType.Unsupported;
                return new CodeFragment(constant.Name, constDataType, OpPrec.None, terminated: false, identSpan, readOnly: true);
            }

            return CodeFragment.Empty;
        }
    }
}
