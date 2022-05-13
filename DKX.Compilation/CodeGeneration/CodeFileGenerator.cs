using DK.AppEnvironment;
using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Files;
using DKX.Compilation.ReportItems;
using System;
using System.Threading.Tasks;

namespace DKX.Compilation.CodeGeneration
{
    class CodeFileGenerator
    {
        private DkAppContext _app;
        private ObjectFileModel _obj;
        private CodeWriter _code;
        private ISourceCodeReporter _report;

        public CodeFileGenerator(DkAppContext app, ObjectFileModel obj, ISourceCodeReporter reporter)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _obj = obj ?? throw new ArgumentNullException(nameof(obj));
            _report = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public Task<string> GenerateCodeAsync()
        {
            _code = new CodeWriter();

            // TODO: Member variables
            // TODO: Properties

            if (_obj.Methods != null)
            {
                foreach (var method in _obj.Methods)
                {
                    if (!GenerateMethod(method)) return Task.FromResult<string>(null);
                }
            }

            return Task.FromResult<string>(_code.ToString());
        }

        private bool GenerateMethod(ObjectMethod method)
        {
            WriteDataType(method.ReturnDataType);
            _code.Write(' ');
            _code.Write(method.Name);
            _code.Write('(');

            var first = true;
            if (method.Arguments != null)
            {
                foreach (var arg in method.Arguments)
                {
                    if (first) first = false;
                    else _code.Write(", ");
                    _code.Write(arg.DataType);
                }
            }

            _code.Write(')');
            _code.WriteLine();
            using (var body = _code.Indent())
            {
                if (method.Body != null)
                {
                    if (method.Body.Variables != null)
                    {
                        foreach (var variable in method.Body.Variables)
                        {
                            WriteDataType(variable.DataType);
                            _code.Write(' ');
                            _code.Write(variable.Name);
                            _code.Write(';');
                            _code.WriteLine();
                        }
                    }

                    var context = new CodeGenerationContext(_app, method.Body.Variables, _obj.Constants);
                    var codeGenerator = new CodeGenerator(method.Body.Code, context, _code, _report);
                    codeGenerator.GenerateBody(mustEndWithClose: false);
                }
            }

            return true;
        }

        private void WriteDataType(string modelDataType)
        {
            var dataType = DataType.Parse(modelDataType);
            if (dataType == null || dataType.Value.BaseType == BaseType.Unsupported) throw new InvalidObjectFileException($"Invalid model data type '{modelDataType}'");
            _code.Write(dataType.Value.ToDkCode());
        }
    }
}
