using DKX.Compilation.DataTypes;
using DKX.Compilation.Exceptions;
using DKX.Compilation.Project.Bson;
using DKX.Compilation.Variables.ConstantValues;
using System;
using System.Collections.Generic;

namespace DKX.Compilation.Variables.ConstTerms
{
    /// <summary>
    /// Stores unresolved operator expressions to be resolved later in the compile process when everything has been pre-scanned.
    /// </summary>
    public abstract class ConstTerm
    {
        public static readonly ConstTerm[] EmptyArray = new ConstTerm[0];

        public abstract DataType DataType { get; }
        internal abstract ConstValue ResolveConstantOrNull(ConstResolutionContext context, IEnumerable<string> circularDependencyCheckList);
        protected abstract void SaveInner(BsonObject obj);

        private Span _span;

        public ConstTerm(Span span)
        {
            _span = span;
        }

        public Span Span => _span;

        public BsonObject ToBson(BsonFile bson)
        {
            var obj = new BsonObject(bson);

            obj.SetString("Type", GetType().FullName);
            obj.SetSpan("Span", _span);
            SaveInner(obj);

            return obj;
        }

        public static ConstTerm FromBson(BsonNode node)
        {
            if (!(node is BsonObject obj)) throw new InvalidBsonDataException("ConstTerm node is not an object.");

            var typeName = obj.GetString("Type");
            var span = obj.GetSpan("Span");

            var type = System.Reflection.Assembly.GetExecutingAssembly().GetType(typeName);
            if (type == null) throw new InvalidBsonDataException($"ConstTerm type '{typeName}' was not found.");
            if (!typeof(ConstTerm).IsAssignableFrom(type)) throw new InvalidBsonDataException($"Type '{typeName}' does not inherit from ConstTerm.");

            // Find a compatible constructor which includes a BinaryReader.
            foreach (var ctor in type.GetConstructors())
            {
                var ctorParams = ctor.GetParameters();
                var gotBsonObject = false;
                var compatible = true;
                foreach (var p in ctorParams)
                {
                    if (p.ParameterType == typeof(BsonObject))
                    {
                        gotBsonObject = true;
                    }
                    else if (p.ParameterType != typeof(Span))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible && gotBsonObject)
                {
                    var parms = new object[ctorParams.Length];
                    for (var i = 0; i < parms.Length; i++)
                    {
                        if (ctorParams[i].ParameterType == typeof(BsonObject)) parms[i] = obj;
                        else if (ctorParams[i].ParameterType == typeof(Span)) parms[i] = span;
                    }
                    return (ConstTerm)ctor.Invoke(parms);
                }
            }

            throw new InvalidOperationException("A compatible constructor could not be found.");
        }
    }

    class InvalidConstTermIdException : CompilerException { }
}
